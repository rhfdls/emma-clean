using System;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Services;
using Emma.IntegrationTests.Fixtures;
using Emma.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Emma.Data;
using Emma.Models.Interfaces;

namespace Emma.IntegrationTests.Services
{
    public class NbaTimeEventHandlerTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IAppDbContext _dbContext;
        private readonly NbaTimeEventHandler _handler;
        private readonly Mock<INbaAgent> _mockNbaAgent;
        private readonly Mock<IAgentActionValidator> _mockActionValidator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NbaTimeEventHandler> _logger;

        public NbaTimeEventHandlerTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _dbContext = fixture.DbContext;
            _serviceProvider = fixture.ServiceProvider;
            
            // Create mocks
            _mockNbaAgent = new Mock<INbaAgent>();
            _mockActionValidator = new Mock<IAgentActionValidator>();
            _logger = new LoggerFactory().CreateLogger<NbaTimeEventHandler>();
            
            // Create the handler with mocks
            _handler = new NbaTimeEventHandler(
                _logger,
                _serviceProvider,
                _mockNbaAgent.Object,
                _mockActionValidator.Object);
        }

        public async Task InitializeAsync()
        {
            // Ensure database is clean before each test
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.Database.EnsureCreatedAsync();
            
            // Seed test data
            await TestDataSeeder.SeedTestData(_dbContext);
        }

        public Task DisposeAsync()
        {
            _handler.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task OnSimulationTimeChangedAsync_WithActiveContact_ProcessesRecommendations()
        {
            // Arrange
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact",
                Email = "test@example.com",
                IsActive = true,
                OrganizationId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            };
            
            await _dbContext.Contacts.AddAsync(contact);
            await _dbContext.SaveChangesAsync();
            
            var recommendation = new NbaRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                ActionType = "follow_up",
                Description = "Follow up with the contact",
                Priority = 1,
                Reasoning = "Previous interaction requires follow-up",
                Timing = "Within 1-2 days",
                ExpectedOutcome = "Improved engagement",
                ConfidenceScore = 0.85,
                TraceId = "test-trace-id"
            };
            
            // Setup mock to return a recommendation
            _mockNbaAgent.Setup(x => x.RecommendNextBestActionsAsync(
                    contact.Id,
                    contact.OrganizationId,
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse
                {
                    Success = true,
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["recommendations"] = new[] { recommendation }
                    }
                });
            
            // Setup validator to approve the action
            _mockActionValidator.Setup(x => x.ValidateActionAsync(It.IsAny<NbaRecommendation>()))
                .ReturnsAsync(new AgentActionValidationResult { IsValid = true });
            
            // Act - Simulate time advancement of 2 hours (should trigger processing)
            var simulationTime = DateTime.UtcNow;
            await _handler.OnSimulationTimeChangedAsync(simulationTime, TimeSpan.FromHours(2));
            
            // Assert - Check that a task was created
            var tasks = await _dbContext.Tasks
                .Where(t => t.ContactId == contact.Id)
                .ToListAsync();
                
            Assert.Single(tasks);
            var task = tasks.First();
            Assert.Contains(recommendation.Description, task.Title);
            Assert.Equal(contact.OrganizationId, task.OrganizationId);
            Assert.Equal("Pending", task.Status);
            
            // Verify the NBA agent was called
            _mockNbaAgent.Verify(x => x.RecommendNextBestActionsAsync(
                contact.Id,
                contact.OrganizationId,
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public async Task OnSimulationTimeChangedAsync_WithInvalidRecommendation_DoesNotCreateTask()
        {
            // Arrange
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Invalid",
                Email = "invalid@example.com",
                IsActive = true,
                OrganizationId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            };
            
            await _dbContext.Contacts.AddAsync(contact);
            await _dbContext.SaveChangesAsync();
            
            var recommendation = new NbaRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                ActionType = "invalid_action",
                Description = "This should be invalid",
                Priority = 1,
                Reasoning = "Invalid test case",
                Timing = "Now",
                ExpectedOutcome = "Should be rejected",
                ConfidenceScore = 0.1,
                TraceId = "test-invalid-trace"
            };
            
            // Setup mock to return a recommendation
            _mockNbaAgent.Setup(x => x.RecommendNextBestActionsAsync(
                    contact.Id,
                    contact.OrganizationId,
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse
                {
                    Success = true,
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["recommendations"] = new[] { recommendation }
                    }
                });
            
            // Setup validator to reject the action
            _mockActionValidator.Setup(x => x.ValidateActionAsync(It.IsAny<NbaRecommendation>()))
                .ReturnsAsync(new AgentActionValidationResult { IsValid = false, Reason = "Invalid action type" });
            
            // Act - Simulate time advancement of 2 hours (should trigger processing)
            var simulationTime = DateTime.UtcNow;
            await _handler.OnSimulationTimeChangedAsync(simulationTime, TimeSpan.FromHours(2));
            
            // Assert - No task should be created for invalid recommendation
            var tasks = await _dbContext.Tasks
                .Where(t => t.ContactId == contact.Id)
                .ToListAsync();
                
            Assert.Empty(tasks);
        }
        
        [Fact]
        public async Task OnSimulationTimeChangedAsync_WithMultipleContacts_ProcessesAllContacts()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var contacts = new[]
            {
                new Contact { Id = Guid.NewGuid(), FirstName = "First", LastName = "Contact", Email = "first@example.com", IsActive = true, OrganizationId = orgId },
                new Contact { Id = Guid.NewGuid(), FirstName = "Second", LastName = "Contact", Email = "second@example.com", IsActive = true, OrganizationId = orgId },
                new Contact { Id = Guid.NewGuid(), FirstName = "Inactive", LastName = "Contact", Email = "inactive@example.com", IsActive = false, OrganizationId = orgId }
            };
            
            await _dbContext.Contacts.AddRangeAsync(contacts);
            await _dbContext.SaveChangesAsync();
            
            // Setup mock to return a recommendation for any contact
            _mockNbaAgent.Setup(x => x.RecommendNextBestActionsAsync(
                    It.IsAny<Guid>(),
                    orgId,
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse
                {
                    Success = true,
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["recommendations"] = new[] 
                        { 
                            new NbaRecommendation 
                            { 
                                Id = Guid.NewGuid().ToString(),
                                ActionType = "follow_up",
                                Description = "Test recommendation"
                            } 
                        }
                    }
                });
            
            // Setup validator to approve all actions
            _mockActionValidator.Setup(x => x.ValidateActionAsync(It.IsAny<NbaRecommendation>()))
                .ReturnsAsync(new AgentActionValidationResult { IsValid = true });
            
            // Act - Simulate time advancement of 2 hours (should trigger processing)
            var simulationTime = DateTime.UtcNow;
            await _handler.OnSimulationTimeChangedAsync(simulationTime, TimeSpan.FromHours(2));
            
            // Assert - Should create tasks for active contacts only
            var tasks = await _dbContext.Tasks.ToListAsync();
            Assert.Equal(2, tasks.Count); // Only 2 active contacts
            
            // Verify the NBA agent was called for each active contact
            foreach (var contact in contacts.Where(c => c.IsActive))
            {
                _mockNbaAgent.Verify(x => x.RecommendNextBestActionsAsync(
                    contact.Id,
                    orgId,
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()), Times.Once);
            }
        }
    }
}
