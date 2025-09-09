using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Emma.Api.Health
{
    public sealed class DbMigrationsHealthCheck : IHealthCheck
    {
        private readonly EmmaDbContext _db;
        public DbMigrationsHealthCheck(EmmaDbContext db) => _db = db;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var pending = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                var message = $"Pending migrations: {string.Join(", ", pending)}";
                return HealthCheckResult.Unhealthy(message);
            }
            return HealthCheckResult.Healthy("DB schema is up-to-date.");
        }
    }
}
