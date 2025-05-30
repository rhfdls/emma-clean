using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Emma.Api.Dtos; // Use standardized DTOs

namespace Emma.Api.Controllers
{
    /// <summary>
    /// Controller for Ask EMMA Anything (AEA) integration.
    /// Provides endpoints to interact with the Emma AI Platform via natural language questions.
    /// </summary>


    /// <summary>
    /// Ask EMMA Anything (AEA) controller for the Emma AI Platform
    /// </summary>
    [ApiController]
    [Route("api/aea")]
    [ApiExplorerSettings(GroupName = "Emma.Api")]
    public class AskEmmaAnythingController : ControllerBase
    {
        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ping()
        {
            _logger.LogInformation("AEA Ping endpoint called");
            return Ok(new { message = "AEA Pong", timestamp = DateTime.UtcNow });
        }

        private readonly ILogger<AskEmmaAnythingController> _logger;
        private readonly IAIFoundryService _aiFoundryService;

        public AskEmmaAnythingController(
            ILogger<AskEmmaAnythingController> logger,
            IAIFoundryService aiFoundryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiFoundryService = aiFoundryService ?? throw new ArgumentNullException(nameof(aiFoundryService));
        }

        /// <summary>
        /// Submit a question to Ask EMMA Anything (AEA)
        /// </summary>
        /// <param name="request">The user's question and optional interaction context</param>
        /// <returns>The Emma AI Platform's response</returns>
        [HttpPost("ask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AskEmmaResponseDto>> Ask([FromBody] AskEmmaRequestDto request)
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[AEA:{RequestId}] Received request. Message length: {MessageLength}, InteractionId: {InteractionId}",
                requestId,
                request.Message?.Length ?? 0,
                request.InteractionId ?? "(none)");

            try
            {
                _logger.LogDebug("[AEA:{RequestId}] Processing message with AI Foundry service...", requestId);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    _logger.LogWarning("Received empty message in AEA request");
                    return BadRequest("Message cannot be empty");
                }

                var response = await _aiFoundryService.ProcessMessageAsync(
                    request.Message,
                    request.InteractionId);
                stopwatch.Stop();

                _logger.LogInformation("[AEA:{RequestId}] Successfully received response in {ElapsedMs}ms. Response length: {ResponseLength}",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    response?.Length ?? 0);

                _logger.LogDebug("[AEA:{RequestId}] Response content: {ResponseContent}",
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
                _logger.LogWarning(ex, "Validation error in AEA Ask endpoint");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AEA Ask endpoint");
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    error = "An error occurred while processing your request",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Submit a simple question to Ask EMMA Anything (AEA)
        /// </summary>
        /// <param name="message">The question or prompt to send to EMMA</param>
        /// <returns>The Emma AI Platform's response</returns>
        [HttpPost("ask-simple")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AskEmmaResponseDto>> AskSimple([FromBody] string message = "Hello, how can EMMA assist you?")
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[AEA:{RequestId}] [Simple] Received request. Message length: {MessageLength}",
                requestId,
                message?.Length ?? 0);

            try
            {
                _logger.LogDebug("[AEA:{RequestId}] [Simple] Processing message with AI Foundry service...", requestId);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Received empty message in simple AEA request");
                    message = "Hello, how can EMMA assist you?";
                }

                var response = await _aiFoundryService.ProcessMessageAsync(message);
                stopwatch.Stop();

                _logger.LogInformation("[AEA:{RequestId}] [Simple] Successfully received response in {ElapsedMs}ms. Response length: {ResponseLength}",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    response?.Length ?? 0);

                _logger.LogDebug("[AEA:{RequestId}] [Simple] Response content: {ResponseContent}",
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
                _logger.LogError(ex, "Error processing simple AEA Ask endpoint");
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    error = "An error occurred while processing your request",
                    details = ex.Message
                });
            }
        }
    }
}
