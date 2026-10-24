namespace JobTrack.Api.Models;

/// <summary>One job the user applied to. Every application belongs to exactly one user.</summary>
public class JobApplication
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Link { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    public DateTime? FollowUpOn { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
