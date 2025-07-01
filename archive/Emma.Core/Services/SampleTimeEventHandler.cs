using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services
{
    /// <summary>
    /// Sample implementation of ISimulationEventHandler that logs time simulation events.
    /// This demonstrates how to extend the time simulation with custom event handling.
    /// </summary>
    public class SampleTimeEventHandler : ISimulationEventHandler
    {
        private readonly ILogger<SampleTimeEventHandler> _logger;

        public SampleTimeEventHandler(ILogger<SampleTimeEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task OnSimulationTimeChangedAsync(DateTime simulationTime, TimeSpan elapsed)
        {
            // Only log every minute to avoid log spam
            if (simulationTime.Second == 0)
            {
                _logger.LogInformation("🕒 Simulation time updated: {SimulationTime:yyyy-MM-dd HH:mm:ss} (elapsed: {Elapsed})", 
                    simulationTime, elapsed);
            }
            return Task.CompletedTask;
        }

        public Task OnSimulationPausedAsync(DateTime simulationTime)
        {
            _logger.LogInformation("⏸️ Simulation PAUSED at {SimulationTime:yyyy-MM-dd HH:mm:ss}", simulationTime);
            return Task.CompletedTask;
        }

        public Task OnSimulationResumedAsync(DateTime simulationTime)
        {
            _logger.LogInformation("▶️ Simulation RESUMED at {SimulationTime:yyyy-MM-dd HH:mm:ss}", simulationTime);
            return Task.CompletedTask;
        }

        public Task OnSimulationSpeedChangedAsync(DateTime simulationTime, double newSpeed)
        {
            _logger.LogInformation("⚡ Simulation speed changed to {NewSpeed}x at {SimulationTime:yyyy-MM-dd HH:mm:ss}", 
                newSpeed, simulationTime);
            return Task.CompletedTask;
        }
    }
}
