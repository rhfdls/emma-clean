using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Emma.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete AI-first CRM agent orchestration system
    /// Tests the full workflow from intent classification through agent communication
    /// </summary>
    public class AgentOrchestrationIntegrationTests : IClassFixture<TestServiceFixture>
    {
        private readonly TestServiceFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger<AgentOrchestrationIntegrationTests> _logger;

        public AgentOrchestrationIntegrationTests(TestServiceFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<AgentOrchestrationIntegrationTests>>();
        }

        [Fact]
        public async Task CompleteWorkflow_ContactManagement_ShouldProcessSuccessfully()
        {
            // Arrange
            var intentClassifier = _fixture.ServiceProvider.GetRequiredService<IIntentClassificationService>();
            var communicationBus = _fixture.ServiceProvider.GetRequiredService<IAgentCommunicationBus>();
            var contextIntelligence = _fixture.ServiceProvider.GetRequiredService<IContextIntelligenceService>();
            var agentRegistry = _fixture.ServiceProvider.GetRequiredService<IAgentRegistryService>();

            var traceId = Guid.NewGuid().ToString();
            var userInput = "I need to update Emily Johnson's contact information and schedule a follow-up call";
            var context = new Dictionary<string, object>
            {
                ["contactId"] = "550e8400-e29b-41d4-a716-446655440001",
                ["agentId"] = "agent-001",
                ["industry"] = "real_estate"
            };

            _output.WriteLine($"Starting complete workflow test with TraceId: {traceId}");

            // Act & Assert - Step 1: Intent Classification
            var intentResult = await intentClassifier.ClassifyIntentAsync(userInput, context, traceId);
            
            Assert.NotNull(intentResult);
            Assert.True(intentResult.Confidence > 0.5);
            Assert.Contains(intentResult.Intent, new[] { 
                AgentIntent.ContactManagement, 
                AgentIntent.ScheduleFollowUp, 
                AgentIntent.UpdateContactInfo 
            });

            _output.WriteLine($"Intent classified as: {intentResult.Intent} with confidence: {intentResult.Confidence}");

            // Act & Assert - Step 2: Agent Registration Check
            var availableAgents = await agentRegistry.GetAvailableAgentsAsync(intentResult.Intent);
            
            Assert.NotEmpty(availableAgents);
            _output.WriteLine($"Found {availableAgents.Count} available agents for intent: {intentResult.Intent}");

            // Act & Assert - Step 3: Agent Communication
            var agentRequest = new AgentRequest
            {
                Id = Guid.NewGuid().ToString(),
                TraceId = traceId,
                EventVersion = "1.0.0",
                WorkflowVersion = "1.0.0",
                Intent = intentResult.Intent,
                OriginalUserInput = userInput,
                InteractionId = Guid.NewGuid(),
                Context = context,
                Urgency = intentResult.Urgency,
                OrchestrationMethod = "custom",
                UserId = "test-user",
                Industry = "real_estate"
            };

            var agentResponse = await communicationBus.RouteRequestAsync(agentRequest);
            
            Assert.NotNull(agentResponse);
            Assert.True(agentResponse.Success);
            Assert.NotEmpty(agentResponse.Content);
            Assert.True(agentResponse.Confidence > 0);

            _output.WriteLine($"Agent response: {agentResponse.Content}");
            _output.WriteLine($"Processing time: {agentResponse.ProcessingTimeMs}ms");

            // Act & Assert - Step 4: Context Intelligence Analysis
            var interactionContent = $"User request: {userInput}\nAgent response: {agentResponse.Content}";
            var contactContext = new ContactContext
            {
                ContactId = "550e8400-e29b-41d4-a716-446655440001",
                Name = "Emily Johnson",
                RelationshipState = "ActiveClient",
                LastInteractionDate = DateTime.UtcNow.AddDays(-3),
                InteractionHistory = new List<string> { "Initial consultation", "Property viewing", "Offer preparation" }
            };

            var analyzedContext = await contextIntelligence.AnalyzeInteractionAsync(
                interactionContent, contactContext, traceId);

            Assert.NotNull(analyzedContext);
            Assert.InRange(analyzedContext.SentimentScore, -1.0, 1.0);
            Assert.NotNull(analyzedContext.BuyingSignals);
            Assert.InRange(analyzedContext.CloseProbability, 0.0, 1.0);

            _output.WriteLine($"Sentiment Score: {analyzedContext.SentimentScore}");
            _output.WriteLine($"Close Probability: {analyzedContext.CloseProbability}");
            _output.WriteLine($"Buying Signals: {string.Join(", ", analyzedContext.BuyingSignals)}");

            // Act & Assert - Step 5: Recommended Actions
            var recommendedActions = await contextIntelligence.GenerateRecommendedActionsAsync(
                analyzedContext, traceId);

            Assert.NotNull(recommendedActions);
            Assert.NotEmpty(recommendedActions);

            _output.WriteLine($"Recommended Actions: {string.Join(", ", recommendedActions)}");

            _output.WriteLine("Complete workflow test completed successfully!");
        }

        [Fact]
        public async Task MultiAgentWorkflow_PropertySearch_ShouldCoordinateAgents()
        {
            // Arrange
            var communicationBus = _fixture.ServiceProvider.GetRequiredService<IAgentCommunicationBus>();
            var agentRegistry = _fixture.ServiceProvider.GetRequiredService<IAgentRegistryService>();

            var traceId = Guid.NewGuid().ToString();
            var workflowId = "property-search-workflow";

            var initialRequest = new AgentRequest
            {
                Id = Guid.NewGuid().ToString(),
                TraceId = traceId,
                EventVersion = "1.0.0",
                WorkflowVersion = "1.0.0",
                Intent = AgentIntent.PropertySearch,
                OriginalUserInput = "Find properties for Emily Johnson - 3 bedroom house, budget $500K, downtown area",
                InteractionId = Guid.NewGuid(), // Use InteractionId
                Context = new Dictionary<string, object>
                {
                    ["contactId"] = "550e8400-e29b-41d4-a716-446655440001",
                    ["bedrooms"] = 3,
                    ["maxBudget"] = 500000,
                    ["area"] = "downtown",
                    ["propertyType"] = "house"
                },
                Urgency = UrgencyLevel.Medium,
                OrchestrationMethod = "custom",
                UserId = "test-agent",
                Industry = "real_estate"
            };

            _output.WriteLine($"Starting multi-agent workflow test with TraceId: {traceId}");

            // Act - Execute Workflow
            var workflowState = await communicationBus.ExecuteWorkflowAsync(workflowId, initialRequest);

            // Assert
            Assert.NotNull(workflowState);
            Assert.Equal(workflowId, workflowState.WorkflowId);
            Assert.Equal(traceId, workflowState.TraceId);
            Assert.NotEmpty(workflowState.Steps);

            _output.WriteLine($"Workflow completed with {workflowState.Steps.Count} steps");

            foreach (var step in workflowState.Steps)
            {
                _output.WriteLine($"Step {step.StepNumber}: {step.AgentName} - {step.Status}");
                if (!string.IsNullOrEmpty(step.Output))
                {
                    _output.WriteLine($"  Output: {step.Output}");
                }
            }

            // Verify workflow state can be retrieved
            var retrievedState = await communicationBus.GetWorkflowStateAsync(workflowId);
            Assert.NotNull(retrievedState);
            Assert.Equal(workflowState.WorkflowId, retrievedState.WorkflowId);
        }

        [Fact]
        public async Task AgentRegistry_LoadCatalog_ShouldRegisterAgents()
        {
            // Arrange
            var agentRegistry = _fixture.ServiceProvider.GetRequiredService<IAgentRegistryService>();
            var catalogPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "AgentCatalog");

            // Ensure test catalog directory exists
            Directory.CreateDirectory(catalogPath);
            
            // Create test agent card
            var testAgentCard = new
            {
                id = "test-agent-001",
                name = "Test Contact Agent",
                version = "1.0.0",
                description = "Test agent for contact management",
                capabilities = new[] { "contact_management", "data_validation" },
                intents = new[] { "ContactManagement", "UpdateContactInfo" },
                endpoints = new
                {
                    primary = "http://localhost:5000/api/agents/contact",
                    health = "http://localhost:5000/api/agents/contact/health"
                },
                metadata = new
                {
                    industry = "real_estate",
                    compliance = new[] { "GDPR", "CCPA" }
                }
            };

            var agentCardPath = Path.Combine(catalogPath, "test-contact-agent.json");
            await File.WriteAllTextAsync(agentCardPath, System.Text.Json.JsonSerializer.Serialize(testAgentCard, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            _output.WriteLine($"Created test agent card at: {agentCardPath}");

            // Act
            var loadedCount = await agentRegistry.LoadAgentCatalogAsync(catalogPath);

            // Assert
            Assert.True(loadedCount > 0);
            _output.WriteLine($"Loaded {loadedCount} agents from catalog");

            // Verify agent is registered
            var registeredAgents = await agentRegistry.GetAllRegisteredAgentsAsync();
            Assert.Contains(registeredAgents, a => a.Id == "test-agent-001");

            // Verify agent capabilities
            var capabilities = await agentRegistry.GetAgentCapabilitiesAsync("test-agent-001");
            Assert.NotNull(capabilities);
            Assert.Contains("contact_management", capabilities.Capabilities);

            _output.WriteLine("Agent catalog loading test completed successfully!");

            // Cleanup
            if (File.Exists(agentCardPath))
            {
                File.Delete(agentCardPath);
            }
        }

        [Fact]
        public async Task OrchestrationMethod_Switching_ShouldUpdateBehavior()
        {
            // Arrange
            var communicationBus = _fixture.ServiceProvider.GetRequiredService<IAgentCommunicationBus>();

            var testRequest = new AgentRequest
            {
                Id = Guid.NewGuid().ToString(),
                TraceId = Guid.NewGuid().ToString(),
                EventVersion = "1.0.0",
                WorkflowVersion = "1.0.0",
                Intent = AgentIntent.GeneralInquiry,
                OriginalUserInput = "Test orchestration method switching",
                ConversationId = Guid.NewGuid(),
                Context = new Dictionary<string, object>(),
                Urgency = UrgencyLevel.Low,
                OrchestrationMethod = "custom",
                UserId = "test-user",
                Industry = "real_estate"
            };

            _output.WriteLine("Testing orchestration method switching");

            // Act & Assert - Test Custom Orchestration
            communicationBus.SetOrchestrationMethod("custom");
            var customResponse = await communicationBus.RouteRequestAsync(testRequest);
            
            Assert.NotNull(customResponse);
            Assert.Equal("custom", customResponse.OrchestrationMethod);
            _output.WriteLine($"Custom orchestration response: {customResponse.Content}");

            // Act & Assert - Test Azure Foundry Orchestration (simulated)
            communicationBus.SetOrchestrationMethod("azure_foundry");
            testRequest.OrchestrationMethod = "azure_foundry";
            var foundryResponse = await communicationBus.RouteRequestAsync(testRequest);
            
            Assert.NotNull(foundryResponse);
            Assert.Equal("azure_foundry", foundryResponse.OrchestrationMethod);
            _output.WriteLine($"Azure Foundry orchestration response: {foundryResponse.Content}");

            _output.WriteLine("Orchestration method switching test completed successfully!");
        }

        [Fact]
        public async Task ContextIntelligence_RealEstateScenario_ShouldProvideInsights()
        {
            // Arrange
            var contextIntelligence = _fixture.ServiceProvider.GetRequiredService<IContextIntelligenceService>();
            var traceId = Guid.NewGuid().ToString();

            var interactionContent = @"
                Agent: Hi Emily, I wanted to follow up on the property viewing yesterday. How did you feel about the house on Maple Street?
                
                Emily: I really loved the kitchen and the backyard! The location is perfect for my commute. However, I'm a bit concerned about the inspection report mentioning the roof needs some work. What do you think about the repair costs?
                
                Agent: The inspection estimated about $8,000 for the roof repairs. Given that the house is priced competitively at $485,000, you'd still be within your budget of $500,000 even with the repairs. Plus, you could potentially negotiate with the seller to cover some of the repair costs.
                
                Emily: That makes sense. I'm definitely interested in moving forward. When can we submit an offer? I don't want to lose this property to another buyer.
                
                Agent: Great! I can prepare the offer documents today and we can submit them by this evening. Given your enthusiasm and the competitive market, I'd recommend offering close to asking price with a request for the seller to contribute $5,000 toward roof repairs.
                
                Emily: Perfect! Let's do it. I have a good feeling about this house.
            ";

            var contactContext = new ContactContext
            {
                ContactId = "550e8400-e29b-41d4-a716-446655440001",
                Name = "Emily Johnson",
                RelationshipState = "ActiveClient",
                LastInteractionDate = DateTime.UtcNow.AddDays(-1),
                InteractionHistory = new List<string> 
                { 
                    "Initial consultation - looking for 3BR house",
                    "Property viewing scheduled",
                    "Inspection arranged",
                    "Follow-up call scheduled"
                }
            };

            _output.WriteLine($"Analyzing real estate interaction with TraceId: {traceId}");

            // Act
            var analyzedContext = await contextIntelligence.AnalyzeInteractionAsync(
                interactionContent, contactContext, traceId);

            // Assert - Sentiment Analysis
            Assert.NotNull(analyzedContext);
            Assert.True(analyzedContext.SentimentScore > 0.5, "Should detect positive sentiment");
            _output.WriteLine($"Sentiment Score: {analyzedContext.SentimentScore}");

            // Assert - Buying Signals
            Assert.NotNull(analyzedContext.BuyingSignals);
            Assert.NotEmpty(analyzedContext.BuyingSignals);
            Assert.Contains(analyzedContext.BuyingSignals, signal => 
                signal.ToLower().Contains("ready") || 
                signal.ToLower().Contains("offer") || 
                signal.ToLower().Contains("interested"));
            _output.WriteLine($"Buying Signals: {string.Join(", ", analyzedContext.BuyingSignals)}");

            // Assert - Close Probability
            Assert.True(analyzedContext.CloseProbability > 0.7, "Should indicate high close probability");
            _output.WriteLine($"Close Probability: {analyzedContext.CloseProbability}");

            // Assert - Urgency Assessment
            Assert.Equal(UrgencyLevel.High, analyzedContext.Urgency);
            _output.WriteLine($"Urgency Level: {analyzedContext.Urgency}");

            // Act - Generate Recommended Actions
            var recommendedActions = await contextIntelligence.GenerateRecommendedActionsAsync(
                analyzedContext, traceId);

            // Assert - Recommended Actions
            Assert.NotNull(recommendedActions);
            Assert.NotEmpty(recommendedActions);
            _output.WriteLine($"Recommended Actions: {string.Join(", ", recommendedActions)}");

            // Should include actions related to offer preparation and follow-up
            Assert.Contains(recommendedActions, action => 
                action.ToLower().Contains("offer") || 
                action.ToLower().Contains("contract") || 
                action.ToLower().Contains("follow"));

            _output.WriteLine("Real estate context intelligence test completed successfully!");
        }
    }
}
