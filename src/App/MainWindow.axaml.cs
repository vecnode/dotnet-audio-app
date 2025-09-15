using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform.Storage;

using App.Audio;
using NAudio.Wave;

namespace App;

// Main application window providing audio recording and system monitoring capabilities.
// Features include microphone input selection, real-time audio level monitoring,
// WAV file recording, and comprehensive system information display.
public partial class MainWindow : Window {
    private DispatcherTimer? _performanceTimer;
    private AudioInputService? _audioService;
    private int _selectedDeviceIndex = 0;
    
    // Debug logging configuration
    private bool _debugEnabled = true;
    private int _debugLineCount = 0;
    private const int MAX_DEBUG_LINES = 100;

    public MainWindow() {                
        InitializeComponent();
        UpdatePlatformInfo();
        StartAudioService();
        PopulateInputDevices();
        InitializePerformanceMonitoring();
    }
    
    protected override void OnClosed(EventArgs e) {
        try {
            _audioService?.Dispose();
        }
        catch (Exception ex)
        {
            _ = ex; // Suppress warning
        }
        
        base.OnClosed(e);
    }
    
    
    
    

    private void StartAudioService() {
        try {
            LogDebug("Initializing NAudio audio service", "INIT");
            _audioService = new AudioInputService(sampleRate: 48000);
            
            LogDebug("NAudio audio service initialized successfully", "INIT");
            
            // Set up audio sample monitoring
            _audioService.OnSamples += OnAudioSamples;
        }
        catch (Exception ex) {
            LogDebug($"Failed to initialize audio service: {ex.Message}", "ERROR");
            
        }
    }
    
    
    private void OnMicrophoneButtonClick(object? sender, RoutedEventArgs e) {
        try {
            if (_audioService == null) {
                LogDebug("Microphone button clicked but audio service not initialized", "ERROR");
                return;
            }
            
            if (_audioService.IsRecording) {
                // Stop recording
                LogDebug("Stopping microphone recording", "AUDIO");
                _audioService.Stop();
                LogDebug("Microphone recording stopped successfully", "AUDIO");
                UpdateButtonContent("MicrophoneButton", "Start Microphone");
                
                // Enable save button if we have recorded audio
                UpdateSaveButtonState();
            } else {
                // Start recording
                LogDebug($"Starting microphone recording with device {_selectedDeviceIndex}", "AUDIO");
                _audioService.Start(_selectedDeviceIndex);
                LogDebug("Microphone recording started successfully", "AUDIO");
                UpdateButtonContent("MicrophoneButton", "Stop Microphone");
                
                // Disable save button while recording
                UpdateSaveButtonState();
            }
        }
        catch (Exception ex) {
            LogDebug($"Microphone button error: {ex.Message}", "ERROR");
        }
    }
    
    private void OnAudioSamples(short[] samples) {
        try {
            // Calculate audio level from samples
            var audioLevel = CalculateAudioLevel(samples);
            
            // Create a simple dB-like display
            var displayLevel = audioLevel > 0.1 ? 20.0 * Math.Log10(audioLevel / 100.0) : -60.0;
            
            // Log audio level changes (but throttle to avoid spam)
            if (_debugLineCount % 5 == 0) { // Log every 5th update
                LogDebug($"Audio Level: {audioLevel:F1}% ({displayLevel:F1} dB)", "AUDIO");
            }
            _debugLineCount++;
            
            // Update UI on the main thread
            Dispatcher.UIThread.Post(() => {
                UpdateAudioLevelText(displayLevel, audioLevel);
            });
        }
        catch (Exception ex) {
            LogDebug($"Audio samples processing error: {ex.Message}", "ERROR");
        }
    }
    
    private float CalculateAudioLevel(short[] samples) {
        if (samples.Length == 0) return 0.0f;
        
        float sum = 0.0f;
        for (int i = 0; i < samples.Length; i++) {
            // Convert 16-bit signed integer to normalized float (-1.0 to +1.0)
            float sample = samples[i] / 32768.0f;
            sum += sample * sample;
        }
        
        var rms = (float)Math.Sqrt(sum / samples.Length);
        return rms * 100; // Convert to percentage (0-100)
    }
    
    private void UpdateAudioLevelText(double dbLevel, double percentage) {
        try {
            var audioLevelText = this.FindControl<TextBlock>("AudioLevelText");
            if (audioLevelText != null) {
                // Show both percentage and dB for better understanding
                audioLevelText.Text = $"{percentage:F1}% ({dbLevel:F1} dB)";
                
                // Change color based on level
                if (percentage > 50) {
                    audioLevelText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 0, 0)); // Red
                } else if (percentage > 10) {
                    audioLevelText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 165, 0)); // Orange
                } else if (percentage > 1) {
                    audioLevelText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(144, 238, 144)); // Light Green
                } else {
                    audioLevelText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(100, 100, 100)); // Gray (silence)
                }
            }
            
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
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
            UpdateTextBlock("MemoryText", $"App Memory: {perfInfo.MemoryUsageMB} MB");
            UpdateTextBlock("GpuAccelerationText", $"GPU Acceleration: {perfInfo.GpuAcceleration}");
            UpdateTextBlock("AudioBackendText", $"Audio Backend: NAudio (Windows)");
            
            // Update audio format information
            if (_audioService != null) {
                var channelType = _audioService.Channels == 1 ? "Mono" : _audioService.Channels == 2 ? "Stereo" : $"{_audioService.Channels}-channel";
                UpdateTextBlock("AudioFormatText", $"Audio Format: {_audioService.SampleRate}Hz, {channelType}, 16-bit (NAudio)");
            }
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
    
    private void UpdateButtonContent(string name, string content) {
        try {
            var button = this.FindControl<Button>(name);
            if (button != null) {
                button.Content = content;
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void UpdateSaveButtonState() {
        try {
            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton != null && _audioService != null) {
                saveButton.IsEnabled = !_audioService.IsRecording && _audioService.HasRecordedAudio;
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void PopulateInputDevices() {
        try {
            var deviceComboBox = this.FindControl<ComboBox>("InputDeviceComboBox");
            if (deviceComboBox != null) {
                var devices = AudioInputService.GetAvailableInputDevices();
                deviceComboBox.ItemsSource = devices;
                
                if (devices.Length > 0) {
                    deviceComboBox.SelectedIndex = 0;
                    _selectedDeviceIndex = 0;
                    LogDebug($"Available input devices: {string.Join(", ", devices)}", "INIT");
                } else {
                    LogDebug("No input devices found", "ERROR");
                }
            }
        }
        catch (Exception ex) {
            LogDebug($"Error populating input devices: {ex.Message}", "ERROR");
        }
    }
    
    private void OnInputDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e) {
        try {
            var deviceComboBox = sender as ComboBox;
            if (deviceComboBox?.SelectedIndex >= 0) {
                _selectedDeviceIndex = deviceComboBox.SelectedIndex;
                LogDebug($"Selected input device: {deviceComboBox.SelectedItem}", "AUDIO");
                
                // If currently recording, restart with new device
                if (_audioService?.IsRecording == true) {
                    LogDebug("Restarting recording with new device", "AUDIO");
                    _audioService.Stop();
                    _audioService.Start(_selectedDeviceIndex);
                }
            }
        }
        catch (Exception ex) {
            LogDebug($"Device selection error: {ex.Message}", "ERROR");
        }
    }
    
    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e) {
        try {
            if (_audioService == null || !_audioService.HasRecordedAudio) {
                LogDebug("No recorded audio to save", "ERROR");
                return;
            }
            
            LogDebug("Opening save dialog for audio file", "AUDIO");
            
            // Get the top level for the file dialog
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) {
                LogDebug("Storage provider not available", "ERROR");
                return;
            }
            
            // Configure file picker options
            var filePickerOptions = new FilePickerSaveOptions
            {
                Title = "Save Audio Recording",
                DefaultExtension = "wav",
                SuggestedFileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("WAV Audio Files")
                    {
                        Patterns = new[] { "*.wav" }
                    }
                }
            };
            
            // Show save dialog
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(filePickerOptions);
            if (file == null) {
                LogDebug("Save dialog cancelled by user", "AUDIO");
                return;
            }
            
            LogDebug($"Saving audio to: {file.Name}", "AUDIO");
            
            // Get recorded audio data
            var audioData = _audioService.GetRecordedAudio();
            if (audioData.Length == 0) {
                LogDebug("No audio data to save", "ERROR");
                return;
            }
            
            // Save as WAV file using NAudio
            await SaveAudioToWavFile(file, audioData);
            
            LogDebug($"Audio saved successfully: {file.Name}", "AUDIO");
            
            // Clear the recorded audio after saving
            _audioService.ClearRecordedAudio();
            UpdateSaveButtonState();
            
        }
        catch (Exception ex) {
            LogDebug($"Save audio error: {ex.Message}", "ERROR");
        }
    }
    
    private async Task SaveAudioToWavFile(IStorageFile file, short[] audioData) {
        try {
            // Open file stream
            await using var stream = await file.OpenWriteAsync();
            
            // Create WAV file writer
            var waveFormat = new WaveFormat(_audioService!.SampleRate, 16, _audioService.Channels);
            using var writer = new WaveFileWriter(stream, waveFormat);
            
            // Write audio data
            writer.WriteSamples(audioData, 0, audioData.Length);
            
            LogDebug($"WAV file created: {audioData.Length} samples, {waveFormat.SampleRate}Hz, {waveFormat.Channels} channels", "AUDIO");
        }
        catch (Exception ex) {
            LogDebug($"Error writing WAV file: {ex.Message}", "ERROR");
            throw;
        }
    }


    
    // Debug logging methods
    private void LogDebug(string message, string level = "INFO") {
        if (!_debugEnabled) return;
        
        try {
            Dispatcher.UIThread.Post(() => {
                var debugTextBlock = this.FindControl<TextBlock>("DebugTextBlock");
                var debugScrollViewer = this.FindControl<ScrollViewer>("DebugScrollViewer");
                
                if (debugTextBlock != null && debugScrollViewer != null) {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    // Add new line if not the first entry
                    var currentText = debugTextBlock.Text;
                    if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith(Environment.NewLine)) {
                        currentText += Environment.NewLine;
                    }
                    
                    currentText += logEntry;
                    
                    // Limit number of lines to prevent memory issues
                    var lines = currentText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    if (lines.Length > MAX_DEBUG_LINES) {
                        currentText = string.Join(Environment.NewLine, lines.Skip(lines.Length - MAX_DEBUG_LINES));
                    }
                    
                    debugTextBlock.Text = currentText;
                    
                    // Auto-scroll to bottom
                    debugScrollViewer.ScrollToEnd();
                }
            });
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private void OnClearDebugClick(object? sender, RoutedEventArgs e) {
        try {
            var debugTextBlock = this.FindControl<TextBlock>("DebugTextBlock");
            if (debugTextBlock != null) {
                debugTextBlock.Text = "[DEBUG] Debug console cleared";
                LogDebug("Debug console cleared by user", "CLEAR");
            }
        }
        catch (Exception ex) {
            _ = ex; // Suppress warning
        }
    }
    
    private async void OnCopyDebugClick(object? sender, RoutedEventArgs e) {
        try {
            var debugTextBlock = this.FindControl<TextBlock>("DebugTextBlock");
            if (debugTextBlock != null) {
                var debugText = debugTextBlock.Text;
                
                // Copy to clipboard using TopLevel
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null) {
                    await topLevel.Clipboard.SetTextAsync(debugText);
                    LogDebug("Debug output copied to clipboard", "SYSTEM");
                } else {
                    LogDebug("Failed to copy debug output - clipboard not available", "ERROR");
                }
            }
        }
        catch (Exception ex) {
            LogDebug($"Copy debug error: {ex.Message}", "ERROR");
        }
    }
    

}

