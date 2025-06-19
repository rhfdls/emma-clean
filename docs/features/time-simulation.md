# Time Simulation Service

The Time Simulation Service provides a flexible way to simulate the passage of time in the EMMA CRM system. This is particularly useful for testing scenarios where you need to simulate long-term interactions and events without waiting for real time to pass.

## Features

- **Configurable Time Scaling**: Speed up or slow down time based on your needs
- **Pause/Resume**: Pause the simulation at any point
- **Event-Based Notifications**: Get notified when time-related events occur
- **REST API**: Control the simulation via HTTP endpoints
- **Extensible**: Add custom event handlers for time-based logic

## Configuration

Time simulation can be configured in `appsettings.json`:

```json
"TimeSimulation": {
  "DefaultTimeScale": 1.0,
  "StartPaused": true,
  "MinTimeScale": 0.1,
  "MaxTimeScale": 1000.0,
  "Presets": {
    "realtime": 1.0,
    "day": 1440.0,
    "week": 10080.0,
    "month": 43800.0,
    "year": 525600.0
  }
}
```

## API Endpoints

### Get Simulation Status
```
GET /api/timesimulation/status
```

### Pause Simulation
```
POST /api/timesimulation/pause
```

### Resume Simulation
```
POST /api/timesimulation/resume
```

### Toggle Pause/Resume
```
POST /api/timesimulation/toggle
```

### Set Time Scale
```
POST /api/timesimulation/timescale?scale={number}
```

### Set Preset Time Scale
```
POST /api/timesimulation/preset/{preset}
```
Where `{preset}` can be one of: `realtime`, `day`, `week`, `month`, `year`

## Extending with Custom Event Handlers

Create a class that implements `ISimulationEventHandler` and register it in the DI container:

```csharp
public class MyTimeEventHandler : ISimulationEventHandler
{
    private readonly ILogger<MyTimeEventHandler> _logger;

    public MyTimeEventHandler(ILogger<MyTimeEventHandler> logger)
    {
        _logger = logger;
    }

    public Task OnSimulationTimeChangedAsync(DateTime simulationTime, TimeSpan elapsed)
    {
        _logger.LogInformation($"Time changed to {simulationTime}");
        return Task.CompletedTask;
    }

    public Task OnSimulationPausedAsync(DateTime simulationTime)
    {
        _logger.LogInformation($"Simulation paused at {simulationTime}");
        return Task.CompletedTask;
    }

    public Task OnSimulationResumedAsync(DateTime simulationTime)
    {
        _logger.LogInformation($"Simulation resumed at {simulationTime}");
        return Task.CompletedTask;
    }

    public Task OnSimulationSpeedChangedAsync(DateTime simulationTime, double newSpeed)
    {
        _logger.LogInformation($"Simulation speed changed to {newSpeed}x at {simulationTime}");
        return Task.CompletedTask;
    }
}
```

Register the handler in `Program.cs`:

```csharp
builder.Services.AddSingleton<ISimulationEventHandler, MyTimeEventHandler>();
```

## Testing

Unit tests are available in `Emma.Core.Tests/Services/TimeSimulatorServiceTests.cs`.

## Best Practices

1. **Use Appropriate Time Scales**: Be mindful of the time scale setting to avoid performance issues.
2. **Handle Pause States**: Ensure your time-based logic respects the `IsPaused` state.
3. **Log Important Events**: Use the provided event handlers to log important time-based events.
4. **Test Thoroughly**: Test your time-based logic with different time scales.
5. **Consider Time Zones**: Always work with UTC times internally and convert to local time only when displaying to users.
