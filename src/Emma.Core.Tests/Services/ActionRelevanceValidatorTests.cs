using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Linq;
using Emma.Core.Config;

namespace Emma.Core.Tests.Services
{
    public class ActionRelevanceValidatorTests
    {
        private readonly Mock<INbaContextService> _mockNbaContextService;
        private readonly Mock<IAIFoundryService> _mockAiFoundryService;
        private readonly Mock<IPromptProvider> _mockPromptProvider;
        private readonly Mock<ILogger<ActionRelevanceValidator>> _mockLogger;
        private readonly ActionRelevanceValidator _validator;
        private readonly ActionRelevanceConfig _config;

        public ActionRelevanceValidatorTests()
        {
            _mockNbaContextService = new Mock<INbaContextService>();
            _mockAiFoundryService = new Mock<IAIFoundryService>();
            _mockPromptProvider = new Mock<IPromptProvider>();
            _mockLogger = new Mock<ILogger<ActionRelevanceValidator>>();
            
            _config = new ActionRelevanceConfig
            {
                EnableAuditLogging = true,
                DefaultExpirationMinutes = 60,
                BatchValidationEnabled = true,
                BatchSize = 50
            };
            
            _validator = new ActionRelevanceValidator(
                _mockNbaContextService.Object,
                _mockAiFoundryService.Object,
                _mockPromptProvider.Object,
                _mockLogger.Object,
                _config);
        }

        [Fact]
        public async Task ValidateActionRelevanceAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _validator.ValidateActionRelevanceAsync(null!));
        }

        [Fact]
        public async Task ValidateActionRelevanceAsync_WithExpiredAction_ReturnsExpiredResult()
        {
            // Arrange
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction
                {
                    Id = "test-action-1",
                    ExecuteAt = DateTime.UtcNow.AddMinutes(-10), // Expired
                    Status = ScheduledActionStatus.Pending
                },
                ContactId = "contact-1",
                OrganizationId = "org-1",
                UserId = "user-1"
            };

            // Act
            var result = await _validator.ValidateActionRelevanceAsync(request);

            // Assert
            Assert.False(result.IsRelevant);
            Assert.Equal(ActionRelevanceStatus.Expired, result.Status);
            Assert.Contains("expired", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateActionRelevanceAsync_WithFutureAction_ReturnsRelevantResult()
        {
            // Arrange
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction
                {
                    Id = "test-action-2",
                    ExecuteAt = DateTime.UtcNow.AddMinutes(30), // Future
                    Status = ScheduledActionStatus.Pending,
                    ActionType = "test-action"
                },
                ContactId = "contact-1",
                OrganizationId = "org-1",
                UserId = "user-1"
            };

            // Mock successful relevance check
            _mockAiFoundryService
                .Setup(x => x.ValidateActionRelevanceAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new ActionRelevanceResult 
                { 
                    IsRelevant = true, 
                    Status = ActionRelevanceStatus.Relevant,
                    Reason = "Action is relevant"
                });

            // Act
            var result = await _validator.ValidateActionRelevanceAsync(request);

            // Assert
            Assert.True(result.IsRelevant);
            Assert.Equal(ActionRelevanceStatus.Relevant, result.Status);
            _mockAiFoundryService.Verify(
                x => x.ValidateActionRelevanceAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateBatchRelevanceAsync_WithMultipleActions_ProcessesAllActions()
        {
            // Arrange
            var requests = new List<ActionRelevanceRequest>
            {
                new ActionRelevanceRequest
                {
                    Action = new ScheduledAction { Id = "action-1", ExecuteAt = DateTime.UtcNow.AddMinutes(30) },
                    ContactId = "contact-1"
                },
                new ActionRelevanceRequest
                {
                    Action = new ScheduledAction { Id = "action-2", ExecuteAt = DateTime.UtcNow.AddMinutes(60) },
                    ContactId = "contact-2"
                }
            };

            // Mock successful relevance checks
            _mockAiFoundryService
                .Setup(x => x.ValidateActionRelevanceBatchAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new List<ActionRelevanceResult>
                {
                    new ActionRelevanceResult { IsRelevant = true, Status = ActionRelevanceStatus.Relevant },
                    new ActionRelevanceResult { IsRelevant = true, Status = ActionRelevanceStatus.Relevant }
                });

            // Act
            var results = await _validator.ValidateBatchRelevanceAsync(requests, "test-trace");

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, r => 
            {
                Assert.True(r.IsRelevant);
                Assert.Equal(ActionRelevanceStatus.Relevant, r.Status);
            });
        }

        [Fact]
        public async Task ValidateActionRelevanceAsync_WithHighRiskAction_RequiresApproval()
        {
            // Arrange
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction
                {
                    Id = "high-risk-action",
                    ExecuteAt = DateTime.UtcNow.AddHours(1),
                    ActionType = "high-risk-action",
                    Parameters = new Dictionary<string, object>
                    {
                        ["riskLevel"] = "high"
                    }
                },
                ContactId = "contact-1",
                OrganizationId = "org-1",
                UserId = "user-1"
            };

            // Mock AI service to flag as high risk
            _mockAiFoundryService
                .Setup(x => x.ValidateActionRelevanceAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new ActionRelevanceResult 
                { 
                    IsRelevant = true, 
                    Status = ActionRelevanceStatus.RequiresApproval,
                    Reason = "High risk action requires approval"
                });

            // Act
            var result = await _validator.ValidateActionRelevanceAsync(request);

            // Assert
            Assert.True(result.IsRelevant);
            Assert.Equal(ActionRelevanceStatus.RequiresApproval, result.Status);
            Assert.Contains("approval", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateActionRelevanceAsync_WithRelevanceCriteria_ValidatesCriteria()
        {
            // Arrange
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction
                {
                    Id = "criteria-action",
                    ExecuteAt = DateTime.UtcNow.AddHours(1),
                    ActionType = "test-action",
                    RelevanceCriteria = new Dictionary<string, object>
                    {
                        ["requiresContactAvailable"] = true,
                        ["minContactScore"] = 80
                    }
                },
                ContactId = "contact-1",
                OrganizationId = "org-1",
                UserId = "user-1"
            };

            // Mock context service to provide contact data
            _mockNbaContextService
                .Setup(x => x.GetContactAsync(request.ContactId, request.OrganizationId, It.IsAny<string>()))
                .ReturnsAsync(new ContactInfo 
                { 
                    IsAvailable = true,
                    Score = 85
                });

            // Act
            var result = await _validator.ValidateActionRelevanceAsync(request);

            // Assert
            Assert.True(result.IsRelevant);
            _mockNbaContextService.Verify(
                x => x.GetContactAsync(request.ContactId, request.OrganizationId, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAuditLog_WithAuditLoggingEnabled_ReturnsLogEntries()
        {
            // Arrange - First validate an action to generate audit log
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction { Id = "audit-action-1", ExecuteAt = DateTime.UtcNow.AddHours(1) },
                ContactId = "contact-1"
            };

            _mockAiFoundryService
                .Setup(x => x.ValidateActionRelevanceAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new ActionRelevanceResult { IsRelevant = true });

            // Act - Perform validation to generate audit log
            await _validator.ValidateActionRelevanceAsync(request, "test-trace");
            
            // Get the audit log
            var auditLog = _validator.GetAuditLog();

            // Assert
            Assert.NotEmpty(auditLog);
            var entry = auditLog.First();
            Assert.Equal(request.Action.Id, entry.ActionId);
            Assert.Equal("test-trace", entry.TraceId);
            Assert.True(entry.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task ClearAuditLog_WhenCalled_RemovesAllEntries()
        {
            // Arrange - Add some audit log entries
            var request = new ActionRelevanceRequest
            {
                Action = new ScheduledAction { Id = "audit-action-2", ExecuteAt = DateTime.UtcNow.AddHours(1) },
                ContactId = "contact-1"
            };

            _mockAiFoundryService
                .Setup(x => x.ValidateActionRelevanceAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new ActionRelevanceResult { IsRelevant = true });

            await _validator.ValidateActionRelevanceAsync(request, "test-trace-1");
            await _validator.ValidateActionRelevanceAsync(request, "test-trace-2");

            // Pre-assert
            var initialLog = _validator.GetAuditLog();
            Assert.Equal(2, initialLog.Count);

            // Act
            _validator.ClearAuditLog();

            // Assert
            var clearedLog = _validator.GetAuditLog();
            Assert.Empty(clearedLog);
        }
    }
}
