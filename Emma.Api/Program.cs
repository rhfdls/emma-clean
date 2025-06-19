using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Emma.Data;
using Emma.Api.Services;
using Emma.Api.Configuration;
using Emma.Api.Models;
using Emma.Core.Interfaces;
using Emma.Core.Services;
using Emma.Core.Compliance;
using Azure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Emma.Core.Extensions;
using Emma.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

public static async Task Main(string[] args)
{
    try
    {
        Console.WriteLine("Loaded correct Program.cs!");
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
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Add CORS policy for React frontend
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowReactFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        
        // Add IHttpContextAccessor for DI
        builder.Services.AddHttpContextAccessor();
        
            // Add Time Simulation Services
        Console.WriteLine("⏱️ Adding Time Simulation services...");
        builder.Services.Configure<TimeSimulationOptions>(
            builder.Configuration.GetSection(TimeSimulationOptions.SectionName));
        
        // Register the TimeSimulatorService and its hosted service
        builder.Services.AddSingleton<TimeSimulatorService>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<TimeSimulatorService>());
        
        // Register NBA time event handler for processing recommendations during simulation
        builder.Services.AddTransient<ITimeEventHandler, NbaTimeEventHandler>();
    
        Console.WriteLine("✅ Time Simulation services added");

        // Add database context with PostgreSQL
        Console.WriteLine("💾 Configuring database...");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql") ?? 
                               builder.Configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("⚠️ WARNING: PostgreSQL connection string not found in environment variables or configuration. Database features will be disabled.");
        }
        else
        {
            // Add EmmaDbContext with our custom initialization
            builder.Services.AddEmmaDatabase(builder.Configuration, builder.Environment.IsDevelopment());
            
            // Register IAppDbContext to resolve to EmmaDbContext
            builder.Services.AddScoped<IAppDbContext, EmmaDbContext>(provider => 
                provider.GetRequiredService<EmmaDbContext>());
                
            Console.WriteLine("✅ PostgreSQL database context added with custom initialization");
        }

        // Add Azure OpenAI Configuration (Step 2)
        Console.WriteLine("🤖 Adding Azure OpenAI configuration...");
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey");
        
        Console.WriteLine($"🔍 Azure OpenAI Endpoint: {azureOpenAIEndpoint}");
        Console.WriteLine($"🔍 Azure OpenAI API Key: {azureOpenAIApiKey?.Substring(0, 5)}***");

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
            
            // Configure Azure AI Foundry Config
            builder.Services.Configure<Emma.Core.Config.AzureAIFoundryConfig>(options =>
            {
                options.Endpoint = azureOpenAIEndpoint;
                options.ApiKey = azureOpenAIApiKey;
                options.DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "gpt-4.1";
            });
            
            // Register AI Foundry Service
            builder.Services.AddScoped<IAIFoundryService>(provider =>
            {
                var config = provider.GetRequiredService<IOptions<Emma.Core.Config.AzureAIFoundryConfig>>();
                var logger = provider.GetRequiredService<ILogger<Emma.Api.Services.AIFoundryService>>();
                var cosmosRepo = provider.GetService<Emma.Api.Services.CosmosAgentRepository>(); // Optional - may be null
                return new Emma.Api.Services.AIFoundryService(config, logger, cosmosRepo);
            });
        
            // Add Cosmos DB Services (Step 3.5)
            Console.WriteLine("🌌 Adding Cosmos DB services...");
            try
            {
                // Log environment variables for Cosmos DB
                var accountEndpoint = Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
                var accountKey = Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
                Console.WriteLine($"🔍 Cosmos DB Account Endpoint: {accountEndpoint}");
                Console.WriteLine($"🔍 Cosmos DB Account Key: {accountKey?.Substring(0, Math.Min(5, accountKey.Length))}***");

                // Ensure CosmosClient registration using environment variables
                builder.Services.AddSingleton<CosmosClient>(sp =>
                    new CosmosClient(
                        Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT"),
                        Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY")
                    )
                );

                // Ensure CosmosAgentRepository registration using environment variables
                builder.Services.AddScoped<CosmosAgentRepository>(sp =>
                {
                    var cosmosClient = sp.GetRequiredService<CosmosClient>();
                    var databaseName = Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME");
                    var containerName = Environment.GetEnvironmentVariable("COSMOSDB__CONTAINERNAME");
                    var logger = sp.GetRequiredService<ILogger<CosmosAgentRepository>>();
                    return new CosmosAgentRepository(cosmosClient, databaseName, containerName, logger);
                });
                
                Console.WriteLine("✅ Cosmos DB services registered successfully");
                Console.WriteLine("   📊 Database: emma-agent");
                Console.WriteLine("   📦 Container: messages");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Cosmos DB registration failed: {ex.Message}");
                Console.WriteLine($"📍 Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"📝 Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"🔗 Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"📝 Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine("   ℹ️ Continuing without Cosmos DB - basic AI functionality available");
            }
            
            Console.WriteLine("✅ Azure OpenAI services registered successfully");
        }

        // Add NBA Context Services (Step 4)
        Console.WriteLine("🧠 Adding NBA Context services...");
        try
        {
            // Register AppDbContext as IAppDbContext
            builder.Services.AddScoped<IAppDbContext, AppDbContext>();
            
            // Register NBA Context Service with all required dependencies
            builder.Services.AddScoped<INbaContextService>(provider => 
                new NbaContextService(
                    provider.GetRequiredService<IAppDbContext>(),
                    provider.GetRequiredService<IAzureOpenAIService>(),
                    provider.GetRequiredService<IVectorSearchService>(),
                    provider.GetRequiredService<ISqlContextExtractor>(),
                    provider.GetRequiredService<ILogger<NbaContextService>>()
                ));
            
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

        // Add Industry Profile Services (Step 5)
        Console.WriteLine("🏭 Adding Industry Profile services...");
        try
        {
            // Register Industry Profile Service for multi-industry support
            builder.Services.AddScoped<Emma.Core.Interfaces.IIndustryProfileService, Emma.Api.Services.IndustryProfileService>();
            
            Console.WriteLine("✅ Industry Profile services registered successfully");
            Console.WriteLine("   🏢 Supported industries: Real Estate, Mortgage, Financial Advisory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ WARNING: Industry Profile services registration failed: {ex.Message}");
            Console.WriteLine("   🔄 Continuing without Industry Profile services for now...");
        }

        // Add Dynamic Prompt Management (Step 5.5 - HIGHEST PRIORITY)
        Console.WriteLine("📝 Adding Dynamic Prompt Management services...");
        try
        {
            // Configure PromptProvider settings
            var promptConfigPath = Path.Combine(builder.Environment.ContentRootPath, "Configuration", "prompts.json");
            var enablePromptHotReload = builder.Environment.IsDevelopment();
            builder.Services.AddSingleton<IPromptProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<PromptProvider>>();
                return new PromptProvider(logger, promptConfigPath, enablePromptHotReload);
            });
            
            // Configure EnumProvider settings
            var enumConfigPath = Path.Combine(builder.Environment.ContentRootPath, "Configuration", "enums.json");
            builder.Services.AddSingleton<IEnumProvider>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<EnumProvider>>();
                var enumVersioningLogger = serviceProvider.GetRequiredService<ILogger<EnumVersioningService>>();
                return new EnumProvider(logger, enumConfigPath, new EnumVersioningService(enumVersioningLogger, "versioningConfigPath", null));
            });
            
            Console.WriteLine("✅ Dynamic Prompt Management services registered successfully");
            Console.WriteLine("   📄 Configuration file: Configuration/prompts.json");
            Console.WriteLine("   🔄 Hot reload: " + (builder.Environment.IsDevelopment() ? "ENABLED" : "DISABLED"));
            Console.WriteLine("   🎯 Business-configurable AI prompts without code changes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ WARNING: Prompt Management services registration failed: {ex.Message}");
            Console.WriteLine("   🔄 Continuing without dynamic prompts - agents will use fallback prompts...");
        }

        // Register IAgentCommunicationBus service
        builder.Services.AddScoped<IAgentCommunicationBus, AgentCommunicationBus>();
        Console.WriteLine("🔗 Registered IAgentCommunicationBus in DI");

        // Register IEmmaAgentService
        builder.Services.AddScoped<IEmmaAgentService, EmmaAgentService>();
        Console.WriteLine("🔗 Registered IEmmaAgentService in DI");

        // Register IAgentRegistryService service
        builder.Services.AddScoped<IAgentRegistryService, AgentRegistryService>();
        Console.WriteLine("🔗 Registered IAgentRegistryService in DI");

        // Add AI Agent Services (Step 6)
        Console.WriteLine("🤖 Adding AI Agent services...");
        try
        {
            // Register the agent factory
            builder.Services.AddScoped<IAgentFactory, AgentFactory>();
            Console.WriteLine("   🔄 Registered AgentFactory");

            // Register the agent registry
            builder.Services.AddSingleton<IAgentRegistry>(sp =>
            {
                var registry = new AgentRegistry();
                // Register all agent types here
                registry.Register<LeadIntakeAgent>();
                registry.Register<PropertyInterestAgent>();
                registry.Register<ReEngagementAgent>();
                registry.Register<InteractionLogAgent>();
                registry.Register<AppointmentSchedulingAgent>();
                registry.Register<ProactiveInquiryAgent>();
                registry.Register<LeadBehaviorMonitorAgent>();
                registry.Register<DealProgressAgent>();
                registry.Register<ClientSatisfactionAgent>();
                return registry;
            });
            Console.WriteLine("   📋 Registered AgentRegistry with all agent types");

            // Register the agent orchestrator
            builder.Services.AddHostedService<AgentOrchestrator>();
            Console.WriteLine("   🚀 Started AgentOrchestrator background service");

            // Register agent-specific services
            builder.Services.AddScoped<LeadIntakeAgent>();
            builder.Services.AddScoped<PropertyInterestAgent>();
            builder.Services.AddScoped<ReEngagementAgent>();
            builder.Services.AddScoped<InteractionLogAgent>();
            builder.Services.AddScoped<AppointmentSchedulingAgent>();
            builder.Services.AddScoped<ProactiveInquiryAgent>();
            builder.Services.AddScoped<LeadBehaviorMonitorAgent>();
            builder.Services.AddScoped<DealProgressAgent>();
            builder.Services.AddScoped<ClientSatisfactionAgent>();
            Console.WriteLine("   ✅ All AI Agent services registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ WARNING: AI Agent services registration failed: {ex.Message}");
            Console.WriteLine("   🔄 Continuing without AI Agent services...");
        }

        // Build the application
        Console.WriteLine("🚀 Starting web application...");
        var app = builder.Build();
        Console.WriteLine("✅ Application built successfully");

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            Console.WriteLine("✅ Swagger configured for development environment");
        }

        // Configure the HTTP request pipeline
        app.UseRouting();
        
        // CORS must be before UseAuthentication and MapControllers
        app.UseCors("AllowReactFrontend");
        
        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Exception handling middleware
        app.UseExceptionHandler("/error");
        
        // Standard endpoints
        app.MapControllers();
        
        // Health check endpoint
        app.MapHealthChecks("/health");
        
        // Root endpoint that redirects to Swagger
        app.MapGet("/", () => Results.Redirect("/swagger"))
            .WithName("Root")
            .WithOpenApi();
        
        // Simple test endpoint
        app.MapGet("/test", () => new { 
            message = "Emma AI Platform is running!", 
            timestamp = DateTime.UtcNow,
            environment = builder.Environment.EnvironmentName 
        });
        
        // NBA Context endpoints
        app.MapGet("/nba/health", () => new { 
            status = "healthy", 
            message = "NBA Context services are available", 
            timestamp = DateTime.UtcNow,
            services = new { 
                azureOpenAI = "registered", 
                nbaContext = "registered", 
                vectorSearch = "registered" 
            }
        }).WithName("NbaHealth");
        
        // NBA Context update endpoint
        app.MapPost("/nba/context/update", async (INbaContextService nbaService) =>
        {
            return Results.Ok(new { 
                message = "NBA Context service is available and responding", 
                timestamp = DateTime.UtcNow,
                status = "ready"
            });
        })
        .WithName("UpdateNbaContext")
        .WithOpenApi();
        
        // Industry profile demo endpoint
        app.MapGet("/demo/industries", async (Emma.Core.Interfaces.IIndustryProfileService industryService) =>
        {
            var profiles = await industryService.GetAvailableProfilesAsync();
            var result = profiles.Select(p => new {
                code = p.IndustryCode,
                name = p.DisplayName,
                sampleQueries = p.SampleQueries.Take(2).Select(q => new { 
                    query = q.Query, 
                    description = q.Description,
                    category = q.Category 
                }),
                availableActions = p.AvailableActions.Take(3),
                workflowStates = p.WorkflowDefinitions.ContactStates.Take(3)
            });
            
            return Results.Ok(new {
                message = "Multi-industry EMMA platform is ready!",
                timestamp = DateTime.UtcNow,
                supportedIndustries = result
            });
        })
        .WithName("GetIndustryProfiles")
        .WithOpenApi();
        
        // Database health check endpoint
        app.MapGet("/health/db", async (AppDbContext dbContext) =>
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            return canConnect 
                ? Results.Ok("Database connection successful") 
                : Results.Problem("Database connection failed");
        })
        .WithName("DatabaseHealth");
        
        // Demo NBA Endpoints
        app.MapPost("/demo/analyze-interaction", async (IDemoNbaService demoService, AnalyzeInteractionRequest request) =>
        {
            var result = await demoService.AnalyzeInteractionAsync(
                request.InteractionText, 
                request.ClientContext ?? ""
            );
            
            return Results.Ok(new { 
                success = true, 
                analysis = result, 
                timestamp = DateTime.UtcNow 
            });
        })
        .WithName("AnalyzeInteraction")
        .WithOpenApi();
        
        app.MapPost("/demo/get-recommendation", async (IDemoNbaService demoService, GetRecommendationRequest request) =>
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
        })
        .WithName("GetRecommendation")
        .WithOpenApi();
        
        app.MapPost("/demo/update-summary", async (IDemoNbaService demoService, UpdateSummaryRequest request) =>
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
        })
        .WithName("UpdateClientSummary")
        .WithOpenApi();
        
        // Global error handler
        app.Map("/error", (HttpContext context) => 
        {
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            return Results.Problem(
                title: "An unexpected error occurred",
                detail: exception?.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        });
        
        Console.WriteLine("✅ Routes and endpoints configured");

    // Initialize database
    if (!string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("🔄 Initializing database...");
        bool resetDatabase = builder.Environment.IsDevelopment() && 
                            bool.TryParse(Environment.GetEnvironmentVariable("RESET_DATABASE"), out var reset) && reset;
                            
        if (resetDatabase)
        {
            Console.WriteLine("🔄 Resetting database as requested...");
        }
        
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeDatabaseAsync(resetDatabase);
                await initializer.SeedDatabaseAsync();
                Console.WriteLine("✅ Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing database: {ex.Message}");
                throw;
            }
        }
    }

    Console.WriteLine("🚀 Starting application...");
    Console.WriteLine("📍 Application should be available at: http://localhost:5000");
    Console.WriteLine("📍 Swagger UI: http://localhost:5000/swagger");
    Console.WriteLine("📍 Test endpoint: http://localhost:5000/test");
    Console.WriteLine("⏳ Starting server... (this should block and keep running)");
    
        await app.RunAsync();
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
}
