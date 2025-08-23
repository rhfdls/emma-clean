using Emma.Api.Models;
using Emma.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>
        /// Register a new organization and user (onboarding).
        /// </summary>
        /// <param name="request">Registration info</param>
        /// <returns>Verification token (stub for local)</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Invalid request body.");

            var tokenOrError = await _onboardingService.RegisterOrganizationAsync(request);
            if (tokenOrError == null)
                return Problem(statusCode: 400, title: "Registration failed", detail: "Duplicate org or email, or invalid plan/seats.");

            return Ok(tokenOrError);
        }
    }
}
