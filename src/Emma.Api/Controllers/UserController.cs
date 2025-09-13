using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Infrastructure.Data;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "VerifiedUser")]
    [Authorize(Policy = Emma.Api.Auth.Policies.OrgOwnerOrAdmin)]
    public class UserController : ControllerBase
    {
        private Guid? GetOrgIdFromClaims()
        {
            var orgIdStr = User?.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdStr) || !Guid.TryParse(orgIdStr, out var orgId))
                return null;
            return orgId;
        }

        /// <summary>List users in the caller's organization (admin only).</summary>
        /// <response code="200">Users in org.</response>
        /// <response code="400">Missing org context.</response>
        /// <response code="403">Forbidden.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers([FromServices] EmmaDbContext db, [FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            page = page < 1 ? 1 : page;
            size = size < 1 ? 1 : (size > 200 ? 200 : size);

            var users = await db.Users.AsNoTracking()
                .Where(u => u.OrganizationId == orgId.Value)
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.IsAdmin,
                    u.IsVerified,
                    u.AccountStatus,
                    u.OrganizationId
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>Get a single user in the caller's organization (admin only).</summary>
        /// <response code="200">User found.</response>
        /// <response code="404">Not found in org.</response>
        /// <response code="400">Missing org context.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUser(Guid id, [FromServices] EmmaDbContext db)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null || u.OrganizationId != orgId.Value)
                return Problem(statusCode: 404, title: "Not Found", detail: $"User {id} not found in your org.");

            return Ok(new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.IsAdmin,
                u.IsVerified,
                u.AccountStatus,
                u.OrganizationId
            });
        }

        public class UpdateUserDto
        {
            /// <summary>Optional role (e.g., Admin, Member).</summary>
            public string? Role { get; set; }
            /// <summary>Optional account active flag.</summary>
            public bool? IsActive { get; set; }
            /// <summary>Optional admin flag.</summary>
            public bool? IsAdmin { get; set; }
        }

        /// <summary>Update role/status/admin flags for a user (admin only).</summary>
        /// <response code="204">Updated.</response>
        /// <response code="404">Not found in org.</response>
        /// <response code="400">Missing org context.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, [FromServices] EmmaDbContext db)
        {
            if (dto is null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Request body is required.");

            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null || u.OrganizationId != orgId.Value)
                return Problem(statusCode: 404, title: "Not Found", detail: $"User {id} not found in your org.");

            if (dto.Role != null) u.Role = dto.Role;
            if (dto.IsActive.HasValue) u.IsActive = dto.IsActive.Value;
            if (dto.IsAdmin.HasValue) u.IsAdmin = dto.IsAdmin.Value;

            u.UpdateTimestamp();
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
