using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Emma.Api.Config;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Emma.Api.Controllers
{
    // SPRINT1: Cosmos DB-backed enum controller with local fallback
    [AllowAnonymous]
    [ApiController]
    [Route("api/enums")]
    public class EnumController : ControllerBase
    {
        private readonly CosmosEnumConfig _cosmosConfig;
        private readonly IConfiguration _config;
        private const string ConfigPath = "Config/enum-config.json";

        public EnumController(IConfiguration config)
        {
            _config = config;
            _cosmosConfig = new CosmosEnumConfig(config);
        }

        [HttpGet("{type}")]
        public async Task<IActionResult> GetEnum(string type)
        {
            try
            {
                var cosmosResults = await _cosmosConfig.GetEnumAsync(type);
                if (cosmosResults != null && cosmosResults.Count > 0)
                    return Ok(cosmosResults);
            }
            catch
            {
                // Cosmos unavailable, fall back to local
            }
            // Fallback to local config
            if (!System.IO.File.Exists(ConfigPath))
                return Problem(statusCode: 404, title: "Not Found", detail: "Local enum configuration file was not found.");
            var json = await System.IO.File.ReadAllTextAsync(ConfigPath);
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(type, out var values))
                return Problem(statusCode: 404, title: "Not Found", detail: $"Enum type '{type}' not found in configuration.");
            return Ok(JsonSerializer.Deserialize<object>(values.GetRawText()));
        }
    }
}
