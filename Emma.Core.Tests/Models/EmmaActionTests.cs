using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Emma.Core.Models;
using Xunit;

namespace Emma.Core.Tests.Models
{
    public class EmmaActionTests
    {
        [Theory]
        [InlineData("{\"action\":\"sendemail\",\"payload\":\"test\"}", EmmaActionType.SendEmail, "test")]
        [InlineData("{\"action\":\"none\"}", EmmaActionType.None, "")]
        public void FromJson_ValidInput_ReturnsExpectedAction(string json, EmmaActionType expectedType, string expectedPayload)
        {
            // Act
            var action = EmmaAction.FromJson(json);

            // Assert
            Assert.Equal(expectedType, action.Action);
            Assert.Equal(expectedPayload, action.Payload);
        }

        [Fact]
        public void FromJson_MissingPayloadForEmail_ThrowsValidationException()
        {
            // Arrange
            var json = "{\"action\":\"sendemail\"}";

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => 
                EmmaAction.FromJson(json));
            Assert.Contains("Payload is required", ex.Message);
        }

        [Fact]
        public void FromJson_InvalidAction_ThrowsValidationException()
        {
            // Arrange
            var json = "{\"action\":\"invalid\"}";

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => 
                EmmaAction.FromJson(json));
            Assert.Contains("Invalid action type", ex.Message);
            Assert.Contains("Valid values are:", ex.Message);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsJsonException()
        {
            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => 
                EmmaAction.FromJson(null));
            Assert.Contains("cannot be null", ex.Message);
        }

        [Fact]
        public void FromJson_EmptyInput_ThrowsJsonException()
        {
            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => 
                EmmaAction.FromJson(string.Empty));
            Assert.Contains("cannot be empty", ex.Message);
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => 
                EmmaAction.FromJson("{invalid json}"));
            // Check for any error message that indicates invalid JSON
            Assert.NotNull(ex.Message);
        }
    }
}
