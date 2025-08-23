using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/agent")] // POST /api/agent/suggest-followup
    public class AgentController : ControllerBase
    {
        private readonly ILogger<AgentController> _logger;
        private readonly IWebHostEnvironment _env;

        public AgentController(ILogger<AgentController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public class SuggestFollowupRequest
        {
            public Guid? ContactId { get; set; }
            public string? Context { get; set; }
        }

        [HttpPost("suggest-followup")]
        [Authorize(Policy = "VerifiedUser")]
        public IActionResult SuggestFollowup([FromBody] SuggestFollowupRequest body)
        {
            if (!_env.IsDevelopment())
            {
                return Problem(statusCode: 404, title: "Not Found", detail: "This endpoint is available only in Development environment.");
            }

            var sw = Stopwatch.StartNew();
            var traceId = HttpContext.TraceIdentifier;
            var orgIdStr = User.FindFirstValue("orgId");
            Guid.TryParse(orgIdStr, out var orgId);

            // Stub UserApprovalRequest-shaped response
            var response = new
            {
                kind = "user_approval_request",
                title = "Suggested follow-up",
                description = "Would you like to send this follow-up message to the contact?",
                actions = new object[]
                {
                    new {
                        id = "send_followup",
                        label = "Send follow-up",
                        type = "confirm",
                        payload = new {
                            contactId = body.ContactId,
                            message = "Hi there — following up on our last conversation. Are you available for a quick call this week?",
                        }
                    },
                    new {
                        id = "dismiss",
                        label = "Dismiss",
                        type = "cancel"
                    }
                },
                metadata = new { orgId, traceId }
            };
            sw.Stop();
            _logger.LogInformation("{Endpoint} returned stub traceId={TraceId} orgId={OrgId} durationMs={Duration}", "POST /api/agent/suggest-followup", traceId, orgId, sw.ElapsedMilliseconds);
            return Ok(response);
        }
    }
}
