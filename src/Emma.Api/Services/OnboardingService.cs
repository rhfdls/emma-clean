using System;
using System.Threading.Tasks;
using Emma.Api.Models;
using Emma.Data;
using Emma.Models.Models; // SPRINT1_ENUM_FIX
using Emma.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Services
{
    // SPRINT1: Onboarding service implementation for registration/profile
    public class OnboardingService : IOnboardingService
    {
        private readonly AppDbContext _db;
        // TODO: Inject plan metadata service if plan validation requires external source
        // TODO: Inject password hasher if using ASP.NET Identity

        public OnboardingService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string?> RegisterOrganizationAsync(RegisterRequest request)
        {
            // 1. Validate unique org name
            var orgExists = await _db.Organizations.AnyAsync(o => o.Name == request.OrganizationName);
            if (orgExists)
                return null;

            // 2. Validate unique email
            var userExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (userExists)
                return null;

            // 3. Validate plan key and seat count (stub: replace with real plan lookup)
            var allowedPlans = new[] { "basic", "pro" };
            if (!allowedPlans.Contains(request.PlanKey.ToLower()))
                return null;
            if (request.SeatCount < 1 || request.SeatCount > 1000)
                return null;

            try
            {
                // 4. Hash password (stub: use real hasher in production)
                var hashedPassword = $"hashed-{request.Password}";

                // 5. Assign org GUID
                var orgGuid = Guid.NewGuid();

                // 6. Create user entity first (no org assigned yet)
                var user = new User
                {
                    Email = request.Email,
                    Password = hashedPassword,
                    AccountStatus = AccountStatus.PendingVerification
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(); // User gets Id

                // 7. Create org entity, referencing user as owner
                var org = new Organization
                {
                    Name = request.OrganizationName,
                    OrgGuid = orgGuid,
                    PlanType = Enum.TryParse<PlanType>(request.PlanKey, true, out var planType) ? planType : PlanType.Basic,
                    SeatCount = request.SeatCount,
                    OwnerUserId = user.Id // This is the fix!
                };
                _db.Organizations.Add(org);
                await _db.SaveChangesAsync(); // Org gets Id

                // 8. Update user to reference org
                user.OrganizationId = org.Id;
                await _db.SaveChangesAsync();

                // 9. Return stubbed email verification token
                return Guid.NewGuid().ToString();
            }
            catch (DbUpdateException ex)
            {
                // Handle unique constraint violation for org email
                if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_Organizations_Email"))
                {
                    // Duplicate org email
                    return null;
                }
                throw;
            }
        }

        public async Task<ProfileResponse?> GetProfileAsync(string email)
        {
            // 1. Lookup user and org (stub: replace with real join)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null;
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId);
            if (org == null)
                return null;

            // 2. Build profile response (stub: fill with real plan metadata)
            var profile = new ProfileResponse
            {
                OrganizationName = org.Name,
                PlanKey = org.PlanType?.ToString() ?? string.Empty, // SPRINT1_ENUM_FIX
                PlanLabel = org.PlanType?.ToString() ?? string.Empty, // SPRINT1_ENUM_FIX
                PlanDescription = "TBD", // TODO: Lookup description
                PlanPrice = 0, // TODO: Lookup price
                SeatCount = org.SeatCount ?? 0, // SPRINT1_ENUM_FIX
                OrgGuid = org.OrgGuid.ToString(), // SPRINT1_ENUM_FIX
                AccountStatus = user.AccountStatus.ToString(), // SPRINT1_ENUM_FIX
                Email = user.Email
            };
            return profile;
        }
    }
}
