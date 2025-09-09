using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.Context;

namespace Emma.Core.Context;

public sealed class ContextRetrievalService : IContextRetrievalService
{
    public async Task<RetrievalBundle> GetAsync(RetrievalQuery q, CancellationToken ct)
    {
        // TODO: Retrieve recent interactions/messages with privacy filtering by Interaction.Tags,
        //       enforce tenant isolation and RBAC, add vector search when available.
        // SPRINT2: Phase0 — thread industry through policy so planner prompts can include it in Phase 1
        return await Task.FromResult(new RetrievalBundle(
            RollingSummary: "No summary yet (stub).",
            Snippets: Array.Empty<ContextSnippet>(),
            PolicyDirectives: new Dictionary<string, string>
            {
                ["safetyMode"] = "standard",
                ["industry"] = string.IsNullOrWhiteSpace(q.Industry) ? "general" : q.Industry
            }
        ));
    }
}
