namespace Bandit.Core.Download;

public sealed record DownloadRequest(
    string SourceUrl,
    string OutputDirectory,
    DownloadKind Kind,           // Audio or Video
    string? TargetFormat = null  // e.g. "mp3" / "mp4" if you want
);

public enum DownloadKind { Audio, Video }
