using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Emma.Api.Telemetry;

public sealed class ReplayTelemetryEnricher : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        var props = telemetry.Context.GlobalProperties;
        props.TryAdd("procedureId", "");
        props.TryAdd("procedureVersion", "");
        props.TryAdd("traceId", "");
        props.TryAdd("tenantId", "");
        props.TryAdd("replay", "");
        props.TryAdd("fallback", "");
        props.TryAdd("overrideRequired", "");
    }
}
