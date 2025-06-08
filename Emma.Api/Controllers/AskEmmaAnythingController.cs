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
        private readonly ILogger<AskEmmaAnythingController> _logger;
        private readonly IAIFoundryService _aiFoundryService;
        private readonly IIndustryProfileService _industryProfileService;

        public AskEmmaAnythingController(
            ILogger<AskEmmaAnythingController> logger,
            IAIFoundryService aiFoundryService,
            IIndustryProfileService industryProfileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiFoundryService = aiFoundryService ?? throw new ArgumentNullException(nameof(aiFoundryService));
            _industryProfileService = industryProfileService ?? throw new ArgumentNullException(nameof(industryProfileService));
        }

        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ping()
        {
            _logger.LogInformation("AEA Ping endpoint called");
            return Ok(new { message = "AEA Pong", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Get available industry profiles
        /// </summary>
        /// <returns>List of available industry profiles</returns>
        [HttpGet("industries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAvailableIndustries()
        {
            try
            {
                var profiles = await _industryProfileService.GetAvailableProfilesAsync();
                var industries = profiles.Select(p => new 
                { 
                    code = p.IndustryCode, 
                    name = p.DisplayName,
                    sampleQueries = p.SampleQueries.Take(3).Select(q => new { q.Query, q.Description, q.Category })
                });
                
                return Ok(industries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available industries");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to get industries" });
            }
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
            _logger.LogInformation("[AEA:{RequestId}] Received request. Message length: {MessageLength}, InteractionId: {InteractionId}, OrganizationId: {OrganizationId}",
                requestId,
                request.Message?.Length ?? 0,
                request.InteractionId ?? "(none)",
                request.OrganizationId?.ToString() ?? "(none)");

            try
            {
                _logger.LogDebug("[AEA:{RequestId}] Processing message with AI Foundry service...", requestId);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    _logger.LogWarning("Received empty message in AEA request");
                    return BadRequest("Message cannot be empty");
                }

                // Get industry-specific profile and build enhanced prompt
                var industryProfile = await GetIndustryProfileForRequest(request);
                var enhancedMessage = await BuildIndustrySpecificPrompt(request.Message, industryProfile);

                _logger.LogDebug("[AEA:{RequestId}] Using industry profile: {IndustryCode}", requestId, industryProfile.IndustryCode);

                var response = await _aiFoundryService.ProcessMessageAsync(
                    enhancedMessage,
                    request.InteractionId);
                stopwatch.Stop();

                _logger.LogInformation("[AEA:{RequestId}] Successfully received response in {ElapsedMs}ms. Response length: {ResponseLength}",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    response?.Length ?? 0);

                _logger.LogDebug("[AEA:{RequestId}] Response content: {ResponseContent}",
                    requestId,
                    response ?? "(null)");

                return Ok(new AskEmmaResponseDto {
                    RequestId = requestId,
                    Response = response ?? "I'm sorry, I couldn't process your request at this time.",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
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
        /// Get the appropriate industry profile for the request
        /// </summary>
        private async Task<Emma.Core.Industry.IIndustryProfile> GetIndustryProfileForRequest(AskEmmaRequestDto request)
        {
            // Use explicit industry code if provided
            if (!string.IsNullOrWhiteSpace(request.IndustryCode))
            {
                var explicitProfile = await _industryProfileService.GetProfileAsync(request.IndustryCode);
                if (explicitProfile != null)
                {
                    return explicitProfile;
                }
            }

            // Use organization's industry if available
            if (request.OrganizationId.HasValue)
            {
                return await _industryProfileService.GetProfileForOrganizationAsync(request.OrganizationId.Value);
            }

            // Default to Real Estate
            return await _industryProfileService.GetProfileAsync("RealEstate") 
                ?? throw new InvalidOperationException("Default RealEstate profile not found");
        }

        /// <summary>
        /// Build industry-specific prompt with system context
        /// </summary>
        private async Task<string> BuildIndustrySpecificPrompt(string userMessage, Emma.Core.Industry.IIndustryProfile profile)
        {
            var systemPrompt = profile.PromptTemplates.SystemPrompt;
            
            // Combine system prompt with user message
            var enhancedPrompt = $@"{systemPrompt}

User Question: {userMessage}

Please provide a helpful, actionable response based on your expertise in {profile.DisplayName.ToLower()}. If the question relates to specific contacts or workflows, provide concrete next steps and recommendations.";

            return enhancedPrompt;
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
