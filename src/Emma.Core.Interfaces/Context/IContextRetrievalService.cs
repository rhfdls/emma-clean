using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces.Context;

public interface IContextRetrievalService
{
    Task<RetrievalBundle> GetAsync(RetrievalQuery query, CancellationToken ct);
}

public sealed record RetrievalQuery(
    Guid TenantId, Guid OrganizationId, Guid? UserId, Guid? ContactId,
    string ActionType, string Channel, string Industry, string RiskBand,
    IReadOnlyDictionary<string,object?>? Hints = null,
    int MaxSnippets = 8
);

public sealed record RetrievalBundle(
    string RollingSummary,
    IReadOnlyList<ContextSnippet> Snippets,
    IReadOnlyDictionary<string,string> PolicyDirectives
);

public sealed record ContextSnippet(
    string Kind,
    string RefId,
    string RedactedText,
    DateTimeOffset WhenUtc,
    IReadOnlyList<string> Tags
);
