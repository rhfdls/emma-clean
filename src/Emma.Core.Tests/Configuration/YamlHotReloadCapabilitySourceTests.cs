using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using Emma.Core.Configuration;
using FluentAssertions;

namespace Emma.Core.Tests.Configuration
{
    public class YamlHotReloadCapabilitySourceTests : IDisposable
    {
        private readonly Mock<IFileProvider> _fileProviderMock;
        private readonly Mock<IOptionsMonitor<YamlAgentCapabilitySourceOptions>> _optionsMonitorMock;
        private readonly Mock<ILogger<YamlHotReloadCapabilitySource>> _loggerMock;
        private readonly TestChangeToken _changeToken;
        private readonly YamlAgentCapabilitySourceOptions _options;
        private readonly string _testYaml = @"
agents:
  NbaAgent:
    capabilities:
      - name: 'suggest:action'
        description: 'Suggest next best actions for a conversation'
        enabled: true
        validation_rules:
          max_suggestions: 5
          allowed_contexts: ['sales', 'support']
";

        public YamlHotReloadCapabilitySourceTests()
        {
            _fileProviderMock = new Mock<IFileProvider>();
            _optionsMonitorMock = new Mock<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>();
            _loggerMock = new Mock<ILogger<YamlHotReloadCapabilitySource>>();
            _changeToken = new TestChangeToken();
            _options = new YamlAgentCapabilitySourceOptions
            {
                FilePath = "test.yml",
                ValidateSchema = true,
                ThrowOnFileNotFound = true
            };

            var fileInfo = new TestFileInfo("test.yml", _testYaml);
            _fileProviderMock.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns(fileInfo);
            _fileProviderMock.Setup(x => x.Watch(It.IsAny<string>())).Returns(_changeToken);
            
            _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        }

        public void Dispose()
        {
            // Clean up test resources if needed
        }

        [Fact]
        public async Task LoadCapabilitiesAsync_WhenFileExists_ReturnsParsedCapabilities()
        {
            // Arrange
            var source = CreateSource();

            // Act
            var capabilities = await source.LoadCapabilitiesAsync();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.Agents.Should().ContainKey("NbaAgent");
            capabilities.Agents["NbaAgent"].Capabilities.Should().HaveCount(1);
            capabilities.Agents["NbaAgent"].Capabilities[0].Name.Should().Be("suggest:action");
        }

        [Fact]
        public void LoadCapabilitiesAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            _options.ThrowOnFileNotFound = true;
            _fileProviderMock.Setup(x => x.GetFileInfo(It.IsAny<string>()))
                .Returns(new TestFileInfo("nonexistent.yml", string.Empty, false));

            var source = CreateSource();

            // Act & Assert
            source.Invoking(async x => await x.LoadCapabilitiesAsync())
                .Should().ThrowAsync<FileNotFoundException>();
        }

        [Fact]
        public async Task LoadCapabilitiesAsync_WhenFileChanges_ReloadsCapabilities()
        {
            // Arrange
            var source = CreateSource();
            var initialCapabilities = await source.LoadCapabilitiesAsync();
            
            // Update the file content
            var updatedYaml = @"
agents:
  NbaAgent:
    capabilities:
      - name: 'analyze:sentiment'
        description: 'Analyze sentiment of conversation'
        enabled: true
";
            _fileProviderMock.Setup(x => x.GetFileInfo(It.IsAny<string>()))
                .Returns(new TestFileInfo("test.yml", updatedYaml));

            // Trigger change notification
            _changeToken.HasChanged = true;
            _changeToken.RaiseChangeCallback();

            // Small delay to allow async processing
            await Task.Delay(100);

            // Act
            var updatedCapabilities = await source.LoadCapabilitiesAsync();

            // Assert
            updatedCapabilities.Should().NotBeSameAs(initialCapabilities);
            updatedCapabilities.Agents["NbaAgent"].Capabilities.Should().Contain(
                x => x.Name == "analyze:sentiment");
        }

        [Fact]
        public async Task LoadCapabilitiesAsync_WhenMultipleThreadsAccess_ReturnsConsistentState()
        {
            // Arrange
            var source = CreateSource();
            var testCompleted = false;
            var tasks = new List<Task>();
            var results = new ConcurrentBag<AgentCapabilityYaml>();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        while (!testCompleted)
                        {
                            var capabilities = await source.LoadCapabilitiesAsync();
                            results.Add(capabilities);
                            await Task.Delay(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }));
            }

            // Trigger a few changes
            for (int i = 0; i < 3; i++)
            {
                _changeToken.HasChanged = true;
                _changeToken.RaiseChangeCallback();
                await Task.Delay(50);
            }

            // Let the tasks run for a bit
            await Task.Delay(200);
            testCompleted = true;
            await Task.WhenAll(tasks);

            // Assert
            exceptions.Should().BeEmpty("No exceptions should be thrown during concurrent access");
            results.Should().NotBeEmpty("Should have collected some capability results");
            
            // All results should be valid (either the initial or updated state)
            foreach (var result in results)
            {
                result.Should().NotBeNull();
                result.Agents.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task CapabilitiesReloaded_WhenFileChanges_EventIsRaised()
        {
            // Arrange
            var source = CreateSource();
            var eventRaised = false;
            source.CapabilitiesReloaded += (sender, args) =>
            {
                eventRaised = true;
                args.Success.Should().BeTrue();
                args.FilePath.Should().Be(_options.FilePath);
                return Task.CompletedTask;
            };

            // Initial load
            await source.LoadCapabilitiesAsync();

            // Act - Trigger change
            _changeToken.HasChanged = true;
            _changeToken.RaiseChangeCallback();

            // Small delay to allow async processing
            await Task.Delay(100);

            // Assert
            eventRaised.Should().BeTrue("CapabilitiesReloaded event should be raised");
        }

        [Fact]
        public async Task LoadCapabilitiesAsync_WhenInvalidYaml_LogsErrorAndReturnsEmptyCapabilities()
        {
            // Arrange
            const string invalidYaml = "invalid: yaml: : : : : : : ";
            _fileProviderMock.Setup(x => x.GetFileInfo(It.IsAny<string>()))
                .Returns(new TestFileInfo("test.yml", invalidYaml));
            
            var source = CreateSource();
            var eventRaised = false;
            source.CapabilitiesReloaded += (sender, args) =>
            {
                eventRaised = true;
                args.Success.Should().BeFalse();
                args.Error.Should().NotBeNull();
                return Task.CompletedTask;
            };

            // Act
            var capabilities = await source.LoadCapabilitiesAsync();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.Agents.Should().BeEmpty("Should return empty capabilities on error");
            eventRaised.Should().BeTrue("CapabilitiesReloaded event should be raised with error");
            
            // Verify error was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        private YamlHotReloadCapabilitySource CreateSource()
        {
            return new YamlHotReloadCapabilitySource(
                _fileProviderMock.Object,
                _optionsMonitorMock.Object,
                _loggerMock.Object);
        }

        private class TestChangeToken : IChangeToken
        {
            private List<(Action<object>, object)> _callbacks = new();

            public bool HasChanged { get; set; }
            public bool ActiveChangeCallbacks => true;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _callbacks.Add((callback, state));
                return new DisposableAction(() => _callbacks.Remove((callback, state)));
            }

            public void RaiseChangeCallback()
            {
                foreach (var (callback, state) in _callbacks.ToList())
                {
                    callback(state);
                }
            }
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }

        private class TestFileInfo : IFileInfo
        {
            private readonly string _content;

            public TestFileInfo(string name, string content, bool exists = true)
            {
                Name = name;
                _content = content;
                Exists = exists;
                LastModified = DateTimeOffset.UtcNow;
            }

            public Stream CreateReadStream()
            {
                if (!Exists)
                    throw new FileNotFoundException();
                    
                return new MemoryStream(Encoding.UTF8.GetBytes(_content));
            }

            public bool Exists { get; }
            public bool IsDirectory => false;
            public DateTimeOffset LastModified { get; }
            public long Length => Exists ? _content.Length : -1;
            public string Name { get; }
            public string? PhysicalPath => null;
        }
    }
}
