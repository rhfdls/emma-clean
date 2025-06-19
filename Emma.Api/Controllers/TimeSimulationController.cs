using System;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// Controller for managing time simulation in the EMMA system.
    /// Allows controlling the simulation clock speed and pausing/resuming the simulation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensure only authenticated users can control the simulation
    public class TimeSimulationController : ControllerBase
    {
        private readonly TimeSimulatorService _timeSimulator;
        private readonly ILogger<TimeSimulationController> _logger;

        public TimeSimulationController(
            TimeSimulatorService timeSimulator,
            ILogger<TimeSimulationController> logger)
        {
            _timeSimulator = timeSimulator ?? throw new ArgumentNullException(nameof(timeSimulator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current simulation time and status.
        /// </summary>
        /// <response code="200">Returns the current simulation time and status</response>
        [HttpGet("status")]
        [ProducesResponseType(200)]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                SimulationTime = _timeSimulator.CurrentSimulationTime,
                IsPaused = _timeSimulator.IsPaused,
                TimeScale = _timeSimulator.TimeScale
            });
        }

        /// <summary>
        /// Pauses the time simulation.
        /// </summary>
        /// <response code="200">Simulation was successfully paused</response>
        [HttpPost("pause")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PauseSimulation(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Pausing time simulation");
            await _timeSimulator.SetPausedAsync(true, cancellationToken);
            return Ok(new { Message = "Simulation paused" });
        }

        /// <summary>
        /// Resumes the time simulation.
        /// </summary>
        /// <response code="200">Simulation was successfully resumed</response>
        [HttpPost("resume")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ResumeSimulation(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resuming time simulation");
            await _timeSimulator.SetPausedAsync(false, cancellationToken);
            return Ok(new { Message = "Simulation resumed" });
        }

        /// <summary>
        /// Toggles the pause state of the time simulation.
        /// </summary>
        /// <response code="200">Simulation pause state was toggled</response>
        [HttpPost("toggle-pause")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> TogglePause(CancellationToken cancellationToken = default)
        {
            var newState = !_timeSimulator.IsPaused;
            await _timeSimulator.SetPausedAsync(newState, cancellationToken);
            
            _logger.LogInformation("Time simulation {State}", newState ? "paused" : "resumed");
            return Ok(new 
            { 
                Message = $"Simulation { (newState ? "paused" : "resumed") }",
                IsPaused = newState
            });
        }

        /// <summary>
        /// Sets the time scale factor for the simulation.
        /// </summary>
        /// <param name="scale">The time scale factor (e.g., 1.0 = real time, 60.0 = 1 minute = 1 hour, 1440.0 = 1 day = 1 minute)</param>
        /// <response code="200">Time scale was successfully updated</response>
        /// <response code="400">Invalid time scale value</response>
        [HttpPost("timescale")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetTimeScale(
            [FromQuery] double scale, 
            CancellationToken cancellationToken = default)
        {
            if (scale <= 0)
            {
                return BadRequest("Time scale must be greater than zero");
            }

            try
            {
                var previousScale = _timeSimulator.TimeScale;
                await _timeSimulator.SetTimeScaleAsync(scale, cancellationToken);
                
                _logger.LogInformation("Time scale changed from {PreviousScale}x to {NewScale}x", 
                    previousScale, scale);
                    
                return Ok(new 
                { 
                    Message = $"Time scale set to {scale}x",
                    PreviousScale = previousScale,
                    NewScale = scale
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting time scale to {Scale}", scale);
                return StatusCode(500, "An error occurred while updating the time scale");
            }
        }

        /// <summary>
        /// Sets a preset time scale.
        /// </summary>
        /// <param name="preset">The preset to use (realtime, day, week, month, year)</param>
        /// <response code="200">Time scale was successfully updated</response>
        /// <response code="400">Invalid preset value</response>
        [HttpPost("preset/{preset}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetPresetTimeScale(
            [FromRoute] string preset,
            CancellationToken cancellationToken = default)
        {
            var presetScale = preset.ToLowerInvariant() switch
            {
                "realtime" => 1.0,
                "day" => 1440.0,     // 1 day = 1 minute (1440 minutes in a day)
                "week" => 10080.0,   // 1 week = 1 minute
                "month" => 43800.0,  // ~1 month = 1 minute (30.42 days)
                "year" => 525600.0,  // 1 year = 1 minute (365 days)
                _ => -1
            };

            if (presetScale <= 0)
            {
                return BadRequest($"Invalid preset: {preset}. Valid presets are: realtime, day, week, month, year");
            }

            try
            {
                var previousScale = _timeSimulator.TimeScale;
                await _timeSimulator.SetTimeScaleAsync(presetScale, cancellationToken);
                
                _logger.LogInformation("Time scale changed from {PreviousScale}x to {NewScale}x using preset '{Preset}'", 
                    previousScale, presetScale, preset);
                
                return Ok(new 
                { 
                    Message = $"Time scale set to {presetScale}x using preset '{preset}'",
                    Preset = preset,
                    PreviousScale = previousScale,
                    NewScale = presetScale
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting time scale preset to {Preset}", preset);
                return StatusCode(500, "An error occurred while updating the time scale");
            }
        }
    }
}
