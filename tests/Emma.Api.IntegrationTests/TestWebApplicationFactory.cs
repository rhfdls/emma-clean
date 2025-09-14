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
            // Ensure Development environment and set JWT env vars before host builds
            Environment.SetEnvironmentVariable("ALLOW_DEV_AUTOPROVISION", "true");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "emma-dev");
            Environment.SetEnvironmentVariable("Jwt__Audience", "emma-dev");
            Environment.SetEnvironmentVariable("Jwt__Key", "supersecret_dev_jwt_key_please_change");
            builder.UseEnvironment(Environments.Development);

            builder.ConfigureServices(services =>
            {
                // Optionally override services for tests here.
                // Example: replace external clients with fakes/mocks using services.Remove()/AddSingleton()
            });
        }
    }
}
