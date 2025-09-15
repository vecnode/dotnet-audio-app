using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace App.Audio;

// Professional audio input service providing real-time microphone capture and recording capabilities.
// Supports multiple input devices, configurable audio formats, and seamless WAV file export.
// Optimized for business applications requiring reliable audio capture and processing.
public sealed class AudioInputService : IDisposable
{
    // Event fired when new audio samples are available for real-time processing.
    // Provides PCM16 format samples suitable for audio analysis and machine learning applications.
    public event Action<short[]>? OnSamples;

    // Audio sample rate in Hz. Default: 48kHz (professional quality)
    public int SampleRate { get; }
    
    // Number of audio channels. Default: 1 (mono)
    public int Channels { get; } = 1;
    
    // Frame size for audio processing. Default: 1024 samples (~21ms at 48kHz)
    public int FrameSamples { get; } = 1024;

    private WaveInEvent? _waveIn;
    private byte[]? _leftover;
    private bool _isRecording = false;
    private List<short[]> _recordedSamples = new List<short[]>();

    // Initialize audio input service with specified sample rate
    public AudioInputService(int sampleRate = 48000) => SampleRate = sampleRate;
    
    // Indicates if audio recording is currently active
    public bool IsRecording => _isRecording;
    
    // Indicates if recorded audio data is available for export
    public bool HasRecordedAudio => _recordedSamples.Count > 0;
    
    // Enumerates all available audio input devices on the system.
    // Returns a list of device names for user selection in the UI.
    public static string[] GetAvailableInputDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var deviceInfo = WaveInEvent.GetCapabilities(i);
            devices.Add($"{i}: {deviceInfo.ProductName}");
        }
        return devices.ToArray();
    }

    // Starts audio recording from the specified input device.
    // Clears any previous recording data and begins capturing audio in real-time.
    public void Start(int deviceIndex = 0)
    {
        if (_waveIn is not null) return;

        _recordedSamples.Clear();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = new WaveFormat(SampleRate, 16, Channels),
            BufferMilliseconds = 20,    // Low latency for real-time processing
            NumberOfBuffers = 4
        };

        _waveIn.DataAvailable += OnData;
        _waveIn.StartRecording();
        _isRecording = true;
    }

    // Stops audio recording and releases system resources.
    // Preserves recorded audio data for export until explicitly cleared.
    public void Stop()
    {
        if (_waveIn is null) return;

        _waveIn.DataAvailable -= OnData;
        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        _leftover = null;
        _isRecording = false;
    }

    // Internal method handling incoming audio data from the microphone.
    // Processes raw audio buffers into consistent frames for recording and real-time analysis.
    private void OnData(object? sender, WaveInEventArgs e)
    {
        var bytesPerSample = 2 * Channels;
        var neededBytes = FrameSamples * bytesPerSample;

        var src = e.Buffer.AsSpan(0, e.BytesRecorded);
        if (_leftover is { Length: > 0 })
        {
            var merged = new byte[_leftover.Length + src.Length];
            Buffer.BlockCopy(_leftover, 0, merged, 0, _leftover.Length);
            src.CopyTo(merged.AsSpan(_leftover.Length));
            src = merged;
            _leftover = null;
        }

        int offset = 0;
        while (src.Length - offset >= neededBytes)
        {
            var frame = new short[FrameSamples * Channels];
            Buffer.BlockCopy(src.ToArray(), offset, frame, 0, neededBytes);
            offset += neededBytes;
            
            _recordedSamples.Add((short[])frame.Clone());
            OnSamples?.Invoke(frame);
        }

        var remaining = src.Length - offset;
        if (remaining > 0)
        {
            _leftover = new byte[remaining];
            Buffer.BlockCopy(src.ToArray(), offset, _leftover, 0, remaining);
        }
    }

    // Retrieves all recorded audio data as a continuous PCM16 sample array.
    // Returns empty array if no audio has been recorded.
    public short[] GetRecordedAudio()
    {
        if (_recordedSamples.Count == 0) return new short[0];
        
        int totalSamples = _recordedSamples.Sum(frame => frame.Length);
        var result = new short[totalSamples];
        
        int offset = 0;
        foreach (var frame in _recordedSamples)
        {
            Array.Copy(frame, 0, result, offset, frame.Length);
            offset += frame.Length;
        }
        
        return result;
    }
    
    // Clears all recorded audio data from memory.
    // Use this after successfully exporting audio to free up system resources.
    public void ClearRecordedAudio()
    {
        _recordedSamples.Clear();
    }

    // Releases all audio resources and stops any active recording.
    public void Dispose() => Stop();
}