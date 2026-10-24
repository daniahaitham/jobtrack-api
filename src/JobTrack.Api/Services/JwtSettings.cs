namespace JobTrack.Api.Services;

/// <summary>Reads the "Jwt" section of appsettings.json.</summary>
public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "JobTrack";
    public string Audience { get; set; } = "JobTrack";
    public int ExpiryMinutes { get; set; } = 120;
}
