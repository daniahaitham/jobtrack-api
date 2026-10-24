namespace JobTrack.Api.Models;

/// <summary>The stages a job application moves through.</summary>
public enum ApplicationStatus
{
    Applied,
    Interview,
    Offer,
    Rejected
}
