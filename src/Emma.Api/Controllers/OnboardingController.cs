using Emma.Api.Models;
using Emma.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly EmmaDbContext _db;

        public OnboardingController(IOnboardingService onboardingService, IWebHostEnvironment env, IConfiguration cfg, EmmaDbContext db)
        {
            _onboardingService = onboardingService;
            _env = env;
            _cfg = cfg;
            _db = db;
        }

        /// <summary>
        /// Register a new organization and user (onboarding).
        /// </summary>
        /// <param name="request">Registration info</param>
        /// <returns>Verification token (stub for local)</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Invalid request body.");

            // Development: auto-provision and mint JWT so frontend can proceed to /admin
            var allowDev = string.Equals(_cfg["ALLOW_DEV_AUTOPROVISION"], "true", StringComparison.OrdinalIgnoreCase);
            var issuer = _cfg["Jwt:Issuer"]; var audience = _cfg["Jwt:Audience"]; var key = _cfg["Jwt:Key"];
            if (_env.IsDevelopment() && allowDev && !string.IsNullOrWhiteSpace(issuer) && !string.IsNullOrWhiteSpace(audience) && !string.IsNullOrWhiteSpace(key))
            {
                // Ensure user
                var email = request.Email.Trim();
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (user == null)
                {
                    user = new Emma.Models.Models.User
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        Password = "x",
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }

                // Ensure organization (by name)
                var orgName = request.OrganizationName?.Trim() ?? "Dev Org";
                var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Name.ToLower() == orgName.ToLower());
                if (org == null)
                {
                    org = new Emma.Models.Models.Organization
                    {
                        Id = Guid.NewGuid(),
                        Name = orgName,
                        Email = $"{Guid.NewGuid():N}@example.local",
                        IsActive = true,
                        PlanId = string.IsNullOrWhiteSpace(request.PlanKey) ? "Basic" : request.PlanKey,
                        OwnerUserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Organizations.Add(org);
                    await _db.SaveChangesAsync();
                }

                // Link user -> org
                if (user.OrganizationId != org.Id)
                {
                    user.OrganizationId = org.Id;
                    await _db.SaveChangesAsync();
                }

                user.LastLoginAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                // Mint JWT
                var claims = new List<Claim>
                {
                    new Claim("orgId", org.Id.ToString()),
                    new Claim("scope", "verified"),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email)
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
                return Ok(new { token = jwt });
            }

            // Non-dev: keep original SPRINT1 behavior (returns verification token string)
            var tokenOrError = await _onboardingService.RegisterOrganizationAsync(request);
            if (tokenOrError == null)
                return Problem(statusCode: 400, title: "Registration failed", detail: "Duplicate org or email, or invalid plan/seats.");
            return Ok(new { token = tokenOrError });
        }
    }
}
