using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace Emma.Api.Auth
{
    // SPRINT2: Central place to register API authorization policies
    public static class Policies
    {
        public const string OrgOwnerOrAdmin = "OrgOwnerOrAdmin";

        public static IServiceCollection AddEmmaAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(OrgOwnerOrAdmin, policy =>
                {
                    // Minimal policy: require role claim of OrgOwner or OrgAdmin
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim(c => c.Type == "role" && (c.Value == "OrgOwner" || c.Value == "OrgAdmin"))
                    );
                });
            });
            return services;
        }
    }
}
