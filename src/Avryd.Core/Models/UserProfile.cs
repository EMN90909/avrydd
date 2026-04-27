using Newtonsoft.Json;

namespace Avryd.Core.Models;

public class UserProfile
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonProperty("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonProperty("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonProperty("is_activated")]
    public bool IsActivated { get; set; }

    [JsonProperty("activation_date")]
    public DateTime? ActivationDate { get; set; }

    [JsonProperty("hardware_id")]
    public string HardwareId { get; set; } = string.Empty;

    [JsonProperty("session_token")]
    public string SessionToken { get; set; } = string.Empty;

    [JsonProperty("token_expiry")]
    public DateTime? TokenExpiry { get; set; }

    [JsonProperty("total_sessions")]
    public int TotalSessions { get; set; }

    [JsonProperty("total_usage_minutes")]
    public double TotalUsageMinutes { get; set; }
}

public class SessionRecord
{
    [JsonProperty("start_time")]
    public DateTime StartTime { get; set; }

    [JsonProperty("end_time")]
    public DateTime? EndTime { get; set; }

    [JsonProperty("duration_minutes")]
    public double DurationMinutes { get; set; }
}

public class ActivationInfo
{
    [JsonProperty("hardware_id")]
    public string HardwareId { get; set; } = string.Empty;

    [JsonProperty("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("session_token")]
    public string SessionToken { get; set; } = string.Empty;

    [JsonProperty("activated_at")]
    public DateTime ActivatedAt { get; set; }

    [JsonProperty("last_seen")]
    public DateTime LastSeen { get; set; }

    [JsonProperty("encrypted_token")]
    public string EncryptedToken { get; set; } = string.Empty;
}
