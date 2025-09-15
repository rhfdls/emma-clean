using Emma.Api.Models;
using Emma.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Infrastructure.Data;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public AccountController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>
        /// Get profile for the current user (admin dashboard).
        /// Returns { email, organizationId, organizationName?, roles? }.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetProfile()
        {
            // Prefer claims for org/user; fall back to stub if missing (dev)
            var email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name ?? string.Empty;
            var orgIdStr = User?.FindFirstValue("orgId");
            Guid? orgId = Guid.TryParse(orgIdStr, out var g) ? g : (Guid?)null;
            var userIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : (Guid?)null;

            string? orgName = null;
            bool isOrgOwner = false;
            if (orgId.HasValue)
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();
                var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);
                orgName = org?.Name;
                if (org != null && userId.HasValue)
                {
                    isOrgOwner = org.OwnerUserId == userId.Value;
                }
            }

            // Development-only fallback: if unauthenticated (no email/org), return the most recent org/user
            if (string.IsNullOrWhiteSpace(email) && !orgId.HasValue)
            {
                var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (env.IsDevelopment())
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();
                    var lastUser = await db.Users.AsNoTracking()
                        .OrderByDescending(u => u.LastLoginAt ?? DateTimeOffset.MinValue)
                        .ThenByDescending(u => u.CreatedAt)
                        .FirstOrDefaultAsync();
                    if (lastUser != null)
                    {
                        email = lastUser.Email ?? string.Empty;
                        orgId = lastUser.OrganizationId;
                        if (orgId.HasValue)
                        {
                            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);
                            orgName = org?.Name;
                            isOrgOwner = org != null && org.OwnerUserId == lastUser.Id;
                        }
                    }
                }
            }

            var roles = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray() ?? Array.Empty<string>();

            return Ok(new { email, organizationId = orgId, organizationName = orgName, roles, isOrgOwner });
        }
        // SPRINT1: Email verification endpoint
        [HttpPost("verify")]
        [AllowAnonymous]
        [SwaggerOperation(OperationId = "verifyEmail", Summary = "Verify email by token", Tags = new[] { "Auth" })]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyRequest request, [FromServices] EmmaDbContext db)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return Problem(statusCode: 400, title: "Validation failed", detail: "Missing verification token.");

            // Find user by verification token
            var user = await db.Users.FirstOrDefaultAsync(u => u.VerificationToken == request.Token);
            if (user == null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Invalid or expired token.");

            if (user.AccountStatus == Emma.Models.Models.AccountStatus.Active)
                return Problem(statusCode: 409, title: "Conflict", detail: "Account already verified.");

            user.AccountStatus = Emma.Models.Models.AccountStatus.Active;
            user.IsVerified = true;
            user.VerificationToken = null;
            await db.SaveChangesAsync();

            return Ok("Account verified.");
        }

        // SPRINT1: Helper for enforcing AccountStatus == Active
        private async Task<bool> IsUserActive(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var user = await _onboardingService.GetProfileAsync(email);
            return user != null && user.AccountStatus == "Active";
        }

        // SPRINT1: Example protected endpoint
        [HttpGet("protected-resource")]
        public async Task<IActionResult> GetProtectedResource()
        {
            var email = User.Identity?.Name;
            if (!await IsUserActive(email))
                return Problem(statusCode: 403, title: "Forbidden", detail: "Account not verified. Please complete email verification.");
            // ... actual resource logic ...
            return Ok(new { message = "You are verified and can access protected resources." });
        }
    }

    // SPRINT1: DTO for verification request
    public class VerifyRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
