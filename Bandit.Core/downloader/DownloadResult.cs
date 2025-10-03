namespace Bandit.Core.Download;

public sealed record DownloadResult(bool Success, string? OutputPath, string? Error);
