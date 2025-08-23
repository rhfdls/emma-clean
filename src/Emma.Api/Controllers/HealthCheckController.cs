using Microsoft.AspNetCore.Mvc;
using Emma.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthCheckController : ControllerBase
    {
        // Liveness: process is up
        [HttpGet("live")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Live()
        {
            return Ok(new { status = "Live" });
        }

        // Readiness: dependencies OK (Postgres/Cosmos)
        [HttpGet("ready")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Ready([FromServices] IConfiguration config)
        {
            // Check Postgres
            try
            {
                var connStr = config.GetConnectionString("PostgreSql") ?? config["ConnectionStrings__PostgreSql"];
                using var conn = new Npgsql.NpgsqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", conn);
                _ = await cmd.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                return ProblemFactory.Create(HttpContext, 503, "Dependency Unhealthy", $"PostgreSQL not ready: {ex.Message}", ProblemFactory.DependencyUnhealthy).ToResult();
            }

            // Check Cosmos
            try
            {
                var endpoint = config["COSMOSDB__ACCOUNTENDPOINT"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
                var key = config["COSMOSDB__ACCOUNTKEY"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
                var db = config["COSMOSDB__DATABASENAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME");
                var client = new Microsoft.Azure.Cosmos.CosmosClient(endpoint, key);
                var dbResponse = await client.GetDatabase(db).ReadAsync();
            }
            catch (Exception ex)
            {
                return ProblemFactory.Create(HttpContext, 503, "Dependency Unhealthy", $"CosmosDB not ready: {ex.Message}", ProblemFactory.DependencyUnhealthy).ToResult();
            }

            return Ok(new { status = "Ready" });
        }

        // SPRINT1: Cosmos DB health check
        [HttpGet("cosmos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
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
                return ProblemFactory.Create(HttpContext, 503, "CosmosDB Unreachable", ex.Message, ProblemFactory.DependencyUnhealthy).ToResult();
            }
        }

        // SPRINT1: PostgreSQL health check
        [HttpGet("postgres")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
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
                return ProblemFactory.Create(HttpContext, 503, "PostgreSQL Unreachable", ex.Message, ProblemFactory.DependencyUnhealthy).ToResult();
            }
        }
        // SPRINT1: Cosmos DB document-level health check
        [HttpGet("cosmos/item")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
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
                return ProblemFactory.Create(HttpContext, 404, "CosmosDB Item Not Found", $"Item id={id}, partitionKey={partitionKey} not found.", ProblemFactory.NotFound).ToResult();
            }
            catch (Exception ex)
            {
                return ProblemFactory.Create(HttpContext, 503, "CosmosDB Item Error", ex.Message, ProblemFactory.DependencyUnhealthy).ToResult();
            }
        }
    }
}

