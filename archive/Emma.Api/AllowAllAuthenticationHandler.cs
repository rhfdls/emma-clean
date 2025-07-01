// MVP/DEV-ONLY: Allows all requests through as 'DevUser'. DO NOT USE IN PRODUCTION!
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

public class AllowAllAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AllowAllAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock systemClock)
        : base(options, logger, encoder, systemClock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "DevUser") };
        var identity = new ClaimsIdentity(claims, "AllowAll");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "AllowAll");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
