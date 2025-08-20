using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Emma.Api.Services;
using Emma.Api.Interfaces;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/contacts/{contactId}/interactions")]
    public class InteractionController : ControllerBase
    {
        private readonly EmmaDbContext _db;
        private readonly IEmmaAnalysisService _analysis;
        private readonly IAnalysisQueue _queue;
        private readonly IConfiguration _config;
        private const int MaxContentLength = 8000; // safety rail; large bodies should be moved to blob

        public InteractionController(EmmaDbContext db, IEmmaAnalysisService analysis, IAnalysisQueue queue, IConfiguration config)
        {
            _db = db;
            _analysis = analysis;
            _queue = queue;
            _config = config;
        }

        public class CreateInteractionRequest
        {
            public string? Type { get; set; }
            public string? Direction { get; set; }
            public string? Subject { get; set; }
            public string? Content { get; set; }
            public bool ConsentGranted { get; set; } = true;
        }

        // POST /contacts/{id}/interactions
        [Authorize(Policy = "VerifiedUser")]
        [HttpPost]
        public async Task<IActionResult> LogInteraction(Guid contactId, [FromBody] CreateInteractionRequest body, CancellationToken ct)
        {
            if (body is null) return BadRequest();

            var interaction = new Interaction
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Type = string.IsNullOrWhiteSpace(body.Type) ? "other" : body.Type!,
                Direction = string.IsNullOrWhiteSpace(body.Direction) ? "inbound" : body.Direction!,
                Subject = body.Subject,
                Content = body.Content,
                Status = "completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Cap input size; for larger content, future: store to blob and pass trimmed text
            var contentForAnalysis = interaction.Content ?? string.Empty;
            if (contentForAnalysis.Length > MaxContentLength)
            {
                contentForAnalysis = contentForAnalysis.Substring(0, MaxContentLength);
                // TODO: store full content to blob and set URL in CustomFields
            }

            // Persist the interaction first
            _db.Set<Interaction>().Add(interaction);
            await _db.SaveChangesAsync(ct);

            // Consent gate
            if (!body.ConsentGranted)
            {
                // Do not analyze; respond 202 Accepted
                return Accepted(new { id = interaction.Id, analysisQueued = false });
            }

            // Synchronous analysis for demo
            EmmaAnalysisResult? result = null;
            try
            {
                result = await _analysis.AnalyzeAsync(contentForAnalysis, ct);
            }
            catch (InvalidDataException)
            {
                // Schema invalid → 422 per guidance
                return UnprocessableEntity(new { message = "AI output failed schema validation" });
            }

            // Failure semantics: if ERROR, do not write analysis_json; write only run log
            if (result != null)
            {
                var runLogJson = System.Text.Json.JsonSerializer.Serialize(result.RunLog);
                if (string.Equals(result.RunLog.Status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    _db.SetAnalysisJson(interaction, result.Json);
                    _db.SetRunLogJson(interaction, runLogJson);
                }
                else
                {
                    _db.SetRunLogJson(interaction, runLogJson);
                }
                await _db.SaveChangesAsync(ct);
            }

            // Enqueue background job (dev worker may re-run or be no-op) behind feature flag
            var reprocessEnabled = _config.GetValue<bool>("EmmaAnalysis:BackgroundReprocessEnabled", true);
            if (reprocessEnabled)
            {
                await _queue.QueueAsync(new AnalysisJob
                {
                    InteractionId = interaction.Id,
                    InputText = contentForAnalysis
                }, ct);
            }

            // Build response per guidance
            var isOk = result != null && string.Equals(result.RunLog.Status, "OK", StringComparison.OrdinalIgnoreCase);
            var runLogSummary = new
            {
                status = result?.RunLog.Status,
                latencyMs = result?.RunLog.LatencyMs,
                traceId = result?.RunLog.TraceId,
                correlationId = result?.RunLog.CorrelationId,
                timestamp = result?.RunLog.Timestamp
            };
            if (!isOk)
            {
                return CreatedAtAction(nameof(GetInteractions), new { contactId }, new { interactionId = interaction.Id, analysisPresent = false, reason = "AI_UNAVAILABLE", runLogSummary });
            }
            return CreatedAtAction(nameof(GetInteractions), new { contactId }, new { interactionId = interaction.Id, analysisPresent = true, runLogSummary });
        }

        // GET /contacts/{id}/interactions
        [HttpGet]
        public async Task<IActionResult> GetInteractions(Guid contactId, CancellationToken ct)
        {
            var list = await _db.Set<Interaction>()
                .Where(i => i.ContactId == contactId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            // Project compact response with analysisSummary if available
            var shaped = new List<object>(list.Count);
            foreach (var i in list)
            {
                string? summary = null;
                var json = _db.GetAnalysisJson(i);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        var jo = Newtonsoft.Json.Linq.JObject.Parse(json);
                        summary = (string?)jo.SelectToken("notes.summary");
                        if (!string.IsNullOrEmpty(summary) && summary.Length > 240) summary = summary.Substring(0, 240) + "…";
                    }
                    catch { /* ignore parse issues */ }
                }
                shaped.Add(new
                {
                    id = i.Id,
                    timestamp = i.CreatedAt,
                    type = i.Type,
                    subject = i.Subject,
                    content = i.Content,
                    analysisSummary = summary
                });
            }
            return Ok(shaped);
        }
    }
}
