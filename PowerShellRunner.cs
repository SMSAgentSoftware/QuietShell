using Microsoft.PowerShell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace QuietShell
{
    public class PowerShellRunner
    {
        private readonly CMTraceLogger _logger;
        private readonly RunnerOptions _options;

        public PowerShellRunner(CMTraceLogger logger, RunnerOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int Execute()
        {
            try
            {
                _logger.LogInfo($"Starting PowerShell execution. Mode: {(_options.UseEmbeddedRunspace ? "Embedded Runspace" : "External Process")}", "PowerShellRunner");

                if (_options.UseEmbeddedRunspace)
                {
                    return ExecuteWithRunspace();
                }
                else
                {
                    return ExecuteWithProcess();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PowerShell execution failed: {ex.Message}", "PowerShellRunner", ex);
                return -1;
            }
        }

        private int ExecuteWithRunspace()
        {
            _logger.LogInfo("Creating PowerShell runspace", "ExecuteWithRunspace");

            try
            {
                // Create initial session state
                var initialSessionState = InitialSessionState.CreateDefault();

                if (!string.IsNullOrEmpty(_options.ExecutionPolicy))
                {
                    var policy = ParseExecutionPolicy(_options.ExecutionPolicy);
                    initialSessionState.ExecutionPolicy = policy;
                }

                // Create runspace
                using (var runspace = RunspaceFactory.CreateRunspace(initialSessionState))
                {
                    // Configure runspace
                    runspace.ApartmentState = _options.MTA ? System.Threading.ApartmentState.MTA : System.Threading.ApartmentState.STA;
                    runspace.ThreadOptions = PSThreadOptions.ReuseThread;

                    runspace.Open();
                    _logger.LogInfo("PowerShell runspace opened successfully", "ExecuteWithRunspace");

                    using (var powershell = PowerShell.Create())
                    {
                        powershell.Runspace = runspace;

                        // Set working directory if specified
                        if (!string.IsNullOrEmpty(_options.WorkingDirectory))
                        {
                            powershell.AddCommand("Set-Location").AddParameter("Path", _options.WorkingDirectory);
                            powershell.Invoke();
                            powershell.Commands.Clear();
                        }

                        // Add script or command
                        if (!string.IsNullOrEmpty(_options.ScriptPath))
                        {
                            if (!File.Exists(_options.ScriptPath))
                            {
                                _logger.LogError($"Script file not found: {_options.ScriptPath}", "ExecuteWithRunspace");
                                return 1;
                            }

                            powershell.AddCommand(_options.ScriptPath);

                            // Add script arguments
                            foreach (var arg in _options.ScriptArguments)
                            {
                                powershell.AddArgument(arg);
                            }
                        }
                        else if (!string.IsNullOrEmpty(_options.Command))
                        {
                            powershell.AddScript(_options.Command);
                        }
                        else if (!string.IsNullOrEmpty(_options.EncodedCommand))
                        {
                            var decodedCommand = Encoding.Unicode.GetString(Convert.FromBase64String(_options.EncodedCommand));
                            powershell.AddScript(decodedCommand);
                        }
                        else
                        {
                            _logger.LogError("No script, command, or encoded command specified", "ExecuteWithRunspace");
                            return 1;
                        }

                        _logger.LogInfo("Executing PowerShell script/command", "ExecuteWithRunspace");

                        // Execute and handle results with timeout
                        IAsyncResult asyncResult = powershell.BeginInvoke();
                        bool completed = asyncResult.AsyncWaitHandle.WaitOne(_options.Timeout * 1000);

                        if (!completed)
                        {
                            powershell.Stop();
                            _logger.LogError($"PowerShell runspace timed out after {_options.Timeout} seconds", "ExecuteWithRunspace");
                            return -2;
                        }

                        var results = powershell.EndInvoke(asyncResult);

                        // Log output
                        if (_options.LogOutputStream)
                        {
                            foreach (var result in results)
                            {
                                if (result != null)
                                {
                                    _logger.LogInfo($"Output: {result}", "ExecuteWithRunspace");
                                }
                            }
                        }

                        // Log errors
                        if (powershell.HadErrors)
                        {
                            foreach (var error in powershell.Streams.Error)
                            {
                                _logger.LogError($"PowerShell Error: {error}", "ExecuteWithRunspace");
                            }
                            return 1;
                        }

                        // Log warnings
                        foreach (var warning in powershell.Streams.Warning)
                        {
                            _logger.LogWarning($"PowerShell Warning: {warning}", "ExecuteWithRunspace");
                        }

                        _logger.LogInfo("PowerShell script executed successfully via runspace", "ExecuteWithRunspace");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Runspace execution failed: {ex.Message}", "ExecuteWithRunspace", ex);
                return -1;
            }
        }

        private int ExecuteWithProcess()
        {
            _logger.LogInfo("Starting PowerShell process execution", "ExecuteWithProcess");

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe", // Use Windows PowerShell 5.1
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false
                };

                // Build command line arguments
                var args = new List<string>
                {
                    // Add default arguments
                    "-NoLogo",
                    "-NonInteractive"
                };

                if (_options.NoProfile)
                    args.Add("-NoProfile");                 

                if (!string.IsNullOrEmpty(_options.ExecutionPolicy))
                {
                    args.Add("-ExecutionPolicy");
                    args.Add(_options.ExecutionPolicy);
                }

                else if (_options.MTA)
                    args.Add("-MTA");

                // Add script or command
                if (!string.IsNullOrEmpty(_options.ScriptPath))
                {
                    if (!File.Exists(_options.ScriptPath))
                    {
                        _logger.LogError($"Script file not found: {_options.ScriptPath}", "ExecuteWithProcess");
                        return 1;
                    }

                    args.Add("-File");
                    args.Add(_options.ScriptPath);
                    args.AddRange(_options.ScriptArguments);
                }
                else if (!string.IsNullOrEmpty(_options.Command))
                {
                    args.Add("-Command");
                    args.Add(_options.Command);
                }
                else if (!string.IsNullOrEmpty(_options.EncodedCommand))
                {
                    args.Add("-EncodedCommand");
                    args.Add(_options.EncodedCommand);
                }

                processInfo.Arguments = string.Join(" ", args.Select(a => a.Contains(" ") ? $"\"{a}\"" : a));

                if (!string.IsNullOrEmpty(_options.WorkingDirectory))
                    processInfo.WorkingDirectory = _options.WorkingDirectory;

                _logger.LogInfo($"Starting PowerShell process: {processInfo.FileName} {processInfo.Arguments}", "ExecuteWithProcess");

                using (var process = new Process { StartInfo = processInfo })
                {
                    var errors = new StringBuilder();

                    if (_options.LogOutputStream)
                    {
                        var output = new StringBuilder();
                        process.OutputDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                output.AppendLine(e.Data);
                                _logger.LogInfo($"Output: {e.Data}", "ExecuteWithProcess");
                            }
                        };
                    }

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errors.AppendLine(e.Data);
                            _logger.LogError($"Error: {e.Data}", "ExecuteWithProcess");
                        }
                    };

                    process.Start();
                    if (_options.LogOutputStream)
                    {
                        process.BeginOutputReadLine();
                    }
                    process.BeginErrorReadLine();

                    // Wait for process completion with timeout
                    if (!process.WaitForExit(_options.Timeout * 1000))
                    {
                        _logger.LogError($"PowerShell process timed out after {_options.Timeout} seconds", "ExecuteWithProcess");
                        process.Kill();
                        return -2;
                    }

                    var exitCode = process.ExitCode;
                    _logger.LogInfo($"PowerShell process completed with exit code: {exitCode}", "ExecuteWithProcess");

                    return exitCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Process execution failed: {ex.Message}", "ExecuteWithProcess", ex);
                return -1;
            }
        }

        private ExecutionPolicy ParseExecutionPolicy(string policy)
        {
            if (Enum.TryParse<ExecutionPolicy>(policy, true, out var result))
            {
                return result;
            }

            _logger.LogWarning($"Invalid execution policy: {policy}, using default", "ParseExecutionPolicy");
            return ExecutionPolicy.Default;
        }
    }
}