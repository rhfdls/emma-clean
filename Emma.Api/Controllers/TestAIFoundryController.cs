using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// Test controller for Azure AI Foundry integration
    /// </summary>
    public class TestMessageRequest
    {
        [Required]
        public string Message { get; set; } = "Hello, how are you?";
        public string? InteractionId { get; set; }
    }

    /// <summary>
    /// Test controller for Azure AI Foundry integration
    /// </summary>
    [ApiController]
    [Route("api/test/ai-foundry")]
    [ApiExplorerSettings(GroupName = "Emma.Api")]
    public class TestAIFoundryController : ControllerBase
    {
        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ping()
        {
            _logger.LogInformation("Ping endpoint called");
            return Ok(new { message = "Pong", timestamp = DateTime.UtcNow });
        }

        private readonly ILogger<TestAIFoundryController> _logger;
        private readonly IAIFoundryService _aiFoundryService;

        public TestAIFoundryController(
            ILogger<TestAIFoundryController> logger,
            IAIFoundryService aiFoundryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiFoundryService = aiFoundryService ?? throw new ArgumentNullException(nameof(aiFoundryService));
        }

        /// <summary>
        /// Test the AI Foundry integration with a message
        /// </summary>
        /// <param name="request">The message request</param>
        /// <returns>The AI's response</returns>
        [HttpPost("test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestMessage([FromBody] TestMessageRequest request)
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[{RequestId}] Received request. Message length: {MessageLength}, InteractionId: {InteractionId}",
                requestId,
                request.Message?.Length ?? 0,
                request.InteractionId ?? "(none)");
                
            try
            {
                _logger.LogDebug("[{RequestId}] Processing message with AI Foundry service...", requestId);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    _logger.LogWarning("Received empty message in request");
                    return BadRequest("Message cannot be empty");
                }

                var response = await _aiFoundryService.ProcessMessageAsync(
                    request.Message, 
                    request.InteractionId);
                stopwatch.Stop();
                
                _logger.LogInformation("[{RequestId}] Successfully received response in {ElapsedMs}ms. Response length: {ResponseLength}", 
                    requestId, 
                    stopwatch.ElapsedMilliseconds,
                    response?.Length ?? 0);
                    
                _logger.LogDebug("[{RequestId}] Response content: {ResponseContent}", 
                    requestId, 
                    response ?? "(null)");
                    
                return Ok(new { 
                    requestId,
                    response,
                    processingTimeMs = stopwatch.ElapsedMilliseconds 
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in TestMessage");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing AI Foundry");
                return StatusCode(StatusCodes.Status500InternalServerError, new { 
                    error = "An error occurred while processing your request",
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Test the AI Foundry integration with a simple string message
        /// </summary>
        /// <param name="message">The message to send to the AI</param>
        /// <returns>The AI's response</returns>
        [HttpPost("test-simple")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestSimpleMessage([FromBody] string message = "Hello, how are you?")
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[{RequestId}] [Simple] Received request. Message length: {MessageLength}",
                requestId,
                message?.Length ?? 0);
                
            try
            {
                _logger.LogDebug("[{RequestId}] [Simple] Processing message with AI Foundry service...", requestId);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Received empty message in simple request");
                    message = "Hello, how are you?";
                }

                var response = await _aiFoundryService.ProcessMessageAsync(message);
                stopwatch.Stop();
                
                _logger.LogInformation("[{RequestId}] [Simple] Successfully received response in {ElapsedMs}ms. Response length: {ResponseLength}", 
                    requestId, 
                    stopwatch.ElapsedMilliseconds,
                    response?.Length ?? 0);
                
                _logger.LogDebug("[{RequestId}] [Simple] Response content: {ResponseContent}", 
                    requestId, 
                    response ?? "(null)");
                
                return Ok(new { 
                    requestId,
                    response,
                    processingTimeMs = stopwatch.ElapsedMilliseconds 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing AI Foundry with simple message");
                return StatusCode(StatusCodes.Status500InternalServerError, new { 
                    error = "An error occurred while processing your request",
                    details = ex.Message 
                });
            }
        }
    }
}
