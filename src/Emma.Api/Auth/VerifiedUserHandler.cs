using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Emma.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Auth
{
    public sealed class VerifiedUserHandler : AuthorizationHandler<VerifiedUserRequirement>
    {
        private readonly EmmaDbContext _db;
        private readonly IHttpContextAccessor _http;

        public VerifiedUserHandler(EmmaDbContext db, IHttpContextAccessor http)
        {
            _db = db; _http = http;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedUserRequirement requirement)
        {
            // Try common claim types for user id
            var userIdStr = context.User.FindFirstValue("sub")
                          ?? context.User.FindFirstValue("user_id")
                          ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdStr, out var userId))
                return;

            var verified = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.VerifiedAt != null)
                .FirstOrDefaultAsync();

            if (verified)
                context.Succeed(requirement);
        }
    }
}
