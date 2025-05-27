using System;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Azure.AI.OpenAI;
using Emma.Core.Config;
using Emma.Core.Interfaces;  // For IEmmaAgentService
using Emma.Api.Services;
using Emma.Api;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Emma.Data;
using Emma.Api.Middleware;
using Emma.Api.Config;  // For AzureOpenAIServiceExtensions
using Npgsql;

// Load environment variables from .env
Env.Load();

// DEBUG: Print all environment variables at startup
foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
{
    Console.WriteLine($"[ENV] {de.Key}={de.Value}");
}

// Fail-fast check for required Cosmos DB environment variables
void ValidateCosmosDbEnvVars()
{
    var required = new[] {
        "COSMOSDB__ACCOUNTENDPOINT",
        "COSMOSDB__ACCOUNTKEY",
        "COSMOSDB__DATABASENAME",
        "COSMOSDB__CONTAINERNAME"
    };
    foreach (var key in required)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            throw new Exception($"Missing required Cosmos DB environment variable: {key}. Please check your .env or deployment secrets.");
    }
}
ValidateCosmosDbEnvVars();

var builder = WebApplication.CreateBuilder(args);

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add HTTP client factory
builder.Services.AddHttpClient();

// Register agent services with proper scoping
builder.Services.AddScoped<IEmailAgent, EmailAgentStub>();
builder.Services.AddScoped<ISchedulerAgent, SchedulerAgentStub>();

// Configure Azure OpenAI with validation
builder.Services.AddAzureOpenAI(builder.Configuration);

// Register EmmaAgentService with the correct interface and dependencies
builder.Services.AddScoped<IEmmaAgentService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<EmmaAgentService>>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var openAIClient = provider.GetRequiredService<OpenAIClient>();
    var config = provider.GetRequiredService<IOptions<AzureOpenAIConfig>>();
    return new EmmaAgentService(
        logger, 
        httpContextAccessor, 
        openAIClient,
        config);
});

// Register Cosmos DB integration
builder.Services.AddCosmosDb(builder.Configuration);

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole().AddDebug());

// Register Azure AI Foundry configuration
builder.Services.Configure<AzureAIFoundryConfig>(
    builder.Configuration.GetSection("AzureAIFoundry"));

// Register AI Foundry Service
builder.Services.AddScoped<IAIFoundryService, AIFoundryService>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Emma API", Version = "v1" });
});

// Configure JSON serialization for controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));

// Register NpgsqlConnection for DI (for HealthController)
builder.Services.AddScoped<Npgsql.NpgsqlConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("PostgreSql");
    return new Npgsql.NpgsqlConnection(connStr);
});

// Add CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



// Enable Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Console.WriteLine("Applying database migrations...");
        // db.Database.Migrate();
        // Console.WriteLine("Database migrations applied successfully.");
        
        // // Seed data
        // Console.WriteLine("Seeding data...");
        // SeedData.EnsureSeeded(db);
        // Console.WriteLine("Data seeding completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating or seeding the database: {ex.Message}");
        throw; // Fail fast if we can't set up the database
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use HTTP instead of HTTPS
app.UseRouting();
app.UseCors(); // Enable CORS for frontend
app.UseAuthorization();
app.MapControllers();

// Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Emma API V1");
    });
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
