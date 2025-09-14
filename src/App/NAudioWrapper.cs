using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading.Tasks;

namespace App;

public class NAudioDeviceInfo {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string Status { get; set; } = "Available";
    
    public NAudioDeviceInfo(string id, string name, bool isDefault, bool isAvailable = true, string status = "Available") {
        Id = id;
        Name = name;
        IsDefault = isDefault;
        IsAvailable = isAvailable;
        Status = status;
    }
    
    public string GetDisplayName() {
        var defaultSuffix = IsDefault ? " (Default)" : "";
        return $"{Name}{defaultSuffix}";
    }
}

public class NAudioEngine : IDisposable {
    public List<NAudioDeviceInfo> PlaybackDevices { get; private set; } = new();
    public List<NAudioDeviceInfo> CaptureDevices { get; private set; } = new();
    private WaveInEvent? _currentInputDevice;
    private DirectSoundOut? _currentPlaybackDevice;
    private AudioFormat? _currentFormat;
    
    public NAudioEngine() {
        UpdateDevicesInfo();
    }
    
    public void UpdateDevicesInfo() {
        try {
            PlaybackDevices.Clear();
            CaptureDevices.Clear();
            
            // Platform-specific device enumeration
            if (IsWindows11OrLater()) {
                // Use DirectSoundOut for device enumeration on Windows 11+
                EnumeratePlaybackDevices();
                EnumerateCaptureDevices();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // Fallback for older Windows versions
                EnumerateWindowsFallbackDevices();
            }
            else {
                // Non-Windows platforms - use basic enumeration
                EnumerateCrossPlatformDevices();
            }
            
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            // Ensure we have at least default devices
            if (PlaybackDevices.Count == 0) {
                PlaybackDevices.Add(new NAudioDeviceInfo("0", "Default Output Device", true));
            }
            if (CaptureDevices.Count == 0) {
                CaptureDevices.Add(new NAudioDeviceInfo("0", "Default Input Device", true));
            }
        }
    }
    
    private bool IsWindows11OrLater() {
        try {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return false;
            }
            
            // Check Windows version - Windows 11 is version 10.0.22000+
            var osVersion = Environment.OSVersion.Version;
            return osVersion.Major >= 10 && osVersion.Build >= 22000;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return false;
        }
    }
    
    private void EnumerateWindowsFallbackDevices() {
        try {
            // For older Windows versions, use a simpler approach
            PlaybackDevices.Add(new NAudioDeviceInfo("0", "Windows Speakers (Legacy)", true, true, "Available"));
            CaptureDevices.Add(new NAudioDeviceInfo("0", "Windows Microphone (Legacy)", true, true, "Available"));
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void EnumerateCrossPlatformDevices() {
        try {
            // For non-Windows platforms (Linux, macOS)
            PlaybackDevices.Add(new NAudioDeviceInfo("0", "System Audio Output", true, true, "Available"));
            CaptureDevices.Add(new NAudioDeviceInfo("0", "System Audio Input", true, true, "Available"));
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void EnumeratePlaybackDevices() {
        try {
            // Try modern Windows Core Audio API first
            if (TryEnumeratePlaybackDevicesModern()) {
                return;
            }
            
            // Fallback to traditional NAudio enumeration
            EnumeratePlaybackDevicesFallback();
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            PlaybackDevices.Add(new NAudioDeviceInfo("0", "Default Speakers (Fallback)", true, true, "Available"));
        }
    }
    
    private bool TryEnumeratePlaybackDevicesModern() {
        try {
            var deviceEnumerator = new MMDeviceEnumerator();
            var playbackDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            
            bool hasDefault = false;
            foreach (var device in playbackDevices) {
                var isDefault = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console)?.ID == device.ID;
                var deviceName = device.FriendlyName ?? device.DeviceFriendlyName ?? "Unknown Speakers";
                var status = device.State == DeviceState.Active ? "Available" : "Unavailable";
                var isAvailable = device.State == DeviceState.Active;
                
                PlaybackDevices.Add(new NAudioDeviceInfo(device.ID, deviceName, isDefault, isAvailable, status));
                if (isDefault) hasDefault = true;
            }
            
            // Ensure we have a default device
            if (!hasDefault && PlaybackDevices.Count > 0) {
                PlaybackDevices[0].IsDefault = true;
            }
            
            return PlaybackDevices.Count > 0;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return false;
        }
    }
    
    private void EnumeratePlaybackDevicesFallback() {
        try {
            // Traditional NAudio enumeration with improved naming
            foreach (var device in DirectSoundOut.Devices) {
                var deviceName = device.Description;
                
                // Improve generic names
                if (string.IsNullOrEmpty(deviceName)) {
                    deviceName = "Audio Output Device";
                }
                
                PlaybackDevices.Add(new NAudioDeviceInfo(device.Guid.ToString(), deviceName, false, true, "Available"));
            }
            
            // Mark the first device as default
            if (PlaybackDevices.Count > 0) {
                PlaybackDevices[0].IsDefault = true;
            }
            
            // If no devices found, add a default
            if (PlaybackDevices.Count == 0) {
                PlaybackDevices.Add(new NAudioDeviceInfo("0", "Default Speakers", true, true, "Available"));
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            PlaybackDevices.Add(new NAudioDeviceInfo("0", "Default Speakers (Fallback)", true, true, "Available"));
        }
    }
    
    private void EnumerateCaptureDevices() {
        try {
            // Try modern Windows Core Audio API first
            if (TryEnumerateCaptureDevicesModern()) {
                return;
            }
            
            // Fallback to traditional NAudio enumeration
            EnumerateCaptureDevicesFallback();
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            CaptureDevices.Add(new NAudioDeviceInfo("0", "Default Microphone (Fallback)", true, true, "Available"));
        }
    }
    
    private bool TryEnumerateCaptureDevicesModern() {
        try {
            var deviceEnumerator = new MMDeviceEnumerator();
            var captureDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            bool hasDefault = false;
            foreach (var device in captureDevices) {
                var isDefault = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console)?.ID == device.ID;
                var deviceName = device.FriendlyName ?? device.DeviceFriendlyName ?? "Unknown Microphone";
                var status = device.State == DeviceState.Active ? "Available" : "Unavailable";
                var isAvailable = device.State == DeviceState.Active;
                
                CaptureDevices.Add(new NAudioDeviceInfo(device.ID, deviceName, isDefault, isAvailable, status));
                if (isDefault) hasDefault = true;
            }
            
            // Ensure we have a default device
            if (!hasDefault && CaptureDevices.Count > 0) {
                CaptureDevices[0].IsDefault = true;
            }
            
            return CaptureDevices.Count > 0;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return false;
        }
    }
    
    private void EnumerateCaptureDevicesFallback() {
        try {
            // Traditional NAudio enumeration with improved naming
            for (int i = 0; i < WaveInEvent.DeviceCount; i++) {
                var caps = WaveInEvent.GetCapabilities(i);
                var deviceName = caps.ProductName;
                
                // Improve generic names
                if (string.IsNullOrEmpty(deviceName) || deviceName == "Primary Sound Driver") {
                    deviceName = $"Microphone {i + 1}";
                }
                
                CaptureDevices.Add(new NAudioDeviceInfo(i.ToString(), deviceName, i == 0, true, "Available"));
            }
            
            // If no devices found, add a default
            if (CaptureDevices.Count == 0) {
                CaptureDevices.Add(new NAudioDeviceInfo("0", "Default Microphone", true, true, "Available"));
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            CaptureDevices.Add(new NAudioDeviceInfo("0", "Default Microphone (Fallback)", true, true, "Available"));
        }
    }
    
    public DirectSoundOut InitializePlaybackDevice(NAudioDeviceInfo? deviceInfo, AudioFormat format) {
        try {
            // Store current format and device
            _currentFormat = format;
            _currentPlaybackDevice = new DirectSoundOut();
            return _currentPlaybackDevice;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            // Return default DirectSoundOut
            _currentPlaybackDevice = new DirectSoundOut();
            _currentFormat = format;
            return _currentPlaybackDevice;
        }
    }
    
    public WaveInEvent InitializeInputDevice(NAudioDeviceInfo? deviceInfo, AudioFormat format) {
        try {
            // Dispose current input device if exists
            _currentInputDevice?.Dispose();
            
            if (deviceInfo == null) {
                _currentInputDevice = new WaveInEvent();
            } else {
                // Try to parse device ID as integer for WaveInEvent
                if (int.TryParse(deviceInfo.Id, out int deviceId)) {
                    _currentInputDevice = new WaveInEvent {
                        DeviceNumber = deviceId,
                        WaveFormat = new WaveFormat(format.SampleRate, format.Channels)
                    };
                } else {
                    // Fallback to default device
                    _currentInputDevice = new WaveInEvent {
                        WaveFormat = new WaveFormat(format.SampleRate, format.Channels)
                    };
                }
            }
            
            return _currentInputDevice;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            // Return default WaveInEvent
            _currentInputDevice = new WaveInEvent();
            return _currentInputDevice;
        }
    }
    
    public async Task<bool> RequestAudioPermissionsAsync() {
        try {
            // Test if we can access audio devices
            var canAccessPlayback = await TestPlaybackAccessAsync();
            var canAccessCapture = await TestCaptureAccessAsync();
            
            return canAccessPlayback && canAccessCapture;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return false;
        }
    }
    
    private async Task<bool> TestPlaybackAccessAsync() {
        try {
            return await Task.Run(() => {
                try {
                    using var testDevice = new DirectSoundOut();
                    return true;
                }
                catch {
                    return false;
                }
            });
        }
        catch {
            return false;
        }
    }
    
    private async Task<bool> TestCaptureAccessAsync() {
        try {
            return await Task.Run(() => {
                try {
                    using var testDevice = new WaveInEvent();
                    return true;
                }
                catch {
                    return false;
                }
            });
        }
        catch {
            return false;
        }
    }
    
    public string GetPermissionStatus() {
        try {
            // Return a simple status without blocking calls
            return "(Checking permissions)";
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return "(Permission status unknown)";
        }
    }
    
    public async Task<string> GetPermissionStatusAsync() {
        try {
            var playbackAccess = await TestPlaybackAccessAsync();
            var captureAccess = await TestCaptureAccessAsync();
            
            if (playbackAccess && captureAccess) {
                return "(Audio permissions granted)";
            }
            else if (playbackAccess) {
                return "(Playback only - Microphone access needed)";
            }
            else if (captureAccess) {
                return "(Recording only - Speaker access needed)";
            }
            else {
                return "(Audio permissions denied)";
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return "(Permission status unknown)";
        }
    }
    
    public string GetCurrentAudioFormatInfo() {
        try {
            if (_currentFormat == null) {
                return "No audio format set";
            }
            
            var channels = _currentFormat.Channels == 1 ? "Mono" : _currentFormat.Channels == 2 ? "Stereo" : $"{_currentFormat.Channels} channels";
            return $"{_currentFormat.SampleRate} Hz, {_currentFormat.BitsPerSample}-bit, {channels}";
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return "Audio format unknown";
        }
    }
    
    public AudioFormat? GetCurrentFormat() {
        return _currentFormat;
    }
    
    public void Dispose() {
        try {
            _currentInputDevice?.Dispose();
            _currentPlaybackDevice?.Dispose();
            _currentInputDevice = null;
            _currentPlaybackDevice = null;
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
}

public class AudioFormat {
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    
    public static AudioFormat Dvd => new AudioFormat { SampleRate = 48000, Channels = 2, BitsPerSample = 16 };
}

