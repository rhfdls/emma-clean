using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Emma.Api.Services;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Emma.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/contacts/{contactId}/interactions")]
    public class InteractionController : ControllerBase
    {
        private readonly EmmaDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<InteractionController> _logger;
        private const int MaxContentLength = 8000; // safety rail; large bodies should be moved to blob

        public InteractionController(EmmaDbContext db, IConfiguration config, ILogger<InteractionController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public class CreateInteractionRequest
        {
            public string? Type { get; set; }
            public string? Direction { get; set; }
            public string? Subject { get; set; }
            public string? Content { get; set; }
            public bool ConsentGranted { get; set; } = true;
            public DateTime? OccurredAt { get; set; }
            public string? PrivacyLevel { get; set; }
            public List<string>? Tags { get; set; }
        }

        // POST /contacts/{id}/interactions
        [Authorize(Policy = "VerifiedUser")]
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> LogInteraction(Guid contactId, [FromBody] CreateInteractionRequest body, CancellationToken ct)
        {
            if (body is null)
            {
                return ProblemFactory.Create(HttpContext, 400, "Validation failed", "Request body is required.", ProblemFactory.ValidationError).ToResult();
            }
            var sw = Stopwatch.StartNew();
            var traceId = HttpContext.TraceIdentifier;
            var orgIdStr = User.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdStr) || !Guid.TryParse(orgIdStr, out var orgId))
            {
                return ProblemFactory.Create(HttpContext, 400, "Missing org context", "Missing or invalid orgId claim.", ProblemFactory.ValidationError).ToResult();
            }
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            Guid.TryParse(userIdStr, out var userId);

            // Validate required privacy fields per UNIFIED_SCHEMA
            var privacy = string.IsNullOrWhiteSpace(body.PrivacyLevel) ? null : body.PrivacyLevel!.Trim().ToLowerInvariant();
            var allowedPrivacy = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "public", "internal", "private", "confidential" };
            if (privacy is null || !allowedPrivacy.Contains(privacy))
            {
                return ProblemFactory.Create(HttpContext, 400, "Validation failed", "privacyLevel is required and must be one of: public, internal, private, confidential.", ProblemFactory.ValidationError).ToResult();
            }
            if (body.Content != null && body.Content.Length > MaxContentLength)
            {
                return ProblemFactory.Create(HttpContext, 422, "Content too large", $"Content length exceeds {MaxContentLength} characters. Move large payloads to blob storage.", ProblemFactory.Unprocessable).ToResult();
            }

            // Ownership check: contact must exist and belong to orgId
            var contact = await _db.Set<Contact>().FirstOrDefaultAsync(c => c.Id == contactId, ct);
            if (contact == null)
            {
                return ProblemFactory.Create(HttpContext, 404, "Contact not found", $"Contact {contactId} does not exist.", ProblemFactory.NotFound).ToResult();
            }
            if (contact.OrganizationId != orgId)
            {
                return ProblemFactory.Create(HttpContext, 403, "Organization mismatch", "You do not have access to this contact in the specified organization.", ProblemFactory.OrgMismatch).ToResult();
            }

            var interaction = new Interaction
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                OrganizationId = orgId,
                TenantId = orgId, // demo: tenant == org in dev
                Type = string.IsNullOrWhiteSpace(body.Type) ? "other" : body.Type!,
                Direction = string.IsNullOrWhiteSpace(body.Direction) ? "inbound" : body.Direction!,
                Subject = body.Subject,
                Content = body.Content,
                Status = "completed",
                // Event time: map occurredAt -> StartedAt; default to now (UTC) when missing
                StartedAt = body.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
                // Record time: server-side now
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PrivacyLevel = privacy!,
                Tags = body.Tags ?? new List<string>()
            };
            if (userId != Guid.Empty)
            {
                interaction.CreatedById = userId;
            }

            // Persist the interaction
            _db.Set<Interaction>().Add(interaction);
            await _db.SaveChangesAsync(ct);
            
            // Build simple response (analysis disabled in this repo)
            sw.Stop();
            _logger.LogInformation("{Endpoint} created interaction traceId={TraceId} orgId={OrgId} durationMs={Duration}", "POST /api/contacts/{id}/interactions", traceId, orgId, sw.ElapsedMilliseconds);
            return CreatedAtAction(nameof(GetInteractions), new { contactId }, new { interactionId = interaction.Id, analysisPresent = false, traceId });
        }

        // GET /contacts/{id}/interactions
        [Authorize(Policy = "VerifiedUser")]
        [HttpGet]
        public async Task<IActionResult> GetInteractions(Guid contactId, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var traceId = HttpContext.TraceIdentifier;
            var orgIdClaim = User?.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var orgId))
            {
                return ProblemFactory.Create(HttpContext, 400, "Missing org context", "Missing or invalid orgId claim.", ProblemFactory.ValidationError).ToResult();
            }
            var query = _db.Set<Interaction>().AsQueryable();
            query = query.Where(i => i.OrganizationId == orgId);

            var list = await query
                .Where(i => i.ContactId == contactId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            // Project compact response with analysisSummary if available
            var shaped = new List<object>(list.Count);
            foreach (var i in list)
            {
                string? summary = null;
                // Analysis disabled in this repo; leave summary null
                shaped.Add(new
                {
                    id = i.Id,
                    // Prefer event time when present; otherwise fallback to record creation time
                    timestamp = i.StartedAt ?? i.CreatedAt,
                    type = i.Type,
                    subject = i.Subject,
                    content = i.Content,
                    analysisSummary = summary
                });
            }
            sw.Stop();
            _logger.LogInformation("{Endpoint} fetched interactions traceId={TraceId} contactId={ContactId} durationMs={Duration}", "GET /api/contacts/{id}/interactions", traceId, contactId, sw.ElapsedMilliseconds);
            return Ok(shaped);
        }
    }
}

