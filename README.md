
# dotnet-audio-app

This repository contains a build to create Desktop audio apps to interface with other OS apps in .NET. Cross-platform .NET Application with Avalonia, ONNX, and NAudio. It will add the AI Runtime in the future. This repository focuses on developing with open libraries and models. Tested on Windows 11.

![.NET Audio Controller Interface](.\media\20250911.png)

### Setup on Windows 11 Laptop:
.NET 8 SDK (LTS)

```powershell
# On powershell
winget install --id Microsoft.DotNet.SDK.8 -e
dotnet -v

# For clean build (if you have issues)  
# cd to current folder .\dotnet-audio-app\  

dotnet clean src/App/App.csproj  
dotnet build src/App/App.csproj  
dotnet run --project src/App/App.csproj  

# Or use your custom script  
.\start.ps1 

# For Release Windows
dotnet publish src/App/App.csproj -c Release -r win-x64 --self-contained true   
```  

### Features

- Compiles Avalonia, NAudio and ONNX
- Accesses available audio input/output devices
- Accesses system information (OS, hardware)


### Design Guidelines

- This repository should be developed as cross-platform as possible.
- For best cross-platform behavior, the script should start with CPU ONNX Runtime. 
- On Windows, we will enable GPU via DirectML in the future (no CUDA install needed):
- `start.ps1` starts `run.ps1` and that should be enough for Windows/Linux development.
- `setup_env.ps1` is to setup the repository from version control a first time and fix `.\cache\` and `.\cache\packages\`

Uncomment the DirectML block in run.ps1, or run once:  
  
```
dotnet add src/App/App.csproj package Microsoft.ML.OnnxRuntime.DirectML
```    

### Next steps

- Manually ask for permissions
- Time clock
- Develop a modular build to include ONNX (or not)
- Develop a Docker image and test on WSL2
- Allow for audio card switching and tone test on OUT
- Allow for Microphone Audio IN
