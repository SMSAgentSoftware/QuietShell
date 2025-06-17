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

# Execute the PowerShell script in an embedded runspace, logging to C:\Temp and including script output in the log file
QuietShell.exe -File .\MyScript.ps1 -LogPath "C:\Temp" -LogOutputStream

# Execute a script with a parameter
QuietShell.exe .\MyScript.ps1 "C:\Temp\MyFile.txt"
```

## Supported parameters
| Parameter | Description | Supported execution modes |
| ---------- | ----------- | -------------------------- |
| UseRunspace | [Default] Executes the PowerShell in an embedded runspace | Runspace |
| UseProcess | Executes the PowerShell in an isolated PowerShell process | Process |
| File | The full path to a PowerShell script to execute | Runspace, Process |
| (args) | Specify any parameters to pass to the script directly after the script file path | Runspace, Process |
| Command | The PowerShell command to execute, eg "Get-ComputerInfo" | Runspace, Process |
| ExecutionPolicy | A supported execution policy. If not specified, the current execution policy will apply | Runspace, Process |



