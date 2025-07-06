using Emma.Api.Interfaces;
using Emma.Api.Services;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load("../../.env");
Console.WriteLine("COSMOSDB__ACCOUNTKEY: " + Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY"));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[DEBUG] Loaded DefaultConnection: {connString}");
builder.Services.AddDbContext<Emma.Data.AppDbContext>(options =>
    options.UseNpgsql(connString));
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EMMA API",
        Version = "v1",
        Description = "API for EMMA platform including dynamic enum management."
    });

});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMMA API v1");
    c.RoutePrefix = string.Empty;
});

// app.UseHttpsRedirection(); // TODO: Uncomment for Sprint 2 (real authentication)
// app.UseAuthentication(); // TODO: Uncomment for Sprint 2 (real authentication)

app.MapControllers();
app.Run();
