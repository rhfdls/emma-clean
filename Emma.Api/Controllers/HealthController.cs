using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// Controller for health monitoring of the Emma AI Platform
    /// Provides endpoints to check the health of all integrated services
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = "Emma.Api")]
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
        /// Comprehensive health check for all Emma AI Platform services
        /// Returns 200 OK with detailed status of each component
        /// GET /health
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public async Task<IActionResult> Get([FromServices] Emma.Api.Services.CosmosAgentRepository? cosmosRepo = null)
        {
            var healthStatus = new Dictionary<string, object>();
            var overallStatus = HealthStatus.Healthy;

            try
            {
                // Check Azure PostgreSQL
                await _pgConnection.OpenAsync();
                await _pgConnection.CloseAsync();
                healthStatus["azurePostgreSql"] = new { status = "healthy", message = "Successfully connected to Azure PostgreSQL" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure PostgreSQL connection");
                healthStatus["azurePostgreSql"] = new { status = "unhealthy", message = ex.Message };
                overallStatus = HealthStatus.Unhealthy;
            }

            // Check Azure Cosmos DB if service is available
            if (cosmosRepo != null)
            {
                try
                {
                    // Try a simple query to Cosmos DB
                    var result = await cosmosRepo.QueryItemsAsync<object>("SELECT TOP 1 * FROM c");
                    healthStatus["azureCosmosDb"] = new { status = "healthy", message = "Successfully connected to Azure Cosmos DB" };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking Azure Cosmos DB connection");
                    healthStatus["azureCosmosDb"] = new { status = "unhealthy", message = ex.Message };
                    overallStatus = HealthStatus.Unhealthy;
                }
            }
            else
            {
                healthStatus["azureCosmosDb"] = new { status = "unknown", message = "Cosmos DB service not available or not configured" };
            }

            // Check Azure OpenAI integration
            try
            {
                // Basic availability check
                var openAiEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");
                var openAiKey = Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey");
                
                if (string.IsNullOrEmpty(openAiEndpoint) || string.IsNullOrEmpty(openAiKey))
                {
                    healthStatus["azureOpenAI"] = new { status = "warning", message = "Azure OpenAI configuration incomplete" };
                }
                else 
                {
                    healthStatus["azureOpenAI"] = new { status = "healthy", message = "Azure OpenAI configured" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure OpenAI configuration");
                healthStatus["azureOpenAI"] = new { status = "warning", message = ex.Message };
            }

            // Add overall status
            healthStatus["overall"] = new { status = overallStatus.ToString().ToLower(), timestamp = DateTime.UtcNow };
            
            return Ok(healthStatus);
        }

        /// <summary>
        /// Health check endpoint for Azure PostgreSQL connectivity.
        /// Returns 200 OK if the API can connect to Azure PostgreSQL; otherwise, returns 500 with error details.
        /// GET /health/postgresql
        /// </summary>
        [HttpGet("postgresql")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> PostgreSqlHealth()
        {
            try
            {
                await _pgConnection.OpenAsync();
                await _pgConnection.CloseAsync();
                return Ok(new { status = "healthy", message = "Successfully connected to Azure PostgreSQL" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure PostgreSQL connection");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint for Azure Cosmos DB connectivity.
        /// Returns 200 OK if the API can connect to Cosmos DB; otherwise, returns 500 with error details.
        /// GET /health/cosmosdb
        /// </summary>
        [HttpGet("cosmosdb")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CosmosDbHealth([FromServices] Emma.Api.Services.CosmosAgentRepository cosmosRepo)
        {
            try
            {
                // Try a simple query to Cosmos DB (list items or ping)
                var result = await cosmosRepo.QueryItemsAsync<object>("SELECT TOP 1 * FROM c");
                return Ok(new { status = "healthy", message = "Successfully connected to Azure Cosmos DB" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure Cosmos DB connection");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
