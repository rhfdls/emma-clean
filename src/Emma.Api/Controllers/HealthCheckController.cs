using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                version = typeof(HealthCheckController).Assembly.GetName().Version?.ToString() ?? "unknown"
            });
        }

        // SPRINT1: Cosmos DB health check
        [HttpGet("cosmos")]
        public async Task<IActionResult> CosmosHealth([FromServices] IConfiguration config)
        {
            try
            {
                var endpoint = config["COSMOSDB__ACCOUNTENDPOINT"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
                var key = config["COSMOSDB__ACCOUNTKEY"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
                var db = config["COSMOSDB__DATABASENAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME");
                var container = config["COSMOSDB__ENUMCONTAINERNAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ENUMCONTAINERNAME") ?? "enumConfig";
                Console.WriteLine($"[HealthCheck] Cosmos endpoint: {endpoint}");
                Console.WriteLine($"[HealthCheck] Cosmos key: {(string.IsNullOrEmpty(key) ? "[NULL OR EMPTY]" : "[LOADED]")}");
                var client = new Microsoft.Azure.Cosmos.CosmosClient(endpoint, key);
                try
                {
                    var dbResponse = await client.GetDatabase(db).ReadAsync();
                    var iterator = client.GetDatabase(db).GetContainerQueryIterator<Microsoft.Azure.Cosmos.ContainerProperties>();
                    Console.WriteLine($"[Cosmos Debug] Listing containers in database '{db}':");
                    while (iterator.HasMoreResults)
                    {
                        foreach (var c in await iterator.ReadNextAsync())
                        {
                            Console.WriteLine($"[Cosmos Debug] Container: {c.Id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cosmos Debug] Error listing containers: {ex.Message}");
                }
                var cont = client.GetContainer(db, container);
                var props = await cont.ReadContainerAsync();
                return Ok(new { status = "CosmosDB OK", id = props.Resource.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "CosmosDB Unreachable", error = ex.Message });
            }
        }

        // SPRINT1: PostgreSQL health check
        [HttpGet("postgres")]
        public async Task<IActionResult> PostgresHealth([FromServices] IConfiguration config)
        {
            try
            {
                var connStr = config.GetConnectionString("PostgreSql") ?? config["ConnectionStrings__PostgreSql"];
                using var conn = new Npgsql.NpgsqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", conn);
                var result = await cmd.ExecuteScalarAsync();
                return Ok(new { status = "PostgreSQL OK", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "PostgreSQL Unreachable", error = ex.Message });
            }
        }
        // SPRINT1: Cosmos DB document-level health check
        [HttpGet("cosmos/item")]
        public async Task<IActionResult> CosmosItemHealth([FromServices] IConfiguration config)
        {
            var endpoint = config["COSMOSDB__ACCOUNTENDPOINT"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
            var key = config["COSMOSDB__ACCOUNTKEY"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
            var db = config["COSMOSDB__DATABASENAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME");
            var container = config["COSMOSDB__CONTAINERNAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__CONTAINERNAME") ?? "messages";
            string id = "msg-20250704-0001";
            string partitionKey = "outbound-sms";
            try
            {
                var client = new Microsoft.Azure.Cosmos.CosmosClient(endpoint, key);
                var cont = client.GetContainer(db, container);
                Console.WriteLine($"[Cosmos Item Health] Reading id={id}, partitionKey={partitionKey}");
                var response = await cont.ReadItemAsync<Emma.Api.Models.MessageItem>(id, new Microsoft.Azure.Cosmos.PartitionKey(partitionKey));
                return Ok(new { status = "CosmosDB Item OK", item = response.Resource });
            }
            catch (Microsoft.Azure.Cosmos.CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { status = "CosmosDB Item Not Found", id, partitionKey });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "CosmosDB Item Error", error = ex.Message });
            }
        }
    }
}
