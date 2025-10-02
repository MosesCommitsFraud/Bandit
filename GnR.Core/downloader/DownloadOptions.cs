namespace GnR.Core.Download;

/// <summary>
/// Configuration options for YouTube downloads
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// Path to yt-dlp executable. If null, will search in PATH
    /// </summary>
    public string? YtDlpPath { get; set; }

    /// <summary>
    /// Default directory for downloads
    /// </summary>
    public string DefaultOutputDirectory { get; set; } = "";

    /// <summary>
    /// Whether to automatically add downloaded audio to soundboard
    /// </summary>
    public bool AutoAddToSoundboard { get; set; } = true;

    /// <summary>
    /// Audio quality (0-9, 0=best, 9=worst)
    /// </summary>
    public int AudioQuality { get; set; } = 0;

    /// <summary>
    /// Maximum number of concurrent downloads
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;
}

