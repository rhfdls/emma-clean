using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Core.Services
{
    public interface ISimulationEventHandler
    {
        Task OnSimulationTimeChangedAsync(DateTime simulationTime, TimeSpan elapsed);
        Task OnSimulationPausedAsync(DateTime simulationTime);
        Task OnSimulationResumedAsync(DateTime simulationTime);
        Task OnSimulationSpeedChangedAsync(DateTime simulationTime, double newSpeed);
    }

    public class TimeSimulationOptions
    {
        public const string SectionName = "TimeSimulation";
        public double DefaultTimeScale { get; set; } = 1.0; // 1 real second = 1 simulated day
        public bool StartPaused { get; set; } = true;
        public double MinTimeScale { get; set; } = 0.1;
        public double MaxTimeScale { get; set; } = 1000.0;
    }

    public class TimeSimulatorService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IOptions<TimeSimulationOptions> _options;
        private readonly ILogger<TimeSimulatorService> _logger;
        private DateTime _simulationStartTime;
        private DateTime _currentSimulationTime;
        private bool _isPaused;
        private double _timeScale;
        private readonly List<ISimulationEventHandler> _eventHandlers = new();
        private readonly object _syncLock = new();
        private DateTime _lastProcessedTime = DateTime.MinValue;

        public TimeSimulatorService(
            IServiceProvider services,
            IOptions<TimeSimulationOptions> options,
            ILogger<TimeSimulatorService> logger,
            IEnumerable<ISimulationEventHandler>? eventHandlers = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _timeScale = _options.Value.DefaultTimeScale.Clamp(
                _options.Value.MinTimeScale, 
                _options.Value.MaxTimeScale);
                
            _isPaused = _options.Value.StartPaused;
            _simulationStartTime = DateTime.UtcNow;
            _currentSimulationTime = _simulationStartTime;
            
            if (eventHandlers != null)
            {
                _eventHandlers.AddRange(eventHandlers);
            }
            
            _logger.LogInformation("TimeSimulatorService initialized with TimeScale: {TimeScale}, Paused: {IsPaused}", 
                _timeScale, _isPaused);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Time simulation service starting...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var realElapsed = now - _simulationStartTime;
                    var previousTime = _currentSimulationTime;
                    
                    if (!_isPaused)
                    {
                        _currentSimulationTime = _simulationStartTime.Add(realElapsed * _timeScale);
                        
                        // Only process if time has advanced
                        if (_currentSimulationTime > _lastProcessedTime)
                        {
                            var timeDelta = _currentSimulationTime - _lastProcessedTime;
                            await ProcessTimeBasedEvents(_currentSimulationTime, timeDelta, stoppingToken);
                            await NotifyTimeChangedAsync(_currentSimulationTime, timeDelta);
                            _lastProcessedTime = _currentSimulationTime;
                        }
                    }
                    
                    // Use a smaller delay for more responsive pausing
                    await Task.Delay(100, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Time simulation service stopping...");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in time simulation service");
                    await Task.Delay(1000, stoppingToken); // Prevent tight error loop
                }
            }
        }

        public DateTime CurrentSimulationTime 
        { 
            get 
            {
                lock (_syncLock)
                {
                    return _currentSimulationTime;
                }
            }
        }
        
        public bool IsPaused => _isPaused;
        
        public double TimeScale 
        { 
            get => _timeScale;
            private set => _timeScale = value.Clamp(_options.Value.MinTimeScale, _options.Value.MaxTimeScale);
        }
        
        public async Task SetPausedAsync(bool paused, CancellationToken ct = default)
        {
            if (_isPaused == paused) return;
            
            lock (_syncLock)
            {
                _isPaused = paused;
                _simulationStartTime = DateTime.UtcNow - (_currentSimulationTime - _simulationStartTime);
            }
            
            _logger.LogInformation("Simulation {State}", paused ? "paused" : "resumed");
            
            if (paused)
            {
                await NotifySimulationPausedAsync(_currentSimulationTime);
            }
            else
            {
                await NotifySimulationResumedAsync(_currentSimulationTime);
            }
        }
        
        public async Task SetTimeScaleAsync(double scale, CancellationToken ct = default)
        {
            if (Math.Abs(scale - _timeScale) < 0.001) return;
            
            var previousScale = _timeScale;
            var previousTime = _currentSimulationTime;
            
            lock (_syncLock)
            {
                TimeScale = scale;
                _simulationStartTime = DateTime.UtcNow - ((_currentSimulationTime - _simulationStartTime) * (previousScale / scale));
            }
            
            _logger.LogInformation("Simulation speed changed from {PreviousScale}x to {NewScale}x", 
                previousScale, scale);
                
            await NotifySpeedChangedAsync(_currentSimulationTime, scale);
        }

        private async Task ProcessTimeBasedEvents(DateTime currentTime, TimeSpan elapsed, CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            try
            {
                // Process any time-based business logic here
                // Example: Check for due tasks, trigger follow-ups, etc.
                
                _logger.LogDebug("Processing time-based events for {SimulationTime} (elapsed: {Elapsed})", 
                    currentTime, elapsed);
                    
                // Process due tasks
                var dueTasks = await dbContext.TaskItems
                    .Where(t => t.DueDate <= currentTime && t.Status != "Completed")
                    .ToListAsync(ct);
                    
                foreach (var task in dueTasks)
                {
                    _logger.LogInformation("Task {TaskId} is now due: {TaskTitle}", 
                        task.TaskId, task.Title);
                    // Additional processing...
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing time-based events");
                throw;
            }
        }
        
        private async Task NotifyTimeChangedAsync(DateTime currentTime, TimeSpan elapsed)
        {
            if (_eventHandlers.Count == 0) return;
            
            try
            {
                await Task.WhenAll(_eventHandlers
                    .Select(h => h.OnSimulationTimeChangedAsync(currentTime, elapsed)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in time change notification handler");
            }
        }
        
        private async Task NotifySimulationPausedAsync(DateTime currentTime)
        {
            if (_eventHandlers.Count == 0) return;
            
            try
            {
                await Task.WhenAll(_eventHandlers
                    .Select(h => h.OnSimulationPausedAsync(currentTime)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simulation paused notification handler");
            }
        }
        
        private async Task NotifySimulationResumedAsync(DateTime currentTime)
        {
            if (_eventHandlers.Count == 0) return;
            
            try
            {
                await Task.WhenAll(_eventHandlers
                    .Select(h => h.OnSimulationResumedAsync(currentTime)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simulation resumed notification handler");
            }
        }
        
        private async Task NotifySpeedChangedAsync(DateTime currentTime, double newSpeed)
        {
            if (_eventHandlers.Count == 0) return;
            
            try
            {
                await Task.WhenAll(_eventHandlers
                    .Select(h => h.OnSimulationSpeedChangedAsync(currentTime, newSpeed)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in speed changed notification handler");
            }
        }
    }

    public static class DoubleExtensions
    {
        public static double Clamp(this double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
