using Emma.Api.Models;
using Emma.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Infrastructure.Data;

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
        /// Get profile for the current user (dashboard rehydration).
        /// </summary>
        [HttpGet("profile")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProfileResponse), 200)]
        public async Task<IActionResult> GetProfile()
        {
            // Assume user identity is available via JWT claims
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                // SPRINT1: Return stub profile for unauthenticated users
                var stubProfile = new ProfileResponse
                {
                    OrganizationName = "Stub Org",
                    PlanKey = "stub",
                    PlanLabel = "Stub Plan",
                    PlanDescription = "Stub plan for Sprint 1 frontend dev",
                    PlanPrice = 0,
                    SeatCount = 1,
                    OrgGuid = "00000000-0000-0000-0000-000000000000",
                    AccountStatus = "PendingVerification",
                    Email = "stub@example.com"
                };
                return Ok(stubProfile);
            }

            var profile = await _onboardingService.GetProfileAsync(email);
            if (profile == null)
                return Problem(statusCode: 404, title: "Profile not found", detail: "No profile exists for the current user.");

            return Ok(profile);
        }
        // SPRINT1: Email verification endpoint
        [HttpPost("verify")]
        [AllowAnonymous]
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
