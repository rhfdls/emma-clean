using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Models
{
    // SPRINT1: DTO for onboarding registration
    public class RegisterRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string PlanKey { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000, ErrorMessage = "Seat count must be between 1 and 1000.")]
        public int SeatCount { get; set; }
    }
}
