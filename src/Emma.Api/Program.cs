using Emma.Api.Interfaces;
using Emma.Api.Services;
using Emma.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Emma.Api.Auth;
using Emma.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Emma.Infrastructure.Data;
using Emma.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Emma.Api.Infrastructure;
using Emma.Api.Infrastructure.Swagger;

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
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Add services to the container.

// Use Infrastructure DI (DbContext + repositories)
builder.Services.AddEmmaDatabase(builder.Configuration, isDevelopment: builder.Environment.IsDevelopment());

builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// Azure OpenAI disabled in this repo to avoid dependency on preview SDK
Console.WriteLine("[AzureOpenAI] Disabled (client not registered in this build)");

// SPRINT2: Analysis services and queue (disabled - implementations not present in this repo)
// builder.Services.AddHttpClient("azure-openai");
// builder.Services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
// builder.Services.AddHostedService<AnalysisQueueWorker>();
// builder.Services.AddScoped<IEmmaAnalysisService, EmmaAnalysisService>();

// SPRINT2: AuthZ policies and email sender
builder.Services.AddEmmaAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, VerifiedUserHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedUser", p => p.AddRequirements(new VerifiedUserRequirement()));
});
builder.Services.AddScoped<IEmailSender, EmailSenderDev>();

// Development-only JWT authentication
if (builder.Environment.IsDevelopment())
{
    var jwtIssuer = builder.Configuration["Jwt:Issuer"];
    var jwtAudience = builder.Configuration["Jwt:Audience"];
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (!string.IsNullOrWhiteSpace(jwtIssuer) && !string.IsNullOrWhiteSpace(jwtAudience) && !string.IsNullOrWhiteSpace(jwtKey))
    {
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };
            });
    }
}

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

    // Add global ProblemDetails examples for common error responses
    options.OperationFilter<ProblemDetailsResponsesOperationFilter>();
});

// Standardized problem details for errors
builder.Services.AddProblemDetails(options =>
{
    // Always include traceId and preserve stable type URIs from controllers/ProblemFactory
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = ctx.HttpContext.TraceIdentifier;
        if (!ctx.ProblemDetails.Extensions.ContainsKey("traceId"))
        {
            ctx.ProblemDetails.Extensions["traceId"] = traceId;
        }
    };
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

// Global exception handler -> RFC7807 ProblemDetails with traceId/type
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        var isDev = app.Environment.IsDevelopment();
        var detail = isDev ? ex?.ToString() ?? "An error occurred." : "An unexpected error occurred.";
        var problem = ProblemFactory.Create(context, StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            detail: detail,
            type: ProblemFactory.InternalError);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

// Explicit routing + dev-only middleware order: UseRouting -> UseCors -> UseAuthN -> UseAuthZ -> MapControllers
app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseCors(MyAllowSpecificOrigins);
    app.UseAuthentication();
}
app.UseAuthorization();
app.MapControllers();
app.Run();
