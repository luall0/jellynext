using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents the Trakt device code response for OAuth device flow.
/// </summary>
public class TraktDeviceCode
{
    /// <summary>
    /// Gets or sets the device code used for polling.
    /// </summary>
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user code to display to the user.
    /// </summary>
    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification URL where user enters the code.
    /// </summary>
    [JsonPropertyName("verification_url")]
    public string VerificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of seconds until the device code expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}
