using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
private readonly Npgsql.NpgsqlConnection _pgConnection;

        public HealthController(ILogger<HealthController> logger, Npgsql.NpgsqlConnection pgConnection)
        {
            _logger = logger;
            _pgConnection = pgConnection;
        }

        /// <summary>
        /// Health check endpoint for PostgreSQL database connectivity.
        /// Returns 200 OK if the API can connect to PostgreSQL; otherwise, returns 500 with error details.
        /// GET /health
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                await _pgConnection.OpenAsync();
                await _pgConnection.CloseAsync();
                return Ok(new { status = "healthy", message = "Successfully connected to PostgreSQL" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking PostgreSQL connection");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        /// <summary>
        /// Health check endpoint for Azure CosmosDB connectivity.
        /// Returns 200 OK if the API can connect to CosmosDB; otherwise, returns 500 with error details.
        /// GET /health/cosmosdb
        /// </summary>
        [HttpGet("cosmosdb")]
        public async Task<IActionResult> CosmosDbHealth([FromServices] Emma.Api.Services.CosmosAgentRepository cosmosRepo)
        {
            try
            {
                // Try a simple query to CosmosDB (list items or ping)
                var result = await cosmosRepo.QueryItemsAsync<object>("SELECT TOP 1 * FROM c");
                return Ok(new { status = "healthy", message = "Successfully connected to CosmosDB" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking CosmosDB connection");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
