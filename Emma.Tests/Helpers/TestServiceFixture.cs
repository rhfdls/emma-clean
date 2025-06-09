using Emma.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emma.Tests.Helpers
{
    /// <summary>
    /// Test fixture for setting up dependency injection container for integration tests
    /// </summary>
    public class TestServiceFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        private bool _disposed = false;

        public TestServiceFixture()
        {
            var services = new ServiceCollection();
            var configuration = BuildConfiguration();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add EMMA services for development/testing
            services.AddEmmaCoreServicesForDevelopment(configuration);
            services.AddEmmaAgentServicesForDevelopment();

            ServiceProvider = services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
