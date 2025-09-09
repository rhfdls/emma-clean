using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Emma.Api.Contracts.Auth;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/auth")] // Development-only token minting
    public class DevAuthController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DevAuthController> _logger;
        private readonly EmmaDbContext _db;

        public DevAuthController(IWebHostEnvironment env, IConfiguration config, ILogger<DevAuthController> logger, EmmaDbContext db)
        {
            _env = env; _config = config; _logger = logger; _db = db;
        }

        [HttpPost("dev-token")]
        public async Task<IActionResult> CreateDevToken([FromBody] DevTokenRequest body)
        {
            if (!_env.IsDevelopment() || !string.Equals(_config["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var issuer = _config["Jwt:Issuer"]; var audience = _config["Jwt:Audience"]; var key = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(key))
            {
                return Problem(statusCode: 500, title: "Server configuration error", detail: "JWT is not configured (Issuer/Audience/Key)");
            }

            var orgId = body.OrgId.GetValueOrDefault(Guid.NewGuid());
            var userId = body.UserId.GetValueOrDefault(Guid.NewGuid());
            var email = string.IsNullOrWhiteSpace(body.Email) ? $"user+{userId}@example.local" : body.Email.Trim();
            var emailNorm = email.ToLowerInvariant();

            // Reuse existing entities to avoid unique constraint violations
            // Prefer existing user by Id or Email
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId || EF.Functions.ILike(u.Email, emailNorm));
            if (existingUser != null)
            {
                userId = existingUser.Id;
                email = existingUser.Email; // prefer stored casing
                emailNorm = email.ToLowerInvariant();
            }

            // Prefer existing organization by Id or dev email
            const string devOrgEmail = "dev-org@example.local";
            var existingOrg = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId || o.Email == devOrgEmail);
            if (existingOrg != null)
            {
                orgId = existingOrg.Id;
            }

            // Dev-only auto-provision: ensure User then Organization (OwnerUserId required)
            // 1) User: ensure exists
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                if (!body.AutoProvision)
                    return Problem(statusCode: 404, title: "User not found", detail: "Set autoProvision=true or provide an existing userId");

                user = new User
                {
                    Id = userId,
                    FirstName = "Dev",
                    LastName = "User",
                    Email = email,
                    Password = "x", // placeholder; not used in dev token flow
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    OrganizationId = null
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(); // ensure user.Id is persisted
            }

            // 2) Organization: ensure exists, set OwnerUserId to user
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org == null)
            {
                if (!body.AutoProvision)
                    return Problem(statusCode: 404, title: "Organization not found", detail: "Set autoProvision=true or provide an existing orgId");

                org = new Organization
                {
                    Id = orgId,
                    Name = "Dev Org",
                    Email = devOrgEmail,
                    IsActive = true,
                    PlanId = "Basic",
                    OwnerUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Organizations.Add(org);
                await _db.SaveChangesAsync();
            }

            // 3) Link user to org if not already
            if (user.OrganizationId != org.Id)
            {
                user.OrganizationId = org.Id;
                await _db.SaveChangesAsync();
            }

            // 4) Update LastLoginAt on success path
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim("orgId", orgId.ToString()),
                new Claim("scope", "verified"),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(2);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("Issued dev token for org {OrgId} user {UserId}", orgId, userId);
            var resp = new DevTokenResponse
            {
                Token = jwt,
                ExpiresAtUtc = new DateTimeOffset(expires, TimeSpan.Zero),
                UserId = userId,
                OrgId = orgId,
                Email = email
            };
            return Ok(resp);
        }
    }
}
