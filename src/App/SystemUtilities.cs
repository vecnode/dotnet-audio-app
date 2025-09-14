using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace App;

public static class SystemUtilities
{
    public static string GetGpuAccelerationInfo() {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsGpuInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxGpuInfo();
            }
            
            return "Unknown Platform";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
    
    private static string GetWindowsGpuInfo()
    {
        try
        {
            // Check for DirectX support
            var dxVersion = GetDirectXVersion();
            var cudaAvailable = IsCudaAvailable();
            var openGLVersion = GetOpenGLVersion();
            
            var info = $"DirectX {dxVersion}";
            if (cudaAvailable)
                info += ", CUDA Available";
            if (!string.IsNullOrEmpty(openGLVersion))
                info += $", OpenGL {openGLVersion}";
                
            return info;
        }
        catch
        {
            return "Windows GPU (Detection Failed)";
        }
    }
    
    private static string GetLinuxGpuInfo()
    {
        try
        {
            // Check for common Linux GPU drivers
            var mesaVersion = GetMesaVersion();
            var nvidiaAvailable = IsNvidiaAvailable();
            var amdAvailable = IsAmdAvailable();
            var intelAvailable = IsIntelAvailable();
            
            var info = "Linux GPU";
            if (!string.IsNullOrEmpty(mesaVersion))
                info += $", Mesa {mesaVersion}";
            if (nvidiaAvailable)
                info += ", NVIDIA";
            if (amdAvailable)
                info += ", AMD";
            if (intelAvailable)
                info += ", Intel";
                
            return info;
        }
        catch
        {
            return "Linux GPU (Detection Failed)";
        }
    }
    
    
    private static string GetDirectXVersion()
    {
        try
        {
            // Check registry for DirectX version (Windows)
            var dxVersion = Environment.GetEnvironmentVariable("DXSDK_DIR");
            if (!string.IsNullOrEmpty(dxVersion))
                return "SDK Available";
            
            // Check for common DirectX installations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dxPath = Path.Combine(programFiles, "Microsoft DirectX SDK");
            if (Directory.Exists(dxPath))
                return "SDK Installed";
                
            return "Runtime Available";
        }
        catch
        {
            return "Unknown";
        }
    }
    
    private static bool IsCudaAvailable()
    {
        try
        {
            // Check for CUDA installation
            var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH");
            if (!string.IsNullOrEmpty(cudaPath))
                return true;
                
            // Check common CUDA installation paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var cudaPath1 = Path.Combine(programFiles, "NVIDIA GPU Computing Toolkit", "CUDA");
            var cudaPath2 = Path.Combine(programFiles, "NVIDIA Corporation", "NVIDIA GPU Computing Toolkit");
            
            return Directory.Exists(cudaPath1) || Directory.Exists(cudaPath2);
        }
        catch
        {
            return false;
        }
    }
    
    private static string GetOpenGLVersion()
    {
        try
        {
            // Check for OpenGL support
            var openglVersion = Environment.GetEnvironmentVariable("OPENGL_VERSION");
            if (!string.IsNullOrEmpty(openglVersion))
                return openglVersion;
                
            // On Windows, OpenGL is typically available
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Available";
                
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
    
    private static string GetMesaVersion()
    {
        try
        {
            // Check for Mesa (common Linux OpenGL implementation)
            var mesaVersion = Environment.GetEnvironmentVariable("MESA_VERSION");
            if (!string.IsNullOrEmpty(mesaVersion))
                return mesaVersion;
                
            return "Available";
        }
        catch
        {
            return "Unknown";
        }
    }
    
    private static bool IsNvidiaAvailable()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check for NVIDIA drivers on Windows
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var nvidiaPath1 = Path.Combine(programFiles, "NVIDIA Corporation");
                var nvidiaPath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "nvlddmkm.sys");
                var nvidiaPath3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvapi64.dll");
                
                return Directory.Exists(nvidiaPath1) || File.Exists(nvidiaPath2) || File.Exists(nvidiaPath3);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Check for NVIDIA drivers on Linux
                var nvidiaPath = "/usr/lib/nvidia";
                var nvidiaPath2 = "/usr/lib64/nvidia";
                var nvidiaPath3 = "/opt/nvidia";
                var nvidiaPath4 = "/usr/lib/x86_64-linux-gnu/nvidia";
                
                return Directory.Exists(nvidiaPath) || Directory.Exists(nvidiaPath2) || 
                       Directory.Exists(nvidiaPath3) || Directory.Exists(nvidiaPath4);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsAmdAvailable()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check for AMD drivers on Windows
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var amdPath1 = Path.Combine(programFiles, "AMD");
                var amdPath2 = Path.Combine(programFiles, "ATI Technologies");
                var amdPath3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "amd");
                
                return Directory.Exists(amdPath1) || Directory.Exists(amdPath2) || Directory.Exists(amdPath3);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Check for AMD drivers on Linux
                var amdPath = "/usr/lib/amd";
                var amdPath2 = "/usr/lib64/amd";
                var amdPath3 = "/opt/amd";
                var amdPath4 = "/usr/lib/x86_64-linux-gnu/amd";
                
                return Directory.Exists(amdPath) || Directory.Exists(amdPath2) || 
                       Directory.Exists(amdPath3) || Directory.Exists(amdPath4);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsIntelAvailable()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check for Intel drivers on Windows
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var intelPath1 = Path.Combine(programFiles, "Intel");
                var intelPath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "intel");
                var intelPath3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "igdkmd64.sys");
                
                return Directory.Exists(intelPath1) || Directory.Exists(intelPath2) || File.Exists(intelPath3);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Check for Intel drivers on Linux
                var intelPath = "/usr/lib/intel";
                var intelPath2 = "/usr/lib64/intel";
                var intelPath3 = "/opt/intel";
                var intelPath4 = "/usr/lib/x86_64-linux-gnu/intel";
                var intelPath5 = "/usr/lib/i386-linux-gnu/intel";
                
                return Directory.Exists(intelPath) || Directory.Exists(intelPath2) || 
                       Directory.Exists(intelPath3) || Directory.Exists(intelPath4) || Directory.Exists(intelPath5);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    
    public static string GetAudioBackendInfo()
    {
        try
        {
            // Check for platform-specific audio backends
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows Audio (WASAPI)";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "ALSA/PulseAudio";
            }
            
            return "Unknown";
        }
        catch
        {
            return "Error detecting audio backend";
        }
    }
    

    
    public static SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            OSDescription = RuntimeInformation.OSDescription,
            Platform = Environment.OSVersion.Platform.ToString(),
            OSVersion = Environment.OSVersion.VersionString,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Is64BitOS = Environment.Is64BitOperatingSystem,
            Is64BitProcess = Environment.Is64BitProcess,
            DotNetVersion = Environment.Version.ToString(),
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            ProcessorCount = Environment.ProcessorCount,
            SystemPageSize = Environment.SystemPageSize,
            WorkingSet = Environment.WorkingSet,
            TotalMemory = GC.GetTotalMemory(false),
            MachineName = Environment.MachineName,
            UserDomainName = Environment.UserDomainName
        };
    }
    
    public static PerformanceInfo GetCurrentPerformanceInfo()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            
            return new PerformanceInfo
            {
                MemoryUsageMB = currentProcess.WorkingSet64 / (1024 * 1024),
                GpuAcceleration = GetGpuAccelerationInfo(),
                AudioBackend = GetAudioBackendInfo()
            };
        }
        catch (Exception ex)
        {
            return new PerformanceInfo
            {
                MemoryUsageMB = 0,
                GpuAcceleration = $"Error: {ex.Message}",
                AudioBackend = "Error"
            };
        }
    }
}

public class SystemInfo
{
    public string OSDescription { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public string OSArchitecture { get; set; } = string.Empty;
    public string ProcessArchitecture { get; set; } = string.Empty;
    public bool Is64BitOS { get; set; }
    public bool Is64BitProcess { get; set; }
    public string DotNetVersion { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public int SystemPageSize { get; set; }
    public long WorkingSet { get; set; }
    public long TotalMemory { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserDomainName { get; set; } = string.Empty;
}

public class PerformanceInfo
{
    public long MemoryUsageMB { get; set; }
    public string GpuAcceleration { get; set; } = string.Empty;
    public string AudioBackend { get; set; } = string.Empty;
}


