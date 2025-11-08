using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Sonarr;

/// <summary>
/// Represents a series in Sonarr.
/// </summary>
public class SonarrSeries
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID.
    /// </summary>
    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    /// <summary>
    /// Gets or sets the root folder path (used when adding new series).
    /// </summary>
    [JsonPropertyName("rootFolderPath")]
    public string RootFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path where the series is stored (required for updates).
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the series is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use season folders.
    /// </summary>
    [JsonPropertyName("seasonFolder")]
    public bool SeasonFolder { get; set; } = true;

    /// <summary>
    /// Gets or sets the series type (standard, anime, daily).
    /// </summary>
    [JsonPropertyName("seriesType")]
    public string SeriesType { get; set; } = "standard";

    /// <summary>
    /// Gets or sets the seasons.
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<SonarrSeason> Seasons { get; set; } = new List<SonarrSeason>();

    /// <summary>
    /// Gets or sets the add options.
    /// </summary>
    [JsonPropertyName("addOptions")]
    public SonarrAddOptions? AddOptions { get; set; }

    /// <summary>
    /// Gets or sets the language profile ID.
    /// </summary>
    [JsonPropertyName("languageProfileId")]
    public int LanguageProfileId { get; set; } = 1;
}
