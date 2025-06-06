using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
// TODO: Add Microsoft.IdentityModel.Tokens package when ready to enable JWT authentication
// using Microsoft.IdentityModel.Tokens;
// using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Emma.Data;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Authentication;

/// <summary>
/// JWT Authentication handler that validates JWT tokens and extracts agent information.
/// Replaces the insecure AllowAllAuthenticationHandler for production use.
/// TODO: Re-enable when Microsoft.IdentityModel.Tokens package is added
/// </summary>
/*
public class JwtAuthenticationHandler : AuthenticationHandler<JwtAuthenticationOptions>
{
    private readonly AppDbContext _context;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        AppDbContext context)
        : base(options, logger, encoder, clock)
    {
        _context = context;
        _logger = logger.CreateLogger<JwtAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Missing Authorization header");
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization header format");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Missing JWT token");
        }

        try
        {
            // Validate and decode JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Options.SecretKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = Options.Issuer,
                ValidateAudience = true,
                ValidAudience = Options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Extract agent ID from claims
            var agentIdClaim = principal.FindFirst("AgentId")?.Value;
            if (string.IsNullOrEmpty(agentIdClaim) || !Guid.TryParse(agentIdClaim, out var agentId))
            {
                _logger.LogWarning("JWT token missing or invalid AgentId claim");
                return AuthenticateResult.Fail("Invalid AgentId in token");
            }

            // Verify agent exists and is active
            var agent = await _context.Agents
                .Include(a => a.Organization)
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (agent == null)
            {
                _logger.LogWarning("Agent {AgentId} not found in database", agentId);
                return AuthenticateResult.Fail("Agent not found");
            }

            if (!agent.IsActive)
            {
                _logger.LogWarning("Agent {AgentId} is not active", agentId);
                return AuthenticateResult.Fail("Agent account is inactive");
            }

            // Create claims for the authenticated agent
            var claims = new List<Claim>
            {
                new Claim("AgentId", agent.Id.ToString()),
                new Claim("OrganizationId", agent.OrganizationId?.ToString() ?? ""),
                new Claim("Email", agent.Email ?? ""),
                new Claim("Name", $"{agent.FirstName} {agent.LastName}".Trim()),
                new Claim(ClaimTypes.NameIdentifier, agent.Id.ToString()),
                new Claim(ClaimTypes.Email, agent.Email ?? ""),
                new Claim(ClaimTypes.Name, $"{agent.FirstName} {agent.LastName}".Trim())
            };

            // Add organization owner claim if applicable
            if (agent.Organization?.OwnerAgentId == agent.Id)
            {
                claims.Add(new Claim("IsOrganizationOwner", "true"));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var authPrincipal = new ClaimsPrincipal(identity);

            _logger.LogInformation("Successfully authenticated agent {AgentId} ({Email})", 
                agent.Id, agent.Email);

            return AuthenticateResult.Success(new AuthenticationTicket(authPrincipal, Scheme.Name));
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired");
            return AuthenticateResult.Fail("Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("JWT token has invalid signature");
            return AuthenticateResult.Fail("Invalid token signature");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return AuthenticateResult.Fail("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }
}

/// <summary>
/// Configuration options for JWT authentication.
/// </summary>
public class JwtAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "JwtBearer";
    
    /// <summary>
    /// Secret key for JWT token validation.
    /// Should be stored securely in configuration.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT token issuer.
    /// </summary>
    public string Issuer { get; set; } = "Emma.Api";
    
    /// <summary>
    /// JWT token audience.
    /// </summary>
    public string Audience { get; set; } = "Emma.Client";
}
*/
