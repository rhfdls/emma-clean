using System.Threading.Tasks;
using Emma.Api.IntegrationTests.TestHelpers;
using Emma.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Emma.Api.IntegrationTests
{
    /// <summary>
    /// Integration test to verify CosmosDB connectivity for the Emma AI Platform.
    /// Fails fast if CosmosDB is not reachable or misconfigured.
    /// </summary>
    public class CosmosDbConnectivityTests : IClassFixture<CosmosDbFixture>
    {
        private readonly CosmosAgentRepository _cosmosRepo;

        public CosmosDbConnectivityTests(CosmosDbFixture fixture)
        {
            _cosmosRepo = fixture.CosmosRepo;
        }

        [Fact(DisplayName = "CosmosDB should be reachable for integration tests")]
        public async Task CosmosDb_ShouldBeReachable()
        {
            Assert.True(await TestHelper.CanConnectToCosmosDb(_cosmosRepo), "CosmosDB is not reachable for integration tests. Check your environment variables, network, and Azure Portal settings.");
        }
    }

    /// <summary>
    /// Test fixture for providing a CosmosAgentRepository instance via DI.
    /// </summary>
    public class CosmosDbFixture
    {
        public CosmosAgentRepository CosmosRepo { get; }

        public CosmosDbFixture()
        {
            // Setup DI for test environment
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<CosmosAgentRepository>();
            // Add any required configuration or mocks here
            var provider = services.BuildServiceProvider();
            CosmosRepo = provider.GetRequiredService<CosmosAgentRepository>();
        }
    }
}
