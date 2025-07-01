using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemoController : ControllerBase
    {
        // Sample GET endpoint
        [HttpGet("/sample")]
        public IActionResult GetSample()
        {
            return Ok("This is a sample response from DemoController.");
        }

        // Sample POST endpoint
        [HttpPost("/sample")]
        public IActionResult PostSample([FromBody] object data)
        {
            // Process the incoming data
            return Ok("Data received successfully.");
        }
    }
}
