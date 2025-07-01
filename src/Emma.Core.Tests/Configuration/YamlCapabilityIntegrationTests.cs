using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Configuration
{
    public class YamlCapabilityIntegrationTests : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly PhysicalFileProvider _fileProvider;
        private readonly ServiceProvider _serviceProvider;
        private readonly Mock<ILogger<YamlHotReloadCapabilitySource>> _loggerMock;
        private readonly YamlAgentCapabilitySourceOptions _options;

        public YamlCapabilityIntegrationTests()
        {
            // Create a temporary file for testing
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"emma-test-{Guid.NewGuid()}.yaml");
            File.WriteAllText(_tempFilePath, GetSampleYaml("1.0"));
            
            // Set up file provider
            var directoryPath = Path.GetDirectoryName(_tempFilePath)!;
            _fileProvider = new PhysicalFileProvider(directoryPath);
            
            // Set up options
            _options = new YamlAgentCapabilitySourceOptions
            {
                FilePath = Path.GetFileName(_tempFilePath),
                ValidateSchema = true,
                ReloadOnChange = true
            };
            
            // Set up services
            var services = new ServiceCollection();
            
            // Add logging
            _loggerMock = new Mock<ILogger<YamlHotReloadCapabilitySource>>();
            services.AddLogging();
            
            // Add configuration
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            services.AddSingleton<IConfiguration>(config);
            
            // Add required services
            services.AddOptions();
            services.AddSingleton<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(
                new OptionsMonitor<YamlAgentCapabilitySourceOptions>(
                    new OptionsFactory<YamlAgentCapabilitySourceOptions>(
                        new[] { new ConfigureOptions<YamlAgentCapabilitySourceOptions>(o => 
                        {
                            o.FilePath = _options.FilePath;
                            o.ValidateSchema = _options.ValidateSchema;
                            o.ReloadOnChange = _options.ReloadOnChange;
                        }) },
                        Array.Empty<IPostConfigureOptions<YamlAgentCapabilitySourceOptions>>()),
                    Array.Empty<IOptionsChangeTokenSource<YamlAgentCapabilitySourceOptions>>(),
                    new OptionsCache<YamlAgentCapabilitySourceOptions>()));
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task LoadCapabilities_WithValidFile_ReturnsCapabilities()
        {
            // Arrange
            var source = new YamlHotReloadCapabilitySource(
                _fileProvider,
                _serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                _loggerMock.Object);
            
            // Act
            var capabilities = await source.LoadCapabilitiesAsync();
            
            // Assert
            Assert.NotNull(capabilities);
            Assert.True(source.IsLoaded);
            Assert.NotNull(capabilities.Agents);
            Assert.True(capabilities.Agents.ContainsKey("NbaAgent"));
            Assert.Equal(2, capabilities.Agents["NbaAgent"].Capabilities.Count);
        }

        [Fact]
        public async Task HotReload_WhenFileChanges_UpdatesCapabilities()
        {
            // Arrange
            var reloadedEvent = new TaskCompletionSource<bool>();
            var source = new YamlHotReloadCapabilitySource(
                _fileProvider,
                _serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                _loggerMock.Object);
            
            // Subscribe to reload event
            source.CapabilitiesReloaded += (s, e) =>
            {
                if (e.Success)
                    reloadedEvent.TrySetResult(true);
                else
                    reloadedEvent.TrySetException(e.Error ?? new Exception("Reload failed"));
                return Task.CompletedTask;
            };
            
            // Initial load
            var initialCapabilities = await source.LoadCapabilitiesAsync();
            
            // Modify the file
            File.WriteAllText(_tempFilePath, GetSampleYaml("1.1"));
            
            // Wait for reload (with timeout)
            var completedTask = await Task.WhenAny(
                reloadedEvent.Task,
                Task.Delay(5000));
                
            // Assert
            Assert.True(completedTask == reloadedEvent.Task, "Reload did not complete within timeout");
            Assert.True(await reloadedEvent.Task, "Reload was not successful");
            
            var updatedCapabilities = await source.LoadCapabilitiesAsync();
            Assert.Equal("1.1", updatedCapabilities.Version);
        }

        [Fact]
        public async Task LoadCapabilities_WithInvalidYaml_ThrowsValidationException()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "invalid: yaml: : :");
            var source = new YamlHotReloadCapabilitySource(
                _fileProvider,
                _serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                _loggerMock.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => source.LoadCapabilitiesAsync());
        }

        [Fact]
        public async Task LoadCapabilities_WithInvalidSchema_ThrowsValidationException()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "version: '1.0'\nagents: {}");
            var source = new YamlHotReloadCapabilitySource(
                _fileProvider,
                _serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                _loggerMock.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
                () => source.LoadCapabilitiesAsync());
        }

        [Fact]
        public async Task LoadCapabilities_WithConcurrentAccess_HandlesSynchronization()
        {
            // Arrange
            var source = new YamlHotReloadCapabilitySource(
                _fileProvider,
                _serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                _loggerMock.Object);
            
            // Act - Run multiple concurrent loads
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var result = await source.LoadCapabilitiesAsync();
                    Assert.NotNull(result);
                });
            }
            
            // Assert - All tasks should complete successfully
            await Task.WhenAll(tasks);
        }

        private string GetSampleYaml(string version)
        {
            return $@"
version: ""{version}""

agents:
  NbaAgent:
    capabilities:
      - name: ""suggest:action""
        description: ""Suggest next best actions for a conversation""
        enabled: true
        validation_rules:
          max_suggestions: 5
          allowed_contexts: [""sales"", ""support""]
      
      - name: ""analyze:sentiment""
        description: ""Analyze sentiment of conversation""
        enabled: true
    
    rate_limits:
      - window: ""1m""
        max_requests: 100
        scope: ""per_tenant""
      - window: ""1h""
        max_requests: 1000
        scope: ""global""
";
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempFilePath))
                {
                    File.Delete(_tempFilePath);
                }
                _fileProvider.Dispose();
                _serviceProvider.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
