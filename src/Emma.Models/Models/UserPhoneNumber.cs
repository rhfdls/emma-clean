namespace Emma.Models.Models;

public class UserPhoneNumber
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Label { get; set; } // e.g., "Mobile", "Work", "Home"
    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}
