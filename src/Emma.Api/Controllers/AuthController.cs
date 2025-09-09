using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace Emma.Api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/_shadow-auth")] // hidden and non-conflicting
    public sealed class AuthController : ControllerBase
    {
        public sealed class DevTokenRequest
        {
            public Guid? OrgId { get; set; }
            public Guid? UserId { get; set; }
            public string? Email { get; set; }
            public int ExpiresInMinutes { get; set; } = 60;
            public string Scope { get; set; } = "verified";
        }

        public sealed class DevTokenResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAtUtc { get; set; }
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
        }

        [HttpPost("shadow-dev-token")]
        public IActionResult CreateDevToken([FromBody] DevTokenRequest req, [FromServices] IConfiguration config, [FromServices] IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
                return Problem(statusCode: 403, title: "Forbidden", detail: "Dev token endpoint is available only in Development.");

            var issuer = config["Jwt:Issuer"];
            var audience = config["Jwt:Audience"];
            var key = config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(key))
                return Problem(statusCode: 500, title: "JWT not configured", detail: "Jwt:Issuer, Jwt:Audience, and Jwt:Key must be set in configuration.");

            var now = DateTimeOffset.UtcNow;
            var expires = now.AddMinutes(Math.Clamp(req?.ExpiresInMinutes ?? 60, 5, 480));

            var claims = new List<Claim>
            {
                new("iss", issuer),
                new("aud", audience),
                new("iat", now.ToUnixTimeSeconds().ToString()),
                new("nbf", now.ToUnixTimeSeconds().ToString()),
                new("exp", expires.ToUnixTimeSeconds().ToString()),
            };

            // Standard claims
            var sub = (req?.UserId ?? Guid.NewGuid()).ToString();
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
            if (!string.IsNullOrWhiteSpace(req?.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, req!.Email!));

            // Org/Tenant scoping claims if provided
            if (req?.OrgId is Guid orgId)
                claims.Add(new Claim("orgId", orgId.ToString()));
            if (Request.Query.ContainsKey("tenantId"))
                claims.Add(new Claim("tenantId", Request.Query["tenantId"]!));

            // Scope: include 'verified' for the VerifiedUser policy fast-path
            var scope = string.IsNullOrWhiteSpace(req?.Scope) ? "verified" : req!.Scope!;
            claims.Add(new Claim("scope", scope));

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(new DevTokenResponse
            {
                Token = token,
                ExpiresAtUtc = expires,
                Issuer = issuer,
                Audience = audience
            });
        }
    }
}
