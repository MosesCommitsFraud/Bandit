using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Bandit.Core.Download;

namespace Bandit.App.Services;

/// <summary>
/// Downloads YouTube videos/audio using yt-dlp.
/// Requires yt-dlp.exe to be in PATH or in the application directory.
/// Download from: https://github.com/yt-dlp/yt-dlp/releases
/// </summary>
public class YouTubeDownloadService : IDownloadService
{
    private readonly string _ytDlpPath;
    private readonly string? _ffmpegPath;

    public YouTubeDownloadService()
    {
        // Try to find yt-dlp.exe in the app directory first, then fall back to PATH
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var localPath = Path.Combine(appDir, "yt-dlp.exe");
        _ytDlpPath = File.Exists(localPath) ? localPath : "yt-dlp";
        
        // Check if ffmpeg is in the app directory
        var ffmpegLocal = Path.Combine(appDir, "ffmpeg.exe");
        if (File.Exists(ffmpegLocal))
        {
            _ffmpegPath = Path.GetDirectoryName(ffmpegLocal);
        }
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request, 
        IProgress<string>? log = null, 
        CancellationToken ct = default)
    {
        try
        {
            // Validate URL
            if (!IsValidYouTubeUrl(request.SourceUrl))
            {
                return new DownloadResult(false, null, "Invalid YouTube URL");
            }

            // Ensure output directory exists
            Directory.CreateDirectory(request.OutputDirectory);

            // Build yt-dlp arguments
            var args = BuildArguments(request);
            log?.Report($"Starting download: {request.SourceUrl}");
            log?.Report($"Arguments: {args}");

            // Start yt-dlp process
            var psi = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = request.OutputDirectory
            };

            using var process = new Process { StartInfo = psi };
            
            string? outputPath = null;
            var errorOutput = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    log?.Report(e.Data);
                    
                    // Try to extract the output filename from yt-dlp output
                    if (e.Data.Contains("[download] Destination:") || e.Data.Contains("[ExtractAudio] Destination:"))
                    {
                        var match = Regex.Match(e.Data, @"Destination:\s*(.+)$");
                        if (match.Success)
                        {
                            outputPath = match.Groups[1].Value.Trim();
                        }
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorOutput.Add(e.Data);
                    log?.Report($"[Error] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                // If we didn't capture the exact path, try to find the newest file
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = FindNewestFile(request.OutputDirectory, request.Kind);
                }

                log?.Report("Download completed successfully!");
                return new DownloadResult(true, outputPath, null);
            }
            else
            {
                var error = string.Join(Environment.NewLine, errorOutput);
                return new DownloadResult(false, null, $"yt-dlp exited with code {process.ExitCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            log?.Report($"Exception: {ex.Message}");
            return new DownloadResult(false, null, ex.Message);
        }
    }

    private string BuildArguments(DownloadRequest request)
    {
        var args = new List<string>();

        // Add ffmpeg location if we have it
        if (!string.IsNullOrEmpty(_ffmpegPath))
        {
            args.Add($"--ffmpeg-location \"{_ffmpegPath}\"");
        }

        if (request.Kind == DownloadKind.Audio)
        {
            // Audio download - extract audio
            args.Add("-x"); // Extract audio
            
            // Try to convert to mp3 if ffmpeg is available, otherwise use best available format
            if (!string.IsNullOrEmpty(_ffmpegPath))
            {
                args.Add("--audio-format mp3");
            }
            else
            {
                // No ffmpeg - accept any format that yt-dlp can extract
                args.Add("--audio-format best");
            }
            
            args.Add("--audio-quality 0"); // Best quality
            args.Add("-o \"%(title)s.%(ext)s\""); // Output template
        }
        else
        {
            // Video download
            args.Add("-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\"");
            args.Add("-o \"%(title)s.%(ext)s\"");
        }

        // Add URL
        args.Add($"\"{request.SourceUrl}\"");

        // Progress reporting
        args.Add("--newline"); // Force newlines for easier parsing
        args.Add("--no-playlist"); // Download single video, not playlist

        return string.Join(" ", args);
    }

    private bool IsValidYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Basic YouTube URL validation
        return url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("youtube.com/shorts/", StringComparison.OrdinalIgnoreCase);
    }

    private string? FindNewestFile(string directory, DownloadKind kind)
    {
        var extensions = kind == DownloadKind.Audio 
            ? new[] { "*.mp3", "*.m4a", "*.opus", "*.wav", "*.webm", "*.ogg", "*.aac" }
            : new[] { "*.mp4", "*.webm", "*.mkv" };

        var files = extensions
            .SelectMany(ext => Directory.GetFiles(directory, ext))
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        return files?.FullName;
    }
}

