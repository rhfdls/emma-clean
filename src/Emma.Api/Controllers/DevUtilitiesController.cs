using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/dev")] // Development helpers
    public class DevUtilitiesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly EmmaDbContext _db;
        private readonly ILogger<DevUtilitiesController> _logger;

        public DevUtilitiesController(IWebHostEnvironment env, EmmaDbContext db, ILogger<DevUtilitiesController> logger)
        {
            _env = env; _db = db; _logger = logger;
        }

        public class IdLookupResponse
        {
            public Guid? UserId { get; set; }
            public Guid? OrganizationId { get; set; }
            public bool FoundUser { get; set; }
            public bool FoundOrganization { get; set; }
        }

        // GET /api/dev/lookup-ids?email=...
        [HttpGet("lookup-ids")]
        [AllowAnonymous]
        public async Task<IActionResult> LookupIds([FromQuery] string email)
        {
            if (!_env.IsDevelopment()) return NotFound();
            if (string.IsNullOrWhiteSpace(email))
                return Problem(statusCode: 400, title: "Validation failed", detail: "email is required");

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                return Ok(new IdLookupResponse
                {
                    UserId = null,
                    OrganizationId = null,
                    FoundUser = false,
                    FoundOrganization = false
                });
            }
            var org = user.OrganizationId.HasValue
                ? await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value)
                : null;

            return Ok(new IdLookupResponse
            {
                UserId = user.Id,
                OrganizationId = org?.Id,
                FoundUser = true,
                FoundOrganization = org != null
            });
        }
    }
}
