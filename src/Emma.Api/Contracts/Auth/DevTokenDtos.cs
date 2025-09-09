using System;

namespace Emma.Api.Contracts.Auth
{
    public sealed class DevTokenRequest
    {
        public Guid? UserId { get; set; }
        public Guid? OrgId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool AutoProvision { get; set; } = true;
    }

    public sealed class DevTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public Guid OrgId { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
