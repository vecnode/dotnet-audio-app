# .NET Audio Control Application Launcher
# Single script that handles everything with proper privileges

$ErrorActionPreference = 'Stop'

# Set execution policy for this process (Windows only)
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned -Force
}

Write-Host "=== .NET Audio Control Application ===" -ForegroundColor Green

# Check if .NET is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK: $dotnetVersion" -ForegroundColor Yellow
} catch {
    Write-Host "Error: .NET SDK not found. Please run .\update.ps1 first to set up the environment." -ForegroundColor Red
    exit 1
}

# Check if project exists
if (-not (Test-Path "src/App/App.csproj")) {
    Write-Host "Error: Project file not found. Please run this script from the project root directory." -ForegroundColor Red
    exit 1
}

try {
    # Restore packages first
    Write-Host "Restoring packages" -ForegroundColor Yellow
    dotnet restore src/App/App.csproj
    
    # Run the application
    Write-Host "Launching application" -ForegroundColor Yellow
    dotnet run --project src/App/App.csproj
}
catch {
    Write-Host "Error running application: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Try running .\update.ps1 to set up the environment first." -ForegroundColor Yellow
    exit 1
}

Write-Host "Application finished" -ForegroundColor Green
