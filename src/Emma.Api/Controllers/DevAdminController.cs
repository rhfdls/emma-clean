using System;
using System.Threading;
using System.Threading.Tasks;
using Emma.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/dev")] // Development-only admin helpers
    public sealed class DevAdminController : ControllerBase
    {
        private readonly EmmaDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        public DevAdminController(EmmaDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db; _env = env; _cfg = cfg;
        }

        [HttpGet("orgs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListOrgs(CancellationToken ct, [FromQuery] bool includeInactive = true)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var query = _db.Organizations.AsNoTracking().AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            var data = await (
                from o in query
                join owner in _db.Users.AsNoTracking() on o.OwnerUserId equals owner.Id
                orderby o.CreatedAt descending
                select new OrgOverviewDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = o.Email,
                    PlanId = o.PlanId,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                    OwnerUserId = o.OwnerUserId,
                    OwnerEmail = owner.Email,
                    UserCount = _db.Users.Count(u => u.OrganizationId == o.Id),
                    UsersLink = $"/api/dev/users?orgId={o.Id}",
                    OrgLink = $"/api/organization/{o.Id}"
                }
            ).ToListAsync(ct);

            return Ok(data);
        }

        public sealed class OrgOverviewDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? PlanId { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid OwnerUserId { get; set; }
            public string OwnerEmail { get; set; } = string.Empty;
            public int UserCount { get; set; }
            public string UsersLink { get; set; } = string.Empty;
            public string OrgLink { get; set; } = string.Empty;
        }

        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListUsers([FromQuery] Guid? orgId, [FromQuery] bool isOwnerOnly = false, CancellationToken ct = default)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var query = _db.Users.AsNoTracking().AsQueryable();

            if (orgId.HasValue)
            {
                query = query.Where(u => u.OrganizationId == orgId.Value);
            }

            // Join to organizations to compute IsOwner flag when needed
            if (isOwnerOnly)
            {
                query = from u in query
                        join o in _db.Organizations.AsNoTracking() on u.OrganizationId equals o.Id
                        where o.OwnerUserId == u.Id
                        select u;
            }

            var data = await (
                from u in query
                join o in _db.Organizations.AsNoTracking() on u.OrganizationId equals o.Id into og
                from o in og.DefaultIfEmpty()
                orderby u.CreatedAt descending
                select new DevUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    OrganizationId = u.OrganizationId,
                    IsOwner = o != null && o.OwnerUserId == u.Id,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                }
            ).ToListAsync(ct);

            return Ok(data);
        }

        public sealed class DevUserDto
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public Guid? OrganizationId { get; set; }
            public bool IsOwner { get; set; }
            public bool IsActive { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? LastLoginAt { get; set; }
        }

        [HttpGet("duplicates/users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserDuplicates(CancellationToken ct)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            // Group by (OrganizationId, lower(Email)) and return sets with count > 1
            var dupes = await (
                from u in _db.Users.AsNoTracking()
                group u by new { u.OrganizationId, EmailNorm = u.Email.ToLower() } into g
                where g.Count() > 1
                select new
                {
                    OrganizationId = g.Key.OrganizationId,
                    EmailNormalized = g.Key.EmailNorm,
                    Count = g.Count(),
                    UserIds = g.Select(x => x.Id).ToArray(),
                    LatestUserId = g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Select(x => x.Id).First()
                }
            ).ToListAsync(ct);

            return Ok(dupes);
        }

        [HttpPost("cleanup/duplicates/users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CleanupUserDuplicates([FromQuery] string policy = "keepNewest", CancellationToken ct = default)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            if (!string.Equals(policy, "keepNewest", StringComparison.OrdinalIgnoreCase))
            {
                return Problem(statusCode: 400, title: "Unsupported policy", detail: "Only policy=keepNewest is supported");
            }

            // Find duplicates
            var dupGroups = await (
                from u in _db.Users.AsNoTracking()
                group u by new { u.OrganizationId, EmailNorm = u.Email.ToLower() } into g
                where g.Count() > 1
                select new
                {
                    g.Key.OrganizationId,
                    g.Key.EmailNorm,
                    Winners = g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(1).Select(x => x.Id),
                    Losers = g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(1).Select(x => x.Id)
                }
            ).ToListAsync(ct);

            int deleted = 0;
            foreach (var g in dupGroups)
            {
                var loserIds = g.Losers.ToArray();
                if (loserIds.Length == 0) continue;

                var losers = await _db.Users.Where(u => loserIds.Contains(u.Id)).ToListAsync(ct);
                _db.Users.RemoveRange(losers);
                try
                {
                    deleted += await _db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex)
                {
                    return Problem(statusCode: 409, title: "Delete failed due to constraints", detail: ex.Message);
                }
            }

            return Ok(new { deleted });
        }

        [HttpDelete("user/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return NotFound();

            _db.Users.Remove(user);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                return Problem(statusCode: 409, title: "Delete failed due to constraints", detail: ex.Message);
            }
            return NoContent();
        }

        [HttpDelete("org/{orgId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteOrg(Guid orgId, CancellationToken ct)
        {
            if (!_env.IsDevelopment() || !string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
            if (org is null) return NotFound();

            _db.Organizations.Remove(org);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                return Problem(statusCode: 409, title: "Delete failed due to constraints", detail: ex.Message);
            }
            return NoContent();
        }
    }
}
