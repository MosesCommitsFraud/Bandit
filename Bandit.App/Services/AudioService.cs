using NAudio.Wave;
using System;
using System.IO;
using System.Linq;

namespace Bandit.App.Services;

public class AudioService : IDisposable
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    private static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma" };

    public bool IsAudioFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return SupportedExtensions.Contains(ext);
    }

    public void Play(string path)
    {
        try
        {
            // Validate file exists
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Audio file not found: {path}");
            }

            // Validate file is an audio format
            if (!IsAudioFile(path))
            {
                var ext = Path.GetExtension(path);
                throw new InvalidOperationException($"Cannot play {ext} files. Only audio files are supported. Videos cannot be played through the soundboard.");
            }

            Stop();
            _reader = new AudioFileReader(path);
            _output = new WaveOutEvent();
            _output.Init(_reader);
            _output.Play();
            _output.PlaybackStopped += (_, __) => Stop();
        }
        catch (Exception ex)
        {
            Stop();
            throw new InvalidOperationException($"Failed to play audio: {ex.Message}", ex);
        }
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose(); _output = null;
        _reader?.Dispose(); _reader = null;
    }

    public void Dispose() => Stop();
}
