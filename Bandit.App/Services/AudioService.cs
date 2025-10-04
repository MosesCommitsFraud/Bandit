using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Bandit.App.Services;

public class AudioService : IDisposable, INotifyPropertyChanged
{
    private WaveOutEvent? _outputDevice;  // For virtual mic output (to Discord/OBS)
    private WaveOutEvent? _monitorDevice;  // For personal monitoring (your headphones/speakers)
    private AudioFileReader? _outputReader;
    private AudioFileReader? _monitorReader;
    private DispatcherTimer? _positionTimer;
    
    // Microphone passthrough
    private WaveInEvent? _microphoneIn;
    private BufferedWaveProvider? _microphoneBuffer;
    private MixingSampleProvider? _mixer;
    private bool _microphonePassthroughEnabled = false;

    private static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma", ".opus", ".webm" };

    // Playback state
    private string _currentFile = "";
    public string CurrentFile { get => _currentFile; private set { _currentFile = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPlaying)); } }

    private bool _isPlaying = false;
    public bool IsPlaying { get => _isPlaying; private set { _isPlaying = value; OnPropertyChanged(); } }

    private TimeSpan _position = TimeSpan.Zero;
    public TimeSpan Position 
    { 
        get => _position; 
        set 
        { 
            _position = value;
            if (_outputReader != null)
            {
                _outputReader.CurrentTime = value;
            }
            if (_monitorReader != null)
            {
                _monitorReader.CurrentTime = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(PositionSeconds));
        } 
    }

    // Bindable position in seconds for the slider
    public double PositionSeconds
    {
        get => Position.TotalSeconds;
        set => Position = TimeSpan.FromSeconds(value);
    }

    private TimeSpan _duration = TimeSpan.Zero;
    public TimeSpan Duration { get => _duration; private set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationSeconds)); } }

    // Bindable duration in seconds for the slider
    public double DurationSeconds => Duration.TotalSeconds;

    // Volume controls (0.0 to 1.0)
    private float _outputVolume = 1.0f;
    public float OutputVolume 
    { 
        get => _outputVolume; 
        set 
        { 
            _outputVolume = Math.Clamp(value, 0f, 1f);
            if (_outputReader != null) _outputReader.Volume = _outputVolume;
            OnPropertyChanged();
        } 
    }

    private float _monitorVolume = 1.0f;
    public float MonitorVolume 
    { 
        get => _monitorVolume; 
        set 
        { 
            _monitorVolume = Math.Clamp(value, 0f, 1f);
            if (_monitorReader != null) _monitorReader.Volume = _monitorVolume;
            OnPropertyChanged();
        } 
    }

    // Device selection
    private int _outputDeviceIndex = 0;  // Virtual mic output (for Discord/OBS)
    public int OutputDeviceIndex 
    { 
        get => _outputDeviceIndex; 
        set 
        { 
            _outputDeviceIndex = value;
            OnPropertyChanged();
            RestartMicrophonePassthrough();
        } 
    }

    private int _monitorDeviceIndex = 0;  // Personal monitor (your headphones/speakers)
    public int MonitorDeviceIndex 
    { 
        get => _monitorDeviceIndex; 
        set 
        { 
            _monitorDeviceIndex = value;
            OnPropertyChanged();
        } 
    }

    private int _microphoneInputIndex = -1;  // Physical microphone input
    public int MicrophoneInputIndex 
    { 
        get => _microphoneInputIndex; 
        set 
        { 
            _microphoneInputIndex = value;
            OnPropertyChanged();
            RestartMicrophonePassthrough();
        } 
    }

    public bool MicrophonePassthroughEnabled
    {
        get => _microphonePassthroughEnabled;
        set
        {
            _microphonePassthroughEnabled = value;
            OnPropertyChanged();
            if (value)
                StartMicrophonePassthrough();
            else
                StopMicrophonePassthrough();
        }
    }

    public List<string> OutputDevices { get; private set; } = new();
    public List<string> InputDevices { get; private set; } = new();

    public AudioService()
    {
        // Load available output devices
        RefreshDevices();

        // Setup position timer
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _positionTimer.Tick += (s, e) =>
        {
            if (_outputReader != null && IsPlaying)
            {
                // Update without triggering the setter (to avoid seeking)
                _position = _outputReader.CurrentTime;
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(PositionSeconds));
            }
        };
    }

    public void RefreshDevices()
    {
        OutputDevices.Clear();
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            OutputDevices.Add(caps.ProductName);
        }
        OnPropertyChanged(nameof(OutputDevices));

        InputDevices.Clear();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            InputDevices.Add(caps.ProductName);
        }
        OnPropertyChanged(nameof(InputDevices));
    }

    private void StartMicrophonePassthrough()
    {
        if (_microphoneInputIndex < 0 || _microphoneInputIndex >= WaveIn.DeviceCount)
            return;

        try
        {
            StopMicrophonePassthrough();

            // Create microphone input
            _microphoneIn = new WaveInEvent
            {
                DeviceNumber = _microphoneInputIndex,
                WaveFormat = new WaveFormat(44100, 1) // 44.1kHz, mono
            };

            // Create buffer for microphone audio
            _microphoneBuffer = new BufferedWaveProvider(_microphoneIn.WaveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(5),
                DiscardOnBufferOverflow = true
            };

            // Create mixer
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            // Add microphone to mixer (convert mono to stereo)
            var micSampleProvider = _microphoneBuffer.ToSampleProvider();
            var micStereo = new MonoToStereoSampleProvider(micSampleProvider);
            _mixer.AddMixerInput(micStereo);

            // Microphone data available event
            _microphoneIn.DataAvailable += (s, e) =>
            {
                _microphoneBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            // Setup output device for mixed audio
            if (_outputDevice == null || _outputDevice.PlaybackState == PlaybackState.Stopped)
            {
                _outputDevice = new WaveOutEvent { DeviceNumber = _outputDeviceIndex };
                _outputDevice.Init(_mixer);
                _outputDevice.Play();
            }

            // Start recording from microphone
            _microphoneIn.StartRecording();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start microphone passthrough: {ex.Message}");
            StopMicrophonePassthrough();
        }
    }

    private void StopMicrophonePassthrough()
    {
        _microphoneIn?.StopRecording();
        _microphoneIn?.Dispose();
        _microphoneIn = null;

        _microphoneBuffer?.ClearBuffer();
        _microphoneBuffer = null;

        if (_mixer != null)
        {
            _mixer.RemoveAllMixerInputs();
            _mixer = null;
        }
    }

    private void RestartMicrophonePassthrough()
    {
        if (_microphonePassthroughEnabled)
        {
            StopMicrophonePassthrough();
            StartMicrophonePassthrough();
        }
    }

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

            // Create readers for both output and monitor
            _outputReader = new AudioFileReader(path);
            _monitorReader = new AudioFileReader(path);
            
            // Apply volume
            _outputReader.Volume = _outputVolume;
            _monitorReader.Volume = _monitorVolume;

            // Setup virtual mic output device (for Discord/OBS)
            _outputDevice = new WaveOutEvent { DeviceNumber = _outputDeviceIndex };
            _outputDevice.Init(_outputReader);
            _outputDevice.PlaybackStopped += (_, __) => 
            {
                IsPlaying = false;
                _positionTimer?.Stop();
                OnPlaybackStopped?.Invoke(this, EventArgs.Empty);
            };

            // Setup personal monitor device (your headphones) only if different from output
            if (_monitorDeviceIndex != _outputDeviceIndex)
            {
                _monitorDevice = new WaveOutEvent { DeviceNumber = _monitorDeviceIndex };
                _monitorDevice.Init(_monitorReader);
                _monitorDevice.Play();
            }
            else
            {
                // Same device selected, don't play twice
                _monitorReader?.Dispose();
                _monitorReader = null;
            }

            // Start playback on both devices
            _outputDevice.Play();

            CurrentFile = path;
            Duration = _outputReader.TotalTime;
            IsPlaying = true;
            _positionTimer?.Start();
        }
        catch (Exception ex)
        {
            Stop();
            throw new InvalidOperationException($"Failed to play audio: {ex.Message}", ex);
        }
    }

    public void Pause()
    {
        if (_outputDevice?.PlaybackState == PlaybackState.Playing)
        {
            _outputDevice?.Pause();
            _monitorDevice?.Pause();
            IsPlaying = false;
            _positionTimer?.Stop();
        }
    }

    public void Resume()
    {
        if (_outputDevice?.PlaybackState == PlaybackState.Paused)
        {
            _outputDevice?.Play();
            _monitorDevice?.Play();
            IsPlaying = true;
            _positionTimer?.Start();
        }
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Pause();
        }
        else if (_outputDevice?.PlaybackState == PlaybackState.Paused)
        {
            Resume();
        }
        // If stopped and we have a current file, restart it
        else if (!string.IsNullOrEmpty(_currentFile) && File.Exists(_currentFile))
        {
            Play(_currentFile);
        }
    }

    public void Stop()
    {
        _positionTimer?.Stop();
        
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _outputDevice = null;

        _monitorDevice?.Stop();
        _monitorDevice?.Dispose();
        _monitorDevice = null;

        _outputReader?.Dispose();
        _outputReader = null;

        _monitorReader?.Dispose();
        _monitorReader = null;

        CurrentFile = "";
        IsPlaying = false;
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
    }

    public event EventHandler? OnPlaybackStopped;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _positionTimer?.Stop();
        StopMicrophonePassthrough();
        Stop();
    }
}
