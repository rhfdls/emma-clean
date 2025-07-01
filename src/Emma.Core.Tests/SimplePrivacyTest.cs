using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Emma.Core.Extensions;
using Emma.Core.Config;
using Emma.Core.Services;
using Emma.Core.Interfaces;

namespace Emma.Core.Tests
{
    public class SimplePrivacyTest
    {
        [Fact]
        public void PrivacyService_ShouldRegisterCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Privacy:DefaultMaskingLevel"] = "Partial",
                    ["Privacy:EnableAuditLogging"] = "true",
                    ["Privacy:EnableJwtAuthentication"] = "false"
                })
                .Build();

            // Act
            services.AddEmmaPrivacyServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var privacySettings = serviceProvider.GetRequiredService<IOptions<PrivacySettings>>();
            var maskingService = serviceProvider.GetRequiredService<IDataMaskingService>();

            Assert.NotNull(privacySettings);
            Assert.NotNull(maskingService);
            Assert.Equal(MaskingLevel.Partial, privacySettings.Value.DefaultMaskingLevel);
            Assert.True(privacySettings.Value.EnableAuditLogging);
            Assert.False(privacySettings.Value.EnableJwtAuthentication);
        }

        [Fact]
        public void DataMaskingService_ShouldMaskEmail()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Privacy:DefaultMaskingLevel"] = "Partial"
                })
                .Build();

            services.AddEmmaPrivacyServices(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var maskingService = serviceProvider.GetRequiredService<IDataMaskingService>();

            // Act
            var result = maskingService.MaskEmail("test@example.com");

            // Assert
            Assert.NotEqual("test@example.com", result);
            Assert.Contains("*", result);
        }
    }
}
