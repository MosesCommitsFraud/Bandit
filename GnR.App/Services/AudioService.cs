using NAudio.Wave;

namespace GnR.App.Services;

public class AudioService : IDisposable
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    public void Play(string path)
    {
        Stop();
        _reader = new AudioFileReader(path);
        _output = new WaveOutEvent();
        _output.Init(_reader);
        _output.Play();
        _output.PlaybackStopped += (_, __) => Stop();
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose(); _output = null;
        _reader?.Dispose(); _reader = null;
    }

    public void Dispose() => Stop();
}
