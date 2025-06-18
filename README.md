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
- **Digitally signed**: Releases are digitally signed to reduce AV interference

## Requirements
- .NET Framework 4.8 or later
- Windows PowerShell 5.1 (PowerShell Core is not supported)
- Windows 10/11 or Windows Server 2016+
- 64-bit OS

## How to get it
Download the release and extract the zip file. Alternatively you can compile the code yourself with Visual Studio, but the compiled executable won't be digitally signed unless you do it.

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
| LogPath | Specify the path to the log directory if required. If not specified, will log to the temp directory of the executing context | Runspace, Process |
| ExecutionPolicy | A supported execution policy. If not specified, the current execution policy will apply | Runspace, Process |
| NoProfile | PowerShell parameter to not load the PowerShell profile | Process |
| EncodedCommand | PowerShell parameter to use a base-64-encoded string version of a command | Runspace, Process |
| InputFormat | PowerShell parameter to specify the input format of data (TEXT/XML) | Process |
| OutputFormat | PowerShell parameter to specify the output format of data (TEXT/XML) | Process |
| ConfigurationName | PowerShell parameter to specify a configuration endpoint in which PowerShell is run | Process |
| WorkingDirectory | PowerShell's working directory | Runspace, Process |
| MTA | Multi-threaded apartment state. The default is STA | Runspace, Process |
| Timeout | How long to wait (in seconds) for the script or command to complete. Default is 30 minutes | Runspace, Process |
| LogOutputStream | By default, standard output is not logged. Use this parameter to include standard output in the log | Runspace, Process |

## Logging
By default, script execution state together with warning and error output from scripts or commands are logged to a file *QuietShell.log* located in the temp directory of the executing user context. Standard output is not included in the log as it could be undesirable and contain sensitive data. To include standard output in the log, use the *LogOutputStream* parameter.
To specify an alternative location for the log file, use the *LogPath* parameter. Just specify the directory path. Ensure that the executing context has the necessary permissions to this location.

## Execution policy
By default, the current execution policy for the executing user and scope will be honored. To specify an execution policy, use the *ExecutionPolicy* parameter and provide a supported value. Note that in some cases in may not be possible to override the prevailing execution policy.

## Execution modes
By default, PowerShell scripts and commands will be executed in-process using a PowerShell runspace. This provides the best performance, however it can be preferable in some cases to spawn a seperate, isolated PowerShell process for full compatibility. Use the *UseProcess* to run PowerShell in an isolated process. If your script or command calls an external cmdline program (eg dsregcmd.exe) when using Runspace mode, these may use a console window and you may see a brief flash on the screen while they execute. For this scenario, an isolated process will work better and provide a fully silent execution.

## Default parameters
By default, the *NoLogo* and *NonInteractive* parameters are specified when executing PowerShell in a separate process. This is non-configurable and ensures efficient execution. Ensure that your PowerShell code does not attempt to interact with the user as this will not be possible.

## Antivirus flagging
Although the executable is digitally signed, it may still be flagged by antivirus engines due to the nature of what it does. You may need to whitelist the file with your AV provider.
