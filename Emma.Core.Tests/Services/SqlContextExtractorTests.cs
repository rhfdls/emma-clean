using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emma.Core.Services;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Data;
using Emma.Models.Models;

namespace Emma.Core.Tests.Services
{
    public class SqlContextExtractorTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<SqlContextExtractor>> _mockLogger;
        private readonly Mock<ITenantContextService> _mockTenantService;
        private readonly SqlContextExtractor _extractor;

        public SqlContextExtractorTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<SqlContextExtractor>>();
            _mockTenantService = new Mock<ITenantContextService>();
            
            _extractor = new SqlContextExtractor(_context, _mockLogger.Object, _mockTenantService.Object);
            
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test organization
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                IndustryCode = "RealEstate",
                OwnerAgentId = Guid.NewGuid()
            };
            _context.Organizations.Add(org);

            // Create test agent
            var agent = new Agent
            {
                Id = org.OwnerAgentId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                OrganizationId = org.Id,
                IsActive = true
            };
            _context.Agents.Add(agent);

            // Create test contacts
            var contacts = new[]
            {
                new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Alice",
                    LastName = "Smith",
                    RelationshipState = RelationshipState.Lead,
                    IsActiveClient = false,
                    OwnerId = agent.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Bob",
                    LastName = "Johnson",
                    RelationshipState = RelationshipState.Client,
                    IsActiveClient = true,
                    ClientSince = DateTime.UtcNow.AddMonths(-3),
                    OwnerId = agent.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Carol",
                    LastName = "Williams",
                    RelationshipState = RelationshipState.Prospect,
                    IsActiveClient = false,
                    OwnerId = agent.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            _context.Contacts.AddRange(contacts);
            _context.SaveChanges();
        }

        [Fact]
        public async Task ExtractContextAsync_AgentRole_ReturnsAgentContext()
        {
            // Arrange
            var agentId = _context.Agents.First().Id;

            // Act
            var result = await _extractor.ExtractContextAsync(UserRole.Agent, agentId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AgentContext);
            Assert.NotNull(result.AgentContext.AssignedContacts);
            Assert.True(result.AgentContext.AssignedContacts.Count > 0);
            Assert.NotNull(result.AgentContext.Performance);
            Assert.NotNull(result.AgentContext.Timeline);
            
            // Verify agent-specific data
            Assert.Equal(3, result.AgentContext.Performance.ContactsThisMonth);
            Assert.True(result.AgentContext.AssignedContacts.Any(c => c.FirstName == "Alice"));
            Assert.True(result.AgentContext.AssignedContacts.Any(c => c.FirstName == "Bob"));
            Assert.True(result.AgentContext.AssignedContacts.Any(c => c.FirstName == "Carol"));
        }

        [Fact]
        public async Task ExtractContextAsync_AdminRole_ReturnsAdminContext()
        {
            // Arrange
            var agentId = _context.Agents.First().Id;

            // Act
            var result = await _extractor.ExtractContextAsync(UserRole.Admin, agentId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AdminContext);
            Assert.NotNull(result.AdminContext.OrganizationSummary);
            Assert.NotNull(result.AdminContext.AgentPerformance);
            
            // Verify admin-specific data
            Assert.True(result.AdminContext.OrganizationSummary.TotalContacts > 0);
            Assert.True(result.AdminContext.OrganizationSummary.ActiveAgents > 0);
        }

        [Fact]
        public async Task ExtractContextAsync_AIWorkflowRole_ReturnsAIWorkflowContext()
        {
            // Arrange
            var agentId = _context.Agents.First().Id;

            // Act
            var result = await _extractor.ExtractContextAsync(UserRole.AIWorkflow, agentId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AIWorkflowContext);
            Assert.NotNull(result.AIWorkflowContext.ActiveTasks);
            Assert.NotNull(result.AIWorkflowContext.ContactSummaries);
            
            // Verify AI workflow-specific data
            Assert.True(result.AIWorkflowContext.ContactSummaries.Count > 0);
        }

        [Fact]
        public async Task SerializeContextAsync_ValidContext_ReturnsJsonString()
        {
            // Arrange
            var agentId = _context.Agents.First().Id;
            var context = await _extractor.ExtractContextAsync(UserRole.Agent, agentId);

            // Act
            var json = await _extractor.SerializeContextAsync(context);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("AgentContext", json);
            Assert.Contains("AssignedContacts", json);
            Assert.Contains("Performance", json);
        }

        [Fact]
        public async Task ExtractContextAsync_ContactsOrderedByMostRecent()
        {
            // Arrange
            var agentId = _context.Agents.First().Id;

            // Act
            var result = await _extractor.ExtractContextAsync(UserRole.Agent, agentId);

            // Assert
            Assert.NotNull(result.AgentContext?.AssignedContacts);
            var contacts = result.AgentContext.AssignedContacts;
            
            // Verify contacts are ordered by most recent activity (UpdatedAt > CreatedAt ? UpdatedAt : CreatedAt)
            for (int i = 0; i < contacts.Count - 1; i++)
            {
                Assert.True(contacts[i].LastInteraction >= contacts[i + 1].LastInteraction);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
