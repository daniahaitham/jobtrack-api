namespace JobTrack.Api.Models;

/// <summary>A person who can log in. We never store the real password, only a hash.</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<JobApplication> Applications { get; set; } = new();
}
