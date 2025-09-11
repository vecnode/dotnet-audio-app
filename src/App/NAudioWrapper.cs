using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace App;

// NAudio wrapper classes to match SoundFlow interface
public class NAudioDeviceInfo {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    
    public NAudioDeviceInfo(string id, string name, bool isDefault) {
        Id = id;
        Name = name;
        IsDefault = isDefault;
    }
}

public class NAudioEngine : IDisposable {
    public List<NAudioDeviceInfo> PlaybackDevices { get; private set; } = new();
    public List<NAudioDeviceInfo> CaptureDevices { get; private set; } = new();
    
    public NAudioEngine() {
        UpdateDevicesInfo();
    }
    
    public void UpdateDevicesInfo() {
        try {
            // Cross-platform device enumeration
            PlaybackDevices.Clear();
            CaptureDevices.Clear();
            
            // Platform-specific device enumeration
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                EnumerateWindowsDevices();
            } else {
                // For non-Windows platforms, NOT TESTED
                PlaybackDevices.Add(new NAudioDeviceInfo("0", "Default Output Device", true));
                CaptureDevices.Add(new NAudioDeviceInfo("0", "Default Input Device", true));
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
    
    private void EnumerateWindowsDevices() {
        try {
            // Windows: Use Core Audio API to enumerate real devices
            using (var deviceEnumerator = new MMDeviceEnumerator()) {
                // Get default devices
                var defaultPlayback = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                var defaultCapture = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                
                
                // Add default devices first
                PlaybackDevices.Add(new NAudioDeviceInfo(defaultPlayback.ID, defaultPlayback.FriendlyName, true));
                CaptureDevices.Add(new NAudioDeviceInfo(defaultCapture.ID, defaultCapture.FriendlyName, true));
                
                // Try to enumerate additional devices using a different approach
                try {
                    // Use the device collection directly
                    var deviceCollection = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    foreach (MMDevice device in deviceCollection) {
                        // Skip if already added as default
                        if (device.ID != defaultPlayback.ID) {
                            PlaybackDevices.Add(new NAudioDeviceInfo(device.ID, device.FriendlyName, false));
                        }
                    }
                }
                catch (Exception ex) {
                    _ = ex; // Suppress warning
                }
                
                try {
                    // Use the device collection directly
                    var deviceCollection = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    foreach (MMDevice device in deviceCollection) {
                        // Skip if already added as default
                        if (device.ID != defaultCapture.ID) {
                            CaptureDevices.Add(new NAudioDeviceInfo(device.ID, device.FriendlyName, false));
                        }
                    }
                }
                catch (Exception ex) {
                    _ = ex; // Suppress warning
                }
            }
            
            
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            // Fallback to DirectSound enumeration
            EnumerateWindowsDevicesFallback();
        }
    }
    
    private void EnumerateWindowsDevicesFallback() {
        try {
            // Fallback: Use DirectSoundOut for more detailed device info
            foreach (var device in DirectSoundOut.Devices) {
                PlaybackDevices.Add(new NAudioDeviceInfo(device.Guid.ToString(), device.Description, false));
            }
            
            // For input devices, we'll use a simple approach
            for (int i = 0; i < WaveInEvent.DeviceCount; i++) {
                var caps = WaveInEvent.GetCapabilities(i);
                CaptureDevices.Add(new NAudioDeviceInfo(i.ToString(), caps.ProductName, i == 0));
            }
            
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    
    public object InitializePlaybackDevice(NAudioDeviceInfo? deviceInfo, object format) {
        try {
            // Just return a simple object - no actual audio initialization
            return new { DeviceId = deviceInfo?.Id ?? "0", DeviceName = deviceInfo?.Name ?? "Default" };
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            return new { DeviceId = "0", DeviceName = "Default" };
        }
    }
    
    public void Dispose() {
        // No resources to dispose since we're not initializing audio
    }
}

// Audio format constants to match SoundFlow
public static class AudioFormat {
    public static object Dvd => new { SampleRate = 48000, Channels = 2, BitsPerSample = 16 };
}