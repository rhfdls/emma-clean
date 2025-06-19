using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Emma.Core.Tests.Configuration
{
    public class YamlCapabilityValidatorTests
    {
        private readonly ILogger<YamlCapabilityValidator> _logger;
        private readonly YamlCapabilityValidator _validator;

        public YamlCapabilityValidatorTests()
        {
            _logger = NullLogger<YamlCapabilityValidator>.Instance;
            _validator = new YamlCapabilityValidator(_logger);
        }

        [Fact]
        public void Validate_WithValidConfig_ReturnsTrue()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    ["TestAgent"] = new()
                    {
                        Capabilities = new List<AgentCapabilityYaml.AgentCapability>
                        {
                            new()
                            {
                                Name = "test:capability",
                                Description = "Test capability"
                            }
                        }
                    }
                }
            };

            // Act
            var result = _validator.Validate(config);

            // Assert
            Assert.True(result);
        }


        [Fact]
        public void Validate_WithMissingVersion_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = null!,
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>()
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("Version is required", ex.Message);
        }

        [Fact]
        public void Validate_WithInvalidVersionFormat_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "invalid-version",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>()
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("Version must be in format 'major.minor'", ex.Message);
        }

        [Fact]
        public void Validate_WithNoAgents_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>()
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("At least one agent configuration is required", ex.Message);
        }

        [Fact]
        public void Validate_WithEmptyAgentName_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    [""] = new() { Capabilities = new List<AgentCapabilityYaml.AgentCapability>() }
                }
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("Agent name cannot be empty", ex.Message);
        }

        [Fact]
        public void Validate_WithNoCapabilities_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    ["TestAgent"] = new() { Capabilities = new List<AgentCapabilityYaml.AgentCapability>() }
                }
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("must have at least one capability", ex.Message);
        }

        [Theory]
        [InlineData("test:capability", true)]
        [InlineData("test-capability", true)]
        [InlineData("test1capability2", true)]
        [InlineData("Test_Capability", false)] // Invalid due to uppercase and underscore
        [InlineData("test capability", false)] // Invalid due to space
        [InlineData("test@capability", false)] // Invalid due to special character
        public void Validate_CapabilityName_ValidatesFormat(string capabilityName, bool shouldPass)
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    ["TestAgent"] = new()
                    {
                        Capabilities = new List<AgentCapabilityYaml.AgentCapability>
                        {
                            new()
                            {
                                Name = capabilityName,
                                Description = "Test capability"
                            }
                        }
                    }
                }
            };

            // Act & Assert
            if (shouldPass)
            {
                var result = _validator.Validate(config);
                Assert.True(result);
            }
            else
            {
                var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
                Assert.Contains("Must contain only lowercase letters, numbers, colons, or hyphens", ex.Message);
            }
        }

        [Fact]
        public void Validate_WithMissingCapabilityDescription_ThrowsValidationException()
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    ["TestAgent"] = new()
                    {
                        Capabilities = new List<AgentCapabilityYaml.AgentCapability>
                        {
                            new()
                            {
                                Name = "test:capability",
                                Description = string.Empty
                            }
                        }
                    }
                }
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
            Assert.Contains("Description is required", ex.Message);
        }

        [Theory]
        [InlineData("1s", true)]
        [InlineData("60s", true)]
        [InlineData("5m", true)]
        [InlineData("2h", true)]
        [InlineData("invalid", false)]
        [InlineData("100", false)]
        [InlineData("1d", false)]
        public void Validate_RateLimitWindow_ValidatesFormat(string window, bool shouldPass)
        {
            // Arrange
            var config = new AgentCapabilityYaml
            {
                Version = "1.0",
                Agents = new Dictionary<string, AgentCapabilityYaml.AgentConfig>
                {
                    ["TestAgent"] = new()
                    {
                        Capabilities = new List<AgentCapabilityYaml.AgentCapability>
                        {
                            new()
                            {
                                Name = "test:capability",
                                Description = "Test capability"
                            }
                        },
                        RateLimits = new List<AgentCapabilityYaml.RateLimit>
                        {
                            new()
                            {
                                Window = window,
                                MaxRequests = 100,
                                Scope = "per_tenant"
                            }
                        }
                    }
                }
            };

            // Act & Assert
            if (shouldPass)
            {
                var result = _validator.Validate(config);
                Assert.True(result);
            }
            else
            {
                var ex = Assert.Throws<ValidationException>(() => _validator.Validate(config));
                Assert.Contains("window must end with 's' (seconds), 'm' (minutes), or 'h' (hours)", ex.Message);
            }
        }
    }
}
