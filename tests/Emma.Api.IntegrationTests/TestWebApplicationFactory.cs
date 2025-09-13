using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Emma.Api.IntegrationTests
{
    /// <summary>
    /// Minimal WebApplicationFactory for integration tests.
    /// Ensures Development environment and allows service overrides if needed.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);

            builder.ConfigureServices(services =>
            {
                // Optionally override services for tests here.
                // Example: replace external clients with fakes/mocks using services.Remove()/AddSingleton()
            });
        }
    }
}
