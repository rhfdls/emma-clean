using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Infrastructure.Data;
using Emma.Models.Models;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/auth")] // SPRINT2: verification endpoints
    public class VerificationController : ControllerBase
    {
        public class VerifyEmailDto
        {
            public string Token { get; set; } = string.Empty;
        }

        // POST /api/auth/verify-email
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto, [FromServices] EmmaDbContext db)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Token is required.");
            try
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.VerificationToken == dto.Token);
                if (user == null)
                    return NotFound("Invalid or expired token.");

                // Idempotent: if already verified, return NoContent
                if (user.IsVerified && user.AccountStatus == AccountStatus.Active)
                    return NoContent();

                user.IsVerified = true;
                user.AccountStatus = AccountStatus.Active;
                user.VerificationToken = null;
                user.UpdatedAt = DateTime.UtcNow;

                db.Users.Update(user);
                await db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null) details += "\nInner: " + ex.InnerException;
                return StatusCode(500, $"Error verifying email: {details}");
            }
        }
    }
}
