using Emma.Api.Interfaces;
using Emma.Api.Services;
using Azure;
using Azure.AI.OpenAI;
using Emma.Core.Config;
using Emma.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Emma.Api.Auth;
using Emma.Core.Services;
using Emma.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Emma.Infrastructure.Data;

DotNetEnv.Env.Load("../../.env");

var builder = WebApplication.CreateBuilder(args);

// Diagnostics: print Npgsql assembly version and path
var npgsqlAsm = typeof(NpgsqlConnection).Assembly;
Console.WriteLine($"[Npgsql] Version: {npgsqlAsm.GetName().Version}, Location: {npgsqlAsm.Location}");

// SPRINT1+2: Enable CORS for local frontend (Next.js dev on 3000 or 4000)
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy  =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:4000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add services to the container.

// Use Infrastructure DI (DbContext + repositories)
builder.Services.AddEmmaDatabase(builder.Configuration, isDevelopment: builder.Environment.IsDevelopment());

builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// Azure OpenAI configuration and client
builder.Services.Configure<AzureOpenAIConfig>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.AddSingleton<OpenAIClient>(sp =>
{
    var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureOpenAIConfig>>().Value;
    return new OpenAIClient(new Uri(cfg.Endpoint), new AzureKeyCredential(cfg.ApiKey));
});
builder.Services.AddScoped<IEmmaAgentService, EmmaAgentService>();

// SPRINT2: Analysis services and queue
builder.Services.AddHttpClient("azure-openai");
builder.Services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
builder.Services.AddHostedService<AnalysisQueueWorker>();
builder.Services.AddScoped<IEmmaAnalysisService, EmmaAnalysisService>();

// SPRINT2: AuthZ policies and email sender
builder.Services.AddEmmaAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, VerifiedUserHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedUser", p => p.AddRequirements(new VerifiedUserRequirement()));
});
builder.Services.AddScoped<IEmailSender, EmailSenderDev>();

// SPRINT1: Modular CRM integration registration
var selectedCrm = builder.Configuration["SelectedCrmIntegration"] ?? Environment.GetEnvironmentVariable("SELECTED_CRM_INTEGRATION") ?? "none";
if (selectedCrm.ToLower() == "fub")
{
    // builder.Services.AddScoped<Emma.Core.Interfaces.Crm.ICrmIntegration, Emma.Crm.Integrations.Fub.FubIntegrationService>();
    Console.WriteLine("[SPRINT1] (FUB CRM integration registration skipped - Emma.Crm project not present)");
}
else
{
    // No CRM integration registered
    Console.WriteLine("[SPRINT1] No CRM integration registered (core onboarding will function independently)");
}

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

// SPRINT1: Enable CORS for local frontend
app.UseCors(MyAllowSpecificOrigins);

// app.UseHttpsRedirection(); // TODO: Uncomment for Sprint 2 (real authentication)
// app.UseAuthentication(); // TODO: Uncomment for Sprint 2 (real authentication)
app.UseAuthorization();

app.MapControllers();
app.Run();
