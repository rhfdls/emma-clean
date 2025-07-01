using Emma.Core.Models;
using Xunit;
using System;
using System.Collections.Generic;

namespace Emma.Core.Tests.Models
{
    public class ScheduledActionTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var action = new ScheduledAction();

            // Assert
            Assert.False(string.IsNullOrEmpty(action.Id));
            Assert.True(Guid.TryParse(action.Id, out _));
            Assert.Equal(DateTime.UtcNow.Date, action.CreatedAt.Date);
            Assert.Equal(ScheduledActionStatus.Pending, action.Status);
            Assert.NotNull(action.Parameters);
            Assert.NotNull(action.RelevanceCriteria);
            Assert.NotNull(action.Metadata);
        }

        [Fact]
        public void ExecuteAt_WhenSetInFuture_ShouldBeValid()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            
            // Act
            var action = new ScheduledAction 
            { 
                ExecuteAt = futureDate 
            };

            // Assert
            Assert.Equal(futureDate, action.ExecuteAt);
        }

        [Fact]
        public void StatusTransitions_ShouldBeValid()
        {
            // Arrange
            var action = new ScheduledAction();
            
            // Act & Assert - Valid transitions
            action.Status = ScheduledActionStatus.RelevanceCheckPassed;
            Assert.Equal(ScheduledActionStatus.RelevanceCheckPassed, action.Status);
            
            action.Status = ScheduledActionStatus.Executing;
            Assert.Equal(ScheduledActionStatus.Executing, action.Status);
            
            action.Status = ScheduledActionStatus.Completed;
            Assert.Equal(ScheduledActionStatus.Completed, action.Status);
        }

        [Fact]
        public void AddParameter_ShouldStoreValue()
        {
            // Arrange
            var action = new ScheduledAction();
            var testKey = "testKey";
            var testValue = "testValue";
            
            // Act
            action.Parameters[testKey] = testValue;
            
            // Assert
            Assert.True(action.Parameters.ContainsKey(testKey));
            Assert.Equal(testValue, action.Parameters[testKey]);
        }

        [Fact]
        public void AddRelevanceCriterion_ShouldStoreValue()
        {
            // Arrange
            var action = new ScheduledAction();
            var criterionKey = "requiresContactAvailable";
            var criterionValue = true;
            
            // Act
            action.RelevanceCriteria[criterionKey] = criterionValue;
            
            // Assert
            Assert.True(action.RelevanceCriteria.ContainsKey(criterionKey));
            Assert.Equal(criterionValue, action.RelevanceCriteria[criterionKey]);
        }

        [Fact]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new ScheduledAction
            {
                ActionType = "testAction",
                Description = "Test Description",
                ContactId = Guid.NewGuid().ToString(),
                ExecuteAt = DateTime.UtcNow.AddHours(1),
                Status = ScheduledActionStatus.Pending
            };
            original.Parameters["testParam"] = "testValue";
            original.RelevanceCriteria["testCriterion"] = true;
            original.Metadata["testMetadata"] = "metadataValue";

            // Act
            var clone = original.Clone();

            // Assert - Verify properties are equal
            Assert.Equal(original.ActionType, clone.ActionType);
            Assert.Equal(original.Description, clone.Description);
            Assert.Equal(original.ContactId, clone.ContactId);
            Assert.Equal(original.ExecuteAt, clone.ExecuteAt);
            Assert.Equal(original.Status, clone.Status);
            
            // Verify collections are deep copied
            Assert.NotSame(original.Parameters, clone.Parameters);
            Assert.Equal(original.Parameters["testParam"], clone.Parameters["testParam"]);
            
            Assert.NotSame(original.RelevanceCriteria, clone.RelevanceCriteria);
            Assert.Equal(original.RelevanceCriteria["testCriterion"], clone.RelevanceCriteria["testCriterion"]);
            
            Assert.NotSame(original.Metadata, clone.Metadata);
            Assert.Equal(original.Metadata["testMetadata"], clone.Metadata["testMetadata"]);
        }

        [Fact]
        public void IsExpired_WhenExecuteAtInPast_ReturnsTrue()
        {
            // Arrange
            var action = new ScheduledAction
            {
                ExecuteAt = DateTime.UtcNow.AddMinutes(-5) // 5 minutes in past
            };

            // Act
            var isExpired = action.IsExpired();

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void IsExpired_WhenExecuteAtInFuture_ReturnsFalse()
        {
            // Arrange
            var action = new ScheduledAction
            {
                ExecuteAt = DateTime.UtcNow.AddMinutes(5) // 5 minutes in future
            };

            // Act
            var isExpired = action.IsExpired();

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void IsExpired_WithCustomExpiration_RespectsExpirationTime()
        {
            // Arrange
            var action = new ScheduledAction
            {
                ExecuteAt = DateTime.UtcNow.AddMinutes(-10) // 10 minutes in past
            };
            
            // Act - Check with 15 minute expiration (should not be expired)
            var isExpired = action.IsExpired(TimeSpan.FromMinutes(15));

            // Assert
            Assert.False(isExpired);
        }
    }
}
