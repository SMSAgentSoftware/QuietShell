# QuietShell
## Execute PowerShell scripts or commands silently with no console window

**QuietShell** is a command-line application for headless PowerShell execution supporting both in-process runspaces and out-of-process execution models. QuietShell eliminates console window visibility while maintaining full PowerShell functionality, parameter passing and output capture, making it ideal for scheduled tasks, custom code projects or automation scenarios where console visibility would be disruptive.

## Features
- **Hidden Execution**: Runs PowerShell scripts without showing console windows
- **Dual Execution Modes**: 
  - Embedded runspace (faster, in-process)
  - External process (more isolated, full compatibilty)
- **CMTrace Logging**: Logs to CMTrace-compatible format with automatic rollover
- **Self-Contained**: Can be deployed as a standalone executable
- **Digitally signed** Releases are digitally signed to reduce AV interference

## Requirements
- .NET Framework 4.8 or later
- Windows PowerShell 5.1 (PowerShell Core is not supported)
- Windows 10/11 or Windows Server 2016+
- 64-bit OS

## Examples
```cmd
# Execute the PowerShell script in an embedded runspace honoring the current execution policy
QuietShell.exe .\MyScript.ps1

# Execute the PowerShell command in an embedded runspace honoring the current execution policy
QuietShell.exe -Command "$csinfo = Get-ComputerInfo; $csinfo | Out-File C:\Temp\csinfo.txt -Force"

# Execute the PowerShell script in a PowerShell process, overriding the current execution policy and not loading the PowerShell profile
QuietShell.exe -UseProcess -File .\MyScript.ps1 -ExecutionPolicy Bypass -NoProfile

QuietShell.exe -File .\MyScript.ps1 -LogPath "C:\
```
