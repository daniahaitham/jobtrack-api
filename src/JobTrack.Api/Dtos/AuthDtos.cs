using System.ComponentModel.DataAnnotations;

namespace JobTrack.Api.Dtos;

// DTO = "data transfer object": the exact shape of JSON going in or out of the API.
// We use them so we never expose the database classes (like the password hash) directly.

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record AuthResponse(string Token, DateTime ExpiresAt, string Email);

public record MeResponse(int Id, string Email);
