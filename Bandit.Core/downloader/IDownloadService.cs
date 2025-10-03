namespace Bandit.Core.Download;

public interface IDownloadService
{
    Task<DownloadResult> DownloadAsync(DownloadRequest request, IProgress<string>? log = null, CancellationToken ct = default);
}
