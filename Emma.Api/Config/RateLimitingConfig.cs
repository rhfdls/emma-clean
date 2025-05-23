using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Emma.Api.Config
{
    public static class RateLimitingConfig
    {
        public const string PolicyName = "PerIpRateLimit";
        public const int PermitLimit = 10;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        public static void ConfigureRateLimiting(IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = PermitLimit,
                            Window = Window
                        }));
                
                options.OnRejected = (context, _) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return new ValueTask();
                };
            });
        }
    }
}
