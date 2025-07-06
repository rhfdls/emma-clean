using Emma.Api.Models;
using Emma.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                return NotFound();

            return Ok(profile);
        }
    }
}
