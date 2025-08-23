using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/contacts/{contactId}/collaborators")]
    public class ContactCollaboratorController : ControllerBase
    {
        // POST /contacts/{id}/collaborators
        [Authorize(Policy = "VerifiedUser")]
        [HttpPost]
        public IActionResult AddCollaborator(Guid contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }

        // GET /contacts/{id}/collaborators
        [HttpGet]
        public IActionResult GetCollaborators(Guid contactId)
        {
            // TODO: Implement
            return StatusCode(501);
        }
    }
}
