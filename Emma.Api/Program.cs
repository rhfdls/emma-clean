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

using System.Net;

// Load environment variables from .env
Env.Load();
// Debug: Check Cosmos DB env vars after Env.Load()
Console.WriteLine("[DEBUG] After Env.Load():");
Console.WriteLine($"[DEBUG] COSMOSDB__ACCOUNTENDPOINT: {Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT")}");
Console.WriteLine($"[DEBUG] COSMOSDB__ACCOUNTKEY: {Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY")}");

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
// Debug: Check Cosmos DB config after CreateBuilder
Console.WriteLine("[DEBUG] After CreateBuilder:");
Console.WriteLine($"[DEBUG] From builder.Configuration: Endpoint={builder.Configuration["COSMOSDB__ACCOUNTENDPOINT"]}, Key={builder.Configuration["COSMOSDB__ACCOUNTKEY"]}");
Console.WriteLine($"[DEBUG] config[\"CosmosDb:AccountEndpoint\"]: {builder.Configuration["CosmosDb:AccountEndpoint"]}");
Console.WriteLine($"[DEBUG] config[\"CosmosDb:AccountKey\"]: {builder.Configuration["CosmosDb:AccountKey"]}");

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add HTTP client factory
builder.Services.AddHttpClient();

// Register agent services with proper scoping
builder.Services.AddScoped<IEmailAgent, EmailAgentStub>();
builder.Services.AddScoped<ISchedulerAgent, SchedulerAgentStub>();

// Configure Azure OpenAI with validation
builder.Services.AddAzureOpenAI(builder.Configuration);

// === ENV CASE VALIDATOR: Diagnosing environment variable case issues ===
try
{
    Emma.EnvCaseValidator.EnvCaseValidator.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[EnvCaseValidator] Error: {ex.Message}");
}
// === END ENV CASE VALIDATOR ===

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

// Register CosmosClient for CosmosDB
builder.Services.AddSingleton(s =>
{
    var config = s.GetRequiredService<IConfiguration>();
    var cosmosDbConfig = config.GetSection("CosmosDb").Get<CosmosDbConfig>();
    var endpoint = cosmosDbConfig.AccountEndpoint;
    var key = cosmosDbConfig.AccountKey;
    Console.WriteLine($"[DEBUG] Cosmos Endpoint: {endpoint}, Key: {(string.IsNullOrEmpty(key) ? "EMPTY" : "SET")}");
    return new Microsoft.Azure.Cosmos.CosmosClient(endpoint, key);
});

// Register CosmosAgentRepository for agent and controller use
builder.Services.AddScoped<CosmosAgentRepository>(s =>
{
    var config = s.GetRequiredService<IConfiguration>();
    var db = config["CosmosDb:DatabaseName"];
    var container = config["CosmosDb:ContainerName"];
    var client = s.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>();
    var logger = s.GetRequiredService<ILogger<CosmosAgentRepository>>();
    return new CosmosAgentRepository(client, db, container, logger);
});


// Register FulltextInteractionService
builder.Services.AddScoped<FulltextInteractionService>();

// Add logging (minimum viable observability)
// Configure console logger to use JSON output for structured logs
builder.Services.AddLogging(configure =>
{
    configure.AddJsonConsole(); // Requires Microsoft.Extensions.Logging.Json
    configure.AddDebug();
});

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


// Register the environment validator service
builder.Services.AddSingleton<EnvironmentValidator>();

// MVP/DEV-ONLY: AllowAll authentication for local development. DO NOT USE IN PRODUCTION!
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("AllowAll")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AllowAllAuthenticationHandler>("AllowAll", null);
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;
    });
}
else
{
    // TODO: Replace with real authentication for production (e.g. JWT Bearer)
    // builder.Services.AddAuthentication(...).AddJwtBearer(...);
}

var app = builder.Build();

// Run environment validation, but use a hybrid approach during transition period
// This logs warnings but doesn't stop startup if variables are missing
try
{
    var validator = app.Services.GetRequiredService<EnvironmentValidator>();
    validator.ValidateRequiredVariables(); // Modified to log warnings instead of throwing exceptions
    validator.ValidateOptionalVariables();
    validator.CheckForConflicts();
    
    // Log a transition message explaining the hybrid approach
    app.Logger.LogInformation("TRANSITION NOTICE: The Emma AI Platform is using a hybrid configuration approach.");
    app.Logger.LogInformation("Values can come from both docker-compose.yml and .env/.env.local files.");
    app.Logger.LogInformation("For improved security, migrate all secrets to .env.local (not in version control).");
}
catch (Exception ex)
{
    // This should only happen for unexpected errors, not missing variables
    Console.Error.WriteLine($"WARNING: Environment validation error: {ex.Message}");
    app.Logger.LogWarning(ex, "Environment validation encountered an error, but startup will continue");
}

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
app.UseAuthentication(); // MVP/DEV-ONLY: AllowAll authentication for local/dev
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

// For integration testing with WebApplicationFactory<Program>
public partial class Program { }
