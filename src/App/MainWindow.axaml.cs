// functions

using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Threading;
using NAudio.Wave;


namespace App;

// Wrapper class to display device names properly in ComboBox
public class DeviceDisplayItem {
    public NAudioDeviceInfo Device { get; set; }
    public string DisplayName { get; set; }
    
    public DeviceDisplayItem(NAudioDeviceInfo device) {
        Device = device;
        DisplayName = device.GetDisplayName();
    }
    
    public override string ToString() {
        return DisplayName;
    }
}

public partial class MainWindow : Window {
    private DispatcherTimer? _performanceTimer;
    private NAudioEngine? _audioEngine;
    private DirectSoundOut? _playbackDevice;
    private WaveInEvent? _inputDevice;
    

    // Constructor
    public MainWindow() {                
        InitializeComponent(); // Avalonia Setup
        
        UpdatePlatformInfo(); // Text blocks specs from the machine

        StartAudioEngine(); // Audio engine
        
        InitializePerformanceMonitoring();
        
    }
    
    protected override void OnClosed(EventArgs e) {
        try {
            _playbackDevice?.Dispose();
            _inputDevice?.Dispose();
            _audioEngine?.Dispose();
            
        }
        catch (Exception ex)
        {
            _ = ex; // Suppress warning
        }
        
        base.OnClosed(e);
    }
    
    
    
    

    private void StartAudioEngine() {
        try {
            _audioEngine = new NAudioEngine();

            // Update device information to get all available devices
            _audioEngine.UpdateDevicesInfo();

            var format = AudioFormat.Dvd; // 48kHz, 16-bit Stereo
            var defaultPlaybackDevice = _audioEngine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
            var defaultInputDevice = _audioEngine.CaptureDevices.FirstOrDefault(x => x.IsDefault);
            
            _playbackDevice = _audioEngine.InitializePlaybackDevice(defaultPlaybackDevice, format);
            _inputDevice = _audioEngine.InitializeInputDevice(defaultInputDevice, format);
            
            // Populate device combo boxes
            PopulateDeviceComboBoxes();
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
            // Error handling - could add UI feedback here if needed
        }
    }
    
   




    private void UpdatePlatformInfo() {
        try {
            var platformOS = this.FindControl<TextBlock>("PlatformOS");
            var systemInfo = SystemUtilities.GetSystemInfo();
            var osInfoText = $"OS: {systemInfo.Platform} {systemInfo.OSVersion}";
            
            if (platformOS != null)
                platformOS.Text = osInfoText;
            
            UpdateTextBlock("DotNetVersionText", $".NET: {systemInfo.DotNetVersion}");
            UpdateTextBlock("Is64BitOSText", $"64-bit OS: {systemInfo.Is64BitOS}");
            UpdateTextBlock("Is64BitProcessText", $"64-bit Process: {systemInfo.Is64BitProcess}");
            UpdateTextBlock("RuntimeIdentifierText", $"Runtime ID: {systemInfo.RuntimeIdentifier}");
            UpdateTextBlock("ProcessorCountText", $"Processors: {systemInfo.ProcessorCount}");
            UpdateTextBlock("WorkingSetText", $"Working Set: {systemInfo.WorkingSet / (1024 * 1024):N0} MB");
            UpdateTextBlock("TotalMemoryText", $"Total Memory: {systemInfo.TotalMemory / (1024 * 1024):N0} MB");
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void InitializePerformanceMonitoring() {
        try {
            _performanceTimer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _performanceTimer.Tick += OnPerformanceTimerTick;
            _performanceTimer.Start();
            
            UpdatePerformanceInfo();
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    

    // Performance timer tick
    private void OnPerformanceTimerTick(object? sender, EventArgs e) {
        try {
            UpdatePerformanceInfo();
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    
    private void UpdatePerformanceInfo() {
        try {
            var perfInfo = SystemUtilities.GetCurrentPerformanceInfo();            
            var audioBackend = perfInfo.AudioBackend;
            UpdateTextBlock("MemoryText", $"App Memory: {perfInfo.MemoryUsageMB} MB");
            UpdateTextBlock("GpuAccelerationText", $"GPU Acceleration: {perfInfo.GpuAcceleration}");
            UpdateTextBlock("AudioBackendText", $"Audio Backend: {audioBackend}");
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    
    private void UpdateTextBlock(string name, string text) {
        try {
            var textBlock = this.FindControl<TextBlock>(name);
            if (textBlock != null)
            {
                textBlock.Text = text;
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }

    private void PopulateDeviceComboBoxes() {
        try {
            if (_audioEngine == null) return;

            // Populate output devices combo box
            var outputComboBox = this.FindControl<ComboBox>("OutputDeviceComboBox");
            if (outputComboBox != null) {
                var playbackDevices = _audioEngine.PlaybackDevices.Select(d => new DeviceDisplayItem(d)).ToList();
                outputComboBox.ItemsSource = playbackDevices;
                outputComboBox.SelectedItem = playbackDevices.FirstOrDefault(x => x.Device.IsDefault);                
            }

            // Populate input devices combo box
            var inputComboBox = this.FindControl<ComboBox>("InputDeviceComboBox");
            if (inputComboBox != null) {
                var captureDevices = _audioEngine.CaptureDevices.Select(d => new DeviceDisplayItem(d)).ToList();
                inputComboBox.ItemsSource = captureDevices;
                inputComboBox.SelectedItem = captureDevices.FirstOrDefault(x => x.Device.IsDefault);
                
            }

        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }

    private void OnOutputDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        try {
            if (_audioEngine == null || _playbackDevice == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is DeviceDisplayItem selectedItem) {
                // Dispose current device
                _playbackDevice?.Dispose();

                // Initialize new device
                var format = AudioFormat.Dvd;
                _playbackDevice = _audioEngine.InitializePlaybackDevice(selectedItem.Device, format);
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }

    private void OnInputDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        try {
            if (_audioEngine == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is DeviceDisplayItem selectedItem) {
                // Dispose current input device
                _inputDevice?.Dispose();

                // Initialize new input device
                var format = AudioFormat.Dvd;
                _inputDevice = _audioEngine.InitializeInputDevice(selectedItem.Device, format);
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
}

