using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/auth")] // Development-only token minting
    public class DevAuthController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DevAuthController> _logger;

        public DevAuthController(IWebHostEnvironment env, IConfiguration config, ILogger<DevAuthController> logger)
        {
            _env = env; _config = config; _logger = logger;
        }

        public class DevTokenRequest
        {
            public Guid? OrgId { get; set; }
            public Guid? UserId { get; set; }
            public string? Email { get; set; }
        }

        [HttpPost("dev-token")]
        [AllowAnonymous]
        public IActionResult CreateDevToken([FromBody] DevTokenRequest body)
        {
            if (!_env.IsDevelopment()) return Problem(statusCode: 404, title: "Not Found", detail: "This endpoint is available only in Development environment.");

            var issuer = _config["Jwt:Issuer"]; var audience = _config["Jwt:Audience"]; var key = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(key))
            {
                return Problem(statusCode: 500, title: "Server configuration error", detail: "JWT is not configured (Issuer/Audience/Key)");
            }

            var orgId = body.OrgId ?? Guid.NewGuid();
            var userId = body.UserId ?? Guid.NewGuid();
            var email = body.Email ?? $"user+{userId}@example.local";

            var claims = new List<Claim>
            {
                new Claim("orgId", orgId.ToString()),
                new Claim("scope", "verified"),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("Issued dev token for org {OrgId} user {UserId}", orgId, userId);
            return Ok(new { token = jwt, orgId, userId, email });
        }
    }
}
