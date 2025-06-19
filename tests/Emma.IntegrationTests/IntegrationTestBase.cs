using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Emma.Models.Interfaces;
using Emma.Data;

namespace Emma.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    protected readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly AppDbContext _dbContext;
    protected readonly IServiceScope _scope;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        _scope = factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure the database is created and seeded
        _dbContext.Database.EnsureCreated();
        TestDataSeeder.SeedTestData(_dbContext).Wait();
    }

    public void Dispose()
    {
        _client.Dispose();
        _dbContext.Database.EnsureDeleted();
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
    
    protected async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(
            content, 
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
    }
}
