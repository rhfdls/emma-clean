using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Infrastructure.Data;
using System.Security.Claims;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Authorize(Policy = "VerifiedUser")]
    [Route("api/admin")] // Org-scoped admin helpers (dev minimal)
    public sealed class AdminController : ControllerBase
    {
        private readonly EmmaDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        public AdminController(EmmaDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        { _db = db; _env = env; _cfg = cfg; }

        private Guid? GetOrgIdFromClaims()
        {
            var orgIdStr = User?.FindFirstValue("orgId");
            return Guid.TryParse(orgIdStr, out var g) ? g : null;
        }

        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var users = await _db.Users.AsNoTracking().CountAsync(u => u.OrganizationId == orgId.Value);
            var contacts = await _db.Contacts.AsNoTracking().CountAsync(c => c.OrganizationId == orgId.Value);
            return Ok(new { users, contacts });
        }

        public sealed class UserDto
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string[] Roles { get; set; } = Array.Empty<string>();
        }

        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ListUsers()
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var rows = await _db.Users.AsNoTracking()
                .Where(u => u.OrganizationId == orgId.Value)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserDto { Id = u.Id, Email = u.Email, IsActive = u.IsActive, Roles = Array.Empty<string>() })
                .ToListAsync();
            return Ok(rows);
        }

        [HttpGet("users/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId.Value);
            if (u == null) return Problem(statusCode: 404, title: "User not found", detail: "No such user in org.");
            return Ok(new UserDto { Id = u.Id, Email = u.Email, IsActive = u.IsActive, Roles = Array.Empty<string>() });
        }

        public sealed class CreateUserRequest { public string Email { get; set; } = string.Empty; }

        [HttpPost("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest body)
        {
            if (string.IsNullOrWhiteSpace(body?.Email))
                return Problem(statusCode: 422, title: "Validation failed", detail: "Email is required.");

            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var email = body.Email.Trim();
            var exists = await _db.Users.AnyAsync(u => u.OrganizationId == orgId.Value && u.Email.ToLower() == email.ToLower());
            if (exists)
                return Problem(statusCode: 409, title: "Conflict", detail: "User with this email already exists in the organization.");

            var user = new Emma.Models.Models.User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "x",
                OrganizationId = orgId.Value,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new UserDto { Id = user.Id, Email = user.Email, IsActive = user.IsActive, Roles = Array.Empty<string>() });
        }

        [HttpPost("users/{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == orgId.Value);
            if (user == null)
                return Problem(statusCode: 404, title: "User not found", detail: "No such user in org.");

            user.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { id = user.Id, isActive = user.IsActive });
        }

        [HttpPost("users/{id:guid}/reactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReactivateUser(Guid id)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == orgId.Value);
            if (user == null)
                return Problem(statusCode: 404, title: "User not found", detail: "No such user in org.");

            user.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(new { id = user.Id, isActive = user.IsActive });
        }
    }
}
