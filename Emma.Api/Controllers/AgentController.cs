using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers;

/// <summary>
/// API controller for AI agent interactions using Azure AI Foundry
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentOrchestrator orchestrator,
        ITenantContextService tenantContext,
        ILogger<AgentController> logger)
    {
        _orchestrator = orchestrator;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Process natural language request using Azure AI Foundry agents
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessRequest([FromBody] ProcessRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserRequest))
            {
                return BadRequest("User request cannot be empty");
            }

            _logger.LogInformation("Processing agent request: {Request}", request.UserRequest);

            var agentRequest = new AgentRequest
            {
                OriginalUserInput = request.UserRequest,
                ConversationId = Guid.NewGuid(), // Generate new conversation ID
                Intent = AgentIntent.Unknown, // Default intent
                Context = new Dictionary<string, object>
                {
                    ["ContactId"] = request.ContactId,
                    ["AgentId"] = request.AgentId
                }
            };

            var response = await _orchestrator.ProcessRequestAsync(agentRequest);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request");
            return StatusCode(500, new { error = "Internal server error processing request" });
        }
    }

    /// <summary>
    /// Get available Azure AI Foundry agents for current tenant
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableAgents()
    {
        try
        {
            var agents = await _orchestrator.GetAvailableAgentsAsync();
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available agents");
            return StatusCode(500, new { error = "Internal server error getting agents" });
        }
    }

    /// <summary>
    /// Get current tenant industry profile
    /// </summary>
    [HttpGet("industry-profile")]
    public async Task<IActionResult> GetIndustryProfile()
    {
        try
        {
            var profile = await _tenantContext.GetIndustryProfileAsync();
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting industry profile");
            return StatusCode(500, new { error = "Internal server error getting industry profile" });
        }
    }

    /// <summary>
    /// Route specific task to Azure AI Foundry agent
    /// </summary>
    [HttpPost("route")]
    public async Task<IActionResult> RouteToAgent([FromBody] RouteTaskDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AgentType))
            {
                return BadRequest("Agent type cannot be empty");
            }

            if (request.Task == null)
            {
                return BadRequest("Task cannot be null");
            }

            _logger.LogInformation("Routing task to Azure AI Foundry agent: {AgentType}", request.AgentType);

            var response = await _orchestrator.RouteToAzureAgentAsync(request.AgentType, request.Task);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to Azure AI Foundry agent");
            return StatusCode(500, new { error = "Internal server error routing to agent" });
        }
    }
}

/// <summary>
/// DTO for processing natural language requests
/// </summary>
public class ProcessRequestDto
{
    public string UserRequest { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
    public Guid AgentId { get; set; }
}

/// <summary>
/// DTO for routing tasks to specific agents
/// </summary>
public class RouteTaskDto
{
    public string AgentType { get; set; } = string.Empty;
    public AgentTask Task { get; set; } = new();
}
