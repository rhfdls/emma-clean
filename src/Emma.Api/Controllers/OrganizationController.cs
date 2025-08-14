using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Models.Models;
using Emma.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using Emma.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Emma.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : ControllerBase
    {
        // POST api/organization
        [Authorize(Policy = "VerifiedUser")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrganizationCreateDto dto, [FromServices] EmmaDbContext db)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest("Name is required.");

                if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
                    return BadRequest("Valid Email is required.");

                if (dto.OwnerUserId == Guid.Empty)
                    return BadRequest("OwnerUserId is required.");

                var exists = await db.Organizations.AnyAsync(o => o.Name == dto.Name);
                if (exists)
                    return Conflict("Organization with this name already exists.");

                // Validate owner user exists
                var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.OwnerUserId);
                if (owner == null)
                    return BadRequest("OwnerUserId is invalid.");

                var org = new Organization
                {
                    Id = Guid.NewGuid(),
                    OrgGuid = Guid.NewGuid(),
                    Name = dto.Name,
                    Email = dto.Email,
                    OwnerUserId = dto.OwnerUserId,
                    PlanType = dto.PlanType,
                    PlanId = dto.PlanId ?? dto.PlanType?.ToString(),
                    SeatCount = dto.SeatCount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Organizations.Add(org);
                await db.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = org.Id }, new OrganizationReadDto
                {
                    Id = org.Id,
                    OrgGuid = org.OrgGuid,
                    Name = org.Name,
                    Email = org.Email,
                    PlanId = org.PlanId,
                    PlanType = org.PlanType?.ToString(),
                    SeatCount = org.SeatCount,
                    IsActive = org.IsActive
                });
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null)
                {
                    details += "\nInner: " + ex.InnerException.ToString();
                }
                return StatusCode(500, $"Error creating organization: {details}");
            }
        }

        // GET api/organization
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromServices] EmmaDbContext db, [FromServices] IHttpContextAccessor http, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            try
            {
                page = page < 1 ? 1 : page;
                size = size < 1 ? 1 : (size > 100 ? 100 : size);

                Guid userId;
                var userIdStr = http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.HttpContext?.User?.FindFirstValue("sub");
                if (!Guid.TryParse(userIdStr, out userId))
                    return Forbid();

                var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (me == null) return Forbid();

                var query = db.Organizations.AsQueryable();
                if (!me.IsAdmin)
                {
                    if (me.OrganizationId == null)
                        return Ok(Array.Empty<OrganizationReadDto>());
                    var myOrgId = me.OrganizationId.Value;
                    query = query.Where(o => o.Id == myOrgId);
                }

                var list = await query
                    .OrderBy(o => o.Name)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(o => new OrganizationReadDto
                    {
                        Id = o.Id,
                        OrgGuid = o.OrgGuid,
                        Name = o.Name,
                        Email = o.Email,
                        PlanId = o.PlanId,
                        PlanType = o.PlanType.HasValue ? o.PlanType.Value.ToString() : null,
                        SeatCount = o.SeatCount,
                        IsActive = o.IsActive
                    })
                    .ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null)
                {
                    details += "\nInner: " + ex.InnerException.ToString();
                }
                return StatusCode(500, $"Error listing organizations: {details}");
            }
        }

        // GET api/organization/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id, [FromServices] EmmaDbContext db, [FromServices] IHttpContextAccessor http)
        {
            try
            {
                Guid userId;
                var userIdStr = http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.HttpContext?.User?.FindFirstValue("sub");
                if (!Guid.TryParse(userIdStr, out userId))
                    return Forbid();

                var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (me == null) return Forbid();

                var o = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id);
                if (o == null) return NotFound();
                if (!me.IsAdmin && me.OrganizationId != o.Id)
                    return Forbid();
                return Ok(new OrganizationReadDto
                {
                    Id = o.Id,
                    OrgGuid = o.OrgGuid,
                    Name = o.Name,
                    Email = o.Email,
                    PlanId = o.PlanId,
                    PlanType = o.PlanType?.ToString(),
                    SeatCount = o.SeatCount,
                    IsActive = o.IsActive
                });
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null)
                {
                    details += "\nInner: " + ex.InnerException.ToString();
                }
                return StatusCode(500, $"Error getting organization: {details}");
            }
        }

        // moved to Dtos/OrganizationReadDto.cs

        // ===== Invitations =====

        // POST api/organization/{orgId}/invitations
        [HttpPost("{orgId}/invitations")]
        [Authorize(Policy = "VerifiedUser")]
        [Authorize(Policy = Emma.Api.Auth.Policies.OrgOwnerOrAdmin)]
        public async Task<IActionResult> CreateInvitation(Guid orgId, [FromBody] CreateInvitationDto dto, [FromServices] EmmaDbContext db)
        {
            try
            {
                var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
                if (org == null) return NotFound($"Organization {orgId} not found");

                if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
                    return BadRequest("Valid email is required");

                // RBAC: inviter must be org owner or admin
                if (dto.InvitedByUserId.HasValue)
                {
                    var inviter = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.InvitedByUserId.Value);
                    if (inviter == null) return BadRequest("InvitedByUserId is invalid");
                    if (inviter.OrganizationId != orgId) return Forbid();
                    var isOwner = org.OwnerUserId == inviter.Id;
                    var isAdmin = inviter.IsAdmin || (inviter.Role != null && inviter.Role == "Admin");
                    if (!isOwner && !isAdmin) return Forbid();
                }

                // Optional: prevent multiple active invites for same email/org
                var existingActive = await db.OrganizationInvitations.AnyAsync(i => i.OrganizationId == orgId && i.Email == dto.Email && i.AcceptedAt == null && i.RevokedAt == null && (i.ExpiresAt == null || i.ExpiresAt > DateTime.UtcNow));
                if (existingActive)
                    return Conflict("An active invitation already exists for this email");

                var token = Guid.NewGuid().ToString("N");
                var invitation = new OrganizationInvitation
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    Email = dto.Email.Trim(),
                    Role = string.IsNullOrWhiteSpace(dto.Role) ? "Member" : dto.Role,
                    Token = token,
                    ExpiresAt = dto.ExpiresInDays.HasValue ? DateTime.UtcNow.AddDays(dto.ExpiresInDays.Value) : DateTime.UtcNow.AddDays(7),
                    InvitedByUserId = dto.InvitedByUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.OrganizationInvitations.Add(invitation);
                await db.SaveChangesAsync();

                // TODO: send email with link to frontend, e.g. https://app.example.com/join?token={token}

                var read = new InvitationReadDto
                {
                    Id = invitation.Id,
                    OrganizationId = invitation.OrganizationId,
                    Email = invitation.Email,
                    Role = invitation.Role,
                    Token = invitation.Token,
                    ExpiresAt = invitation.ExpiresAt,
                    AcceptedAt = invitation.AcceptedAt,
                    RevokedAt = invitation.RevokedAt
                };

                return CreatedAtAction(nameof(GetInvitationByToken), new { token = token }, read);
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null) details += "\nInner: " + ex.InnerException;
                return StatusCode(500, $"Error creating invitation: {details}");
            }
        }

        // GET api/organization/invitations/{token}
        [HttpGet("invitations/{token}")]
        public async Task<IActionResult> GetInvitationByToken(string token, [FromServices] EmmaDbContext db)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required");
                var invitation = await db.OrganizationInvitations.FirstOrDefaultAsync(i => i.Token == token);
                if (invitation == null) return NotFound();

                var active = invitation.RevokedAt == null && invitation.AcceptedAt == null && (!invitation.ExpiresAt.HasValue || invitation.ExpiresAt > DateTime.UtcNow);
                var read = new InvitationReadDto
                {
                    Id = invitation.Id,
                    OrganizationId = invitation.OrganizationId,
                    Email = invitation.Email,
                    Role = invitation.Role,
                    Token = invitation.Token,
                    ExpiresAt = invitation.ExpiresAt,
                    AcceptedAt = invitation.AcceptedAt,
                    RevokedAt = invitation.RevokedAt,
                    IsActive = active
                };
                return Ok(read);
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null) details += "\nInner: " + ex.InnerException;
                return StatusCode(500, $"Error retrieving invitation: {details}");
            }
        }

        // POST api/organization/invitations/{token}/accept
        [HttpPost("invitations/{token}/accept")]
        public async Task<IActionResult> AcceptInvitation(string token, [FromServices] EmmaDbContext db)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required");
                var invitation = await db.OrganizationInvitations.FirstOrDefaultAsync(i => i.Token == token);
                if (invitation == null) return NotFound();

                if (invitation.RevokedAt != null) return Conflict("Invitation has been revoked");
                if (invitation.AcceptedAt != null) return Conflict("Invitation already accepted");
                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt <= DateTime.UtcNow) return Conflict("Invitation has expired");

                invitation.AcceptedAt = DateTime.UtcNow;
                invitation.UpdatedAt = DateTime.UtcNow;
                db.OrganizationInvitations.Update(invitation);
                await db.SaveChangesAsync();

                // TODO: Link or create User and assign OrganizationId/Role here.

                return NoContent();
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null) details += "\nInner: " + ex.InnerException;
                return StatusCode(500, $"Error accepting invitation: {details}");
            }
        }

        // POST api/organization/invitations/{token}/register
        [HttpPost("invitations/{token}/register")]
        public async Task<IActionResult> RegisterFromInvitation(string token, [FromBody] RegisterFromInvitationDto dto, [FromServices] EmmaDbContext db, [FromServices] IEmailSender emailSender, [FromServices] IConfiguration config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required");
                var invitation = await db.OrganizationInvitations.FirstOrDefaultAsync(i => i.Token == token);
                if (invitation == null) return NotFound();
                if (invitation.RevokedAt != null) return Conflict("Invitation has been revoked");
                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt <= DateTime.UtcNow) return Conflict("Invitation has expired");

                // Normalize email
                var email = invitation.Email.Trim();

                // Try find existing user by email (case-insensitive)
                var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (existing != null)
                {
                    // Enforce single-tenant membership (policy: single org per user)
                    if (existing.OrganizationId.HasValue && existing.OrganizationId.Value != invitation.OrganizationId)
                        return Conflict("User already belongs to a different organization");

                    existing.OrganizationId = invitation.OrganizationId;
                    if (string.IsNullOrWhiteSpace(existing.Role)) existing.Role = invitation.Role;
                    existing.UpdateTimestamp();
                }
                else
                {
                    // Create a new user with PendingVerification; TODO: hash password in production
                    var names = (dto.FullName ?? string.Empty).Trim();
                    var first = string.IsNullOrWhiteSpace(dto.FirstName) ? names.Split(' ').FirstOrDefault() ?? "" : dto.FirstName.Trim();
                    var last = string.IsNullOrWhiteSpace(dto.LastName) ? (names.Contains(' ') ? names.Substring(names.IndexOf(' ') + 1) : "") : dto.LastName.Trim();
                    if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
                        return BadRequest("FirstName and LastName are required");

                    var verifyToken = Guid.NewGuid().ToString("N");
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = first,
                        LastName = last,
                        Email = email,
                        Password = dto.Password ?? Guid.NewGuid().ToString("N"),
                        OrganizationId = invitation.OrganizationId,
                        Role = string.IsNullOrWhiteSpace(invitation.Role) ? "Member" : invitation.Role,
                        AccountStatus = AccountStatus.PendingVerification,
                        VerificationToken = verifyToken,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.Users.Add(user);

                    // Send verification email (dev stub)
                    var baseUrl = config["VerifyUrlBase"] ?? Environment.GetEnvironmentVariable("VERIFY_URL_BASE") ?? "http://localhost:3000/onboarding/verify";
                    var verifyUrl = $"{baseUrl}?token={Uri.EscapeDataString(verifyToken)}";
                    await emailSender.SendVerificationAsync(email, verifyUrl);
                }

                // Mark invitation accepted if not already
                if (invitation.AcceptedAt == null)
                {
                    invitation.AcceptedAt = DateTime.UtcNow;
                    invitation.UpdatedAt = DateTime.UtcNow;
                    db.OrganizationInvitations.Update(invitation);
                }

                await db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var details = ex.ToString();
                if (ex.InnerException != null) details += "\nInner: " + ex.InnerException;
                return StatusCode(500, $"Error registering from invitation: {details}");
            }
        }

        public class CreateInvitationDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
            public string? Role { get; set; }
            public int? ExpiresInDays { get; set; }
            public Guid? InvitedByUserId { get; set; }
        }

        public class InvitationReadDto
        {
            public Guid Id { get; set; }
            public Guid OrganizationId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "Member";
            public string Token { get; set; } = string.Empty;
            public DateTime? ExpiresAt { get; set; }
            public DateTime? AcceptedAt { get; set; }
            public DateTime? RevokedAt { get; set; }
            public bool IsActive { get; set; }
        }

        public class RegisterFromInvitationDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? FullName { get; set; }
            public string? Password { get; set; }
        }
    }
}
