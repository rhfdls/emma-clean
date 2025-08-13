using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/contacts/{contactId}/interactions")]
    public class InteractionController : ControllerBase
    {
        // POST /contacts/{id}/interactions
        [HttpPost]
        public IActionResult LogInteraction(int contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }

        // GET /contacts/{id}/interactions
        [HttpGet]
        public IActionResult GetInteractions(int contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }
    }
}
