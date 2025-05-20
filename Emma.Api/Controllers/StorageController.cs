using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly string connectionString;

        public StorageController(ILogger<StorageController> logger, IConfiguration configuration)
        {
            _logger = logger;
            connectionString = configuration.GetSection("Storage:ConnectionString").Value;
        }
        private readonly ILogger<StorageController> _logger;



        [HttpGet("test")]
        public async Task<IActionResult> TestStorage()
        {
            try
            {
                _logger.LogInformation("Starting Azure Storage test connection");
                
                // Create a blob service client
                var blobServiceClient = new BlobServiceClient(connectionString);
                _logger.LogInformation("Created blob service client");

                // Create a unique name for the container
                string containerName = "test-container" + Guid.NewGuid().ToString();
                _logger.LogInformation($"Creating container: {containerName}");

                // Create the container and get the container client
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateAsync();
                _logger.LogInformation($"Successfully created container: {containerName}");

                // List all containers
                var containers = blobServiceClient.GetBlobContainersAsync();
                var containerList = new List<BlobContainerItem>();
                await foreach (var container in containers)
                {
                    containerList.Add(container);
                }
                _logger.LogInformation($"Found {containerList.Count} containers");

                // Delete the test container after successful test
                await containerClient.DeleteAsync();
                _logger.LogInformation($"Deleted container: {containerName}");

                return Ok(new
                {
                    status = "success",
                    message = "Successfully connected to Azure Storage",
                    containerCount = containerList.Count
                });
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage Request Failed");
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message,
                    statusCode = ex.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Azure Storage test");
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
