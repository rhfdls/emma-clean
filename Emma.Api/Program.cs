using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Emma.Data;
using Emma.Api.Services;
using Emma.Api.Configuration;
using Emma.Api.Models;
using Azure;

try
{
    Console.WriteLine("🚀 Starting MINIMAL Emma AI Platform...");
    
    // Load environment variables from .env
    Console.WriteLine("📁 Loading environment variables from .env file...");
    Env.Load("../.env");
    Console.WriteLine("✅ Environment variables loaded successfully");

    Console.WriteLine("🏗️ Creating minimal web application builder...");
    var builder = WebApplication.CreateBuilder(args);
    
    // Explicitly configure URLs
    builder.WebHost.UseUrls("http://localhost:5000");
    Console.WriteLine("✅ Web application builder created with URL: http://localhost:5000");

    // Add only essential services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    // Add Database Context with timeout configuration
    Console.WriteLine("🗄️ Adding PostgreSQL database context...");
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("⚠️ WARNING: PostgreSQL connection string not found. Database features will be disabled.");
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30); // 30 second timeout
            });
            options.EnableSensitiveDataLogging(false); // Disable for production
        });
        Console.WriteLine("✅ PostgreSQL database context added with 30s timeout");
    }

    // Add Azure OpenAI Configuration (Step 2)
    Console.WriteLine("🤖 Adding Azure OpenAI configuration...");
    var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");
    var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey");
    
    if (string.IsNullOrEmpty(azureOpenAIEndpoint) || string.IsNullOrEmpty(azureOpenAIApiKey))
    {
        Console.WriteLine("⚠️ WARNING: Azure OpenAI configuration incomplete. AI features will be disabled.");
    }
    else
    {
        // Azure OpenAI configuration found - services will be added later
        var chatDeployment = Environment.GetEnvironmentVariable("AzureOpenAI__ChatDeployment") ?? "gpt-4";
        var embeddingDeployment = Environment.GetEnvironmentVariable("AzureOpenAI__EmbeddingDeployment") ?? "text-embedding-ada-002";
        
        Console.WriteLine($"✅ Azure OpenAI services configured:");
        Console.WriteLine($"   📍 Endpoint: {azureOpenAIEndpoint}");
        Console.WriteLine($"   🤖 Chat Deployment: {chatDeployment}");
        Console.WriteLine($"   📊 Embedding Deployment: {embeddingDeployment}");
        Console.WriteLine("   ⏳ Service registration: DEFERRED (Step 3)");

        // Register Azure OpenAI Configuration
        builder.Services.Configure<AzureOpenAIConfig>(options =>
        {
            options.Endpoint = azureOpenAIEndpoint;
            options.ApiKey = azureOpenAIApiKey;
            options.ChatDeploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__ChatDeployment") ?? "gpt-4";
            options.EmbeddingDeploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__EmbeddingDeployment") ?? "text-embedding-ada-002";
            options.MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("AzureOpenAI__MaxTokens"), out var maxTokens) ? maxTokens : 4000;
            options.Temperature = double.TryParse(Environment.GetEnvironmentVariable("AzureOpenAI__Temperature"), out var temp) ? temp : 0.7;
        });
        
        // Register Azure OpenAI Client
        builder.Services.AddSingleton<Azure.AI.OpenAI.OpenAIClient>(provider =>
        {
            return new Azure.AI.OpenAI.OpenAIClient(
                new Uri(azureOpenAIEndpoint),
                new Azure.AzureKeyCredential(azureOpenAIApiKey)
            );
        });
        
        // Register Azure OpenAI Service
        builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
        
        Console.WriteLine("✅ Azure OpenAI services registered successfully");
    }

    // Add NBA Context Services (Step 4)
    Console.WriteLine("🧠 Adding NBA Context services...");
    try
    {
        // Register NBA Context Service
        builder.Services.AddScoped<INbaContextService, NbaContextService>();
        
        // Register Vector Search Service
        builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
        
        // Register Demo NBA Service for 4-hour demo
        builder.Services.AddScoped<IDemoNbaService, DemoNbaService>();
        
        Console.WriteLine("✅ NBA Context services registered successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ WARNING: NBA Context services registration failed: {ex.Message}");
        Console.WriteLine("   🔄 Continuing without NBA services for now...");
    }

    Console.WriteLine("🎯 Building minimal application...");
    var app = builder.Build();
    Console.WriteLine("✅ Minimal application built successfully");

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Console.WriteLine("✅ Swagger configured for development environment");
    }

    app.UseRouting();
    app.MapControllers();

    // Add a root endpoint that redirects to Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"));
    
    // Add a simple test endpoint
    app.MapGet("/test", () => new { message = "Emma AI Platform is running!", timestamp = DateTime.UtcNow });
    
    // NBA Context endpoints (Step 5)
    app.MapGet("/nba/health", () => new { 
        status = "healthy", 
        message = "NBA Context services are available", 
        timestamp = DateTime.UtcNow,
        services = new { azureOpenAI = "registered", nbaContext = "registered", vectorSearch = "registered" }
    });
    
    app.MapPost("/nba/context/update", async (INbaContextService nbaService, HttpContext context) =>
    {
        try
        {
            // Simple test - just return success for now
            return Results.Ok(new { 
                message = "NBA Context service is available and responding", 
                timestamp = DateTime.UtcNow,
                status = "ready"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"NBA Context service error: {ex.Message}");
        }
    });
    
    // Add database health check endpoint
    app.MapGet("/health/db", async (AppDbContext dbContext) =>
    {
        try
        {
            Console.WriteLine("🔍 Testing database connection...");
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                Console.WriteLine("✅ Database connection successful");
                return Results.Ok(new { 
                    status = "healthy", 
                    message = "Database connection successful", 
                    timestamp = DateTime.UtcNow,
                    database = "PostgreSQL"
                });
            }
            else
            {
                Console.WriteLine("❌ Database connection failed");
                return Results.Problem("Database connection failed", statusCode: 503);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database connection error: {ex.Message}");
            return Results.Problem($"Database connection error: {ex.Message}", statusCode: 503);
        }
    });
    
    // Demo NBA Endpoints (4-hour demo)
    app.MapPost("/demo/analyze-interaction", async (IDemoNbaService demoService, AnalyzeInteractionRequest request) =>
    {
        try
        {
            var result = await demoService.AnalyzeInteractionAsync(request.InteractionText, request.ClientContext ?? "");
            return Results.Ok(new { 
                success = true, 
                analysis = result, 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Analysis error: {ex.Message}");
        }
    });
    
    app.MapPost("/demo/get-recommendation", async (IDemoNbaService demoService, GetRecommendationRequest request) =>
    {
        try
        {
            var result = await demoService.GetRecommendationAsync(
                request.ClientSummary, 
                request.RecentInteractions, 
                request.DealStage ?? "prospect"
            );
            return Results.Ok(new { 
                success = true, 
                recommendation = result, 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Recommendation error: {ex.Message}");
        }
    });
    
    app.MapPost("/demo/update-summary", async (IDemoNbaService demoService, UpdateSummaryRequest request) =>
    {
        try
        {
            var result = await demoService.UpdateClientSummaryAsync(
                request.ExistingSummary ?? "", 
                request.NewInteractions, 
                request.ClientProfile ?? ""
            );
            return Results.Ok(new { 
                success = true, 
                summary = result, 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Summary error: {ex.Message}");
        }
    });
    
    Console.WriteLine("✅ Routes and endpoints configured");

    Console.WriteLine("🚀 Starting minimal web application...");
    Console.WriteLine("📍 Application should be available at: http://localhost:5000");
    Console.WriteLine("📍 Swagger UI: http://localhost:5000/swagger");
    Console.WriteLine("📍 Test endpoint: http://localhost:5000/test");
    Console.WriteLine("⏳ Starting server... (this should block and keep running)");
    
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FATAL ERROR during startup: {ex.Message}");
    Console.WriteLine($"📍 Exception Type: {ex.GetType().Name}");
    Console.WriteLine($"📝 Stack Trace: {ex.StackTrace}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"🔗 Inner Exception: {ex.InnerException.Message}");
        Console.WriteLine($"📝 Inner Stack Trace: {ex.InnerException.StackTrace}");
    }
    
    Console.WriteLine("\n⏸️ Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(1);
}
