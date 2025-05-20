using Emma.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

// Load environment variables from .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Register EmmaAgentService and agent stubs
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Emma.Api.Services.IEmailAgent, Emma.Api.Services.EmailAgentStub>();
builder.Services.AddSingleton<Emma.Api.Services.ISchedulerAgent, Emma.Api.Services.SchedulerAgentStub>();
builder.Services.AddSingleton<Emma.Api.Services.IEmmaAgentService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var emailAgent = sp.GetRequiredService<Emma.Api.Services.IEmailAgent>();
    var schedulerAgent = sp.GetRequiredService<Emma.Api.Services.ISchedulerAgent>();
    var logger = sp.GetRequiredService<ILogger<Emma.Api.Services.EmmaAgentService>>();
    var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "YOUR_OPENAI_API_KEY";
    return new Emma.Api.Services.EmmaAgentService(httpClient, emailAgent, schedulerAgent, logger, openAiApiKey);
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
builder.Services.AddSwaggerGen();
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine("Applying database migrations...");
        db.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
        
        // Seed data
        Console.WriteLine("Seeding data...");
        SeedData.EnsureSeeded(db);
        Console.WriteLine("Data seeding completed.");
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
