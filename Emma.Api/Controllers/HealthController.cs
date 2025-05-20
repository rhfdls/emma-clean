using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
    }
}
