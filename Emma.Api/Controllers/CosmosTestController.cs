using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Emma.Api.Services;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/cosmos/test")]
    public class CosmosTestController : ControllerBase
    {
        private readonly CosmosAgentRepository _cosmosRepo;
        private readonly ILogger<CosmosTestController> _logger;

        public CosmosTestController(CosmosAgentRepository cosmosRepo, ILogger<CosmosTestController> logger)
        {
            _cosmosRepo = cosmosRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Write a test item (partition key is 'id')
            var newId = Guid.NewGuid().ToString();
            var testItem = new TestItem { id = newId, message = "Hello from Cosmos DB!", timestamp = DateTime.UtcNow };
            await _cosmosRepo.UpsertItemAsync(testItem, newId);

            // Read it back as TestItem
            var fetched = await _cosmosRepo.GetItemAsync<TestItem>(newId, newId);
            return Ok(new { written = testItem, fetched });
        }
    }
}
