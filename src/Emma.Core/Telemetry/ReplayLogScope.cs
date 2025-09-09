using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Telemetry;

public static class ReplayLogScope
{
    public static IDisposable BeginReplayScope(
        ILogger logger,
        string procedureId = "",
        int? procedureVersion = null,
        string traceId = "",
        string tenantId = "",
        bool replay = false,
        bool fallback = false,
        bool overrideRequired = false)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["procedureId"] = procedureId ?? string.Empty,
            ["procedureVersion"] = procedureVersion?.ToString() ?? string.Empty,
            ["traceId"] = traceId ?? string.Empty,
            ["tenantId"] = tenantId ?? string.Empty,
            ["replay"] = replay ? "1" : "0",
            ["fallback"] = fallback ? "1" : "0",
            ["overrideRequired"] = overrideRequired ? "1" : "0"
        })!;
    }
}
