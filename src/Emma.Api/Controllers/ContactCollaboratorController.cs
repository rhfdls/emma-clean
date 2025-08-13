using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/contacts/{contactId}/collaborators")]
    public class ContactCollaboratorController : ControllerBase
    {
        // POST /contacts/{id}/collaborators
        [HttpPost]
        public IActionResult AddCollaborator(int contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }

        // GET /contacts/{id}/collaborators
        [HttpGet]
        public IActionResult GetCollaborators(int contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }
    }
}
