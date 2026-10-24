using System.ComponentModel.DataAnnotations;
using JobTrack.Api.Models;

namespace JobTrack.Api.Dtos;

/// <summary>JSON the client sends to create or update an application.</summary>
public class ApplicationRequest
{
    [Required, MaxLength(200)]
    public string Company { get; set; } = "";

    [Required, MaxLength(200)]
    public string Role { get; set; } = "";

    [Url, MaxLength(500)]
    public string? Link { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    public DateTime? AppliedOn { get; set; }

    public DateTime? FollowUpOn { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

/// <summary>JSON the API sends back.</summary>
public record ApplicationResponse(
    int Id,
    string Company,
    string Role,
    string? Link,
    ApplicationStatus Status,
    DateTime AppliedOn,
    DateTime? FollowUpOn,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
