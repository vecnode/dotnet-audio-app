# Update and Setup Folder Script for .NET Audio Control Application
# not tested

Write-Host "=== .NET Audio Control - Environment Setup ===" -ForegroundColor Green

# Check if .NET is installed
Write-Host "Checking .NET installation" -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET SDK not found. Please install .NET 8.0 or later from https://dotnet.microsoft.com/download" -ForegroundColor Red
    Write-Host "After installing .NET, run this script again." -ForegroundColor Yellow
    exit 1
}

# Check if we're in the correct directory
if (-not (Test-Path "src/App/App.csproj")) {
    Write-Host "Please run this script from the project root directory" -ForegroundColor Red
    exit 1
}

Write-Host "`nChecking project dependencies" -ForegroundColor Yellow

# Restore NuGet packages
Write-Host "Restoring NuGet packages" -ForegroundColor Yellow
try {
    dotnet restore src/App/App.csproj
    Write-Host "Packages restored successfully" -ForegroundColor Green
} catch {
    Write-Host "Failed to restore packages: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# List installed packages
Write-Host "`nInstalled packages:" -ForegroundColor Yellow
try {
    dotnet list src/App/App.csproj package
} catch {
    Write-Host "Could not list packages: $($_.Exception.Message)" -ForegroundColor Red
}

# Check for required packages
Write-Host "`nChecking for required packages" -ForegroundColor Yellow
$requiredPackages = @(
    "Avalonia",
    "NAudio",
    "Microsoft.ML.OnnxRuntime"
)

$missingPackages = @()
foreach ($package in $requiredPackages) {
    try {
        $result = dotnet list src/App/App.csproj package | Select-String $package
        if ($result) {
            Write-Host "$package" -ForegroundColor Green
        } else {
            $missingPackages += $package
            Write-Host "$package (missing)" -ForegroundColor Red
        }
    } catch {
        $missingPackages += $package
        Write-Host "$package (check failed)" -ForegroundColor Red
    }
}

if ($missingPackages.Count -gt 0) {
    Write-Host "`nInstalling missing packages" -ForegroundColor Yellow
    foreach ($package in $missingPackages) {
        try {
            Write-Host "Installing $package" -ForegroundColor Yellow
            dotnet add src/App/App.csproj package $package
            Write-Host "$package installed" -ForegroundColor Green
        } catch {
            Write-Host "Failed to install $package: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Build the project
Write-Host "`nBuilding project" -ForegroundColor Yellow
try {
    dotnet build src/App/App.csproj --configuration Release
    Write-Host "Project built successfully" -ForegroundColor Green
} catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check the error messages above and fix any issues." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== Environment Setup Complete ===" -ForegroundColor Green
Write-Host "You can now run the application using: .\run.ps1" -ForegroundColor Cyan
Write-Host "Or directly with: dotnet run --project src/App/App.csproj" -ForegroundColor Cyan

Write-Host "`n=== NAudio Audio Engine ===" -ForegroundColor Yellow
Write-Host "NAudio provides cross-platform audio functionality:" -ForegroundColor White
Write-Host "Windows: Uses Windows Audio Session API (WASAPI)" -ForegroundColor Cyan
Write-Host "Linux: Uses ALSA/PulseAudio" -ForegroundColor Cyan
Write-Host "macOS: Uses Core Audio" -ForegroundColor Cyan
Write-Host "`nNote: NAudio handles all audio dependencies automatically." -ForegroundColor Green


