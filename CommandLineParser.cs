using System;
using System.Collections.Generic;

namespace QuietShell
{
    public class RunnerOptions
    {
        public bool UseEmbeddedRunspace { get; set; } = true;
        public string ScriptPath { get; set; }
        public string[] ScriptArguments { get; set; } = new string[0];
        public string LogPath { get; set; }
        public bool NoProfile { get; set; }
        public string ExecutionPolicy { get; set; }
        public string Command { get; set; }
        public string EncodedCommand { get; set; }
        public string InputFormat { get; set; }
        public string OutputFormat { get; set; }
        public string ConfigurationName { get; set; }
        public string WorkingDirectory { get; set; }
        public bool MTA { get; set; }
        public int Timeout { get; set; } = 1800; // Default timeout in seconds
        public bool LogOutputStream { get; set; } = false; // Default to false for logging output stream
    }

    public static class CommandLineParser
    {
        public static RunnerOptions Parse(string[] args)
        {
            var options = new RunnerOptions();

            if (args == null || args.Length == 0)
            {
                return options;
            }

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    if (arg.StartsWith("-") || arg.StartsWith("/"))
                    {
                        var parameter = arg.Substring(1).ToLowerInvariant();

                        switch (parameter)
                        {
                            case "useprocess":
                                options.UseEmbeddedRunspace = false;
                                break;

                            case "userunspace":
                                options.UseEmbeddedRunspace = true;
                                break;

                            case "logpath":
                                if (i + 1 < args.Length)
                                {
                                    options.LogPath = args[++i];
                                }
                                break;

                            case "noprofile":
                                options.NoProfile = true;
                                break;

                            case "executionpolicy":
                            case "ep":
                                if (i + 1 < args.Length)
                                {
                                    options.ExecutionPolicy = args[++i];
                                }
                                break;

                            case "command":
                            case "c":
                                if (i + 1 < args.Length)
                                {
                                    options.Command = args[++i];
                                }
                                break;

                            case "encodedcommand":
                            case "e":
                            case "ec":
                                if (i + 1 < args.Length)
                                {
                                    options.EncodedCommand = args[++i];
                                }
                                break;

                            case "file":
                            case "f":
                                if (i + 1 < args.Length)
                                {
                                    options.ScriptPath = args[++i];
                                    var scriptArgs = new List<string>();
                                    // Collect only non-option arguments as script parameters
                                    while (i + 1 < args.Length && !(args[i + 1].StartsWith("-") || args[i + 1].StartsWith("/")))
                                    {
                                        scriptArgs.Add(args[++i]);
                                    }
                                    options.ScriptArguments = scriptArgs.ToArray();
                                }
                                break;

                            case "inputformat":
                            case "if":
                                if (i + 1 < args.Length)
                                {
                                    options.InputFormat = args[++i];
                                }
                                break;

                            case "outputformat":
                            case "of":
                                if (i + 1 < args.Length)
                                {
                                    options.OutputFormat = args[++i];
                                }
                                break;

                            case "configurationname":
                            case "config":
                                if (i + 1 < args.Length)
                                {
                                    options.ConfigurationName = args[++i];
                                }
                                break;

                            case "workingdirectory":
                            case "wd":
                                if (i + 1 < args.Length)
                                {
                                    options.WorkingDirectory = args[++i];
                                }
                                break;

                            case "mta":
                                options.MTA = true;
                                break;

                            case "timeout":
                                case "t":
                                if (i + 1 < args.Length && int.TryParse(args[++i], out int timeout))
                                {
                                    options.Timeout = timeout;
                                }
                                break;
                            
                            case "logoutputstream":
                                options.LogOutputStream = true;
                                break;
                        }
                    }
                    else
                    {
                        // If it's not a parameter and no script path is set, treat as script path
                        if (string.IsNullOrEmpty(options.ScriptPath))
                        {
                            options.ScriptPath = arg;
                            // Collect remaining arguments as script parameters
                            var scriptArgs = new List<string>();
                            for (int j = i + 1; j < args.Length; j++)
                            {
                                scriptArgs.Add(args[j]);
                            }
                            options.ScriptArguments = scriptArgs.ToArray();
                            break;
                        }
                    }
                }

                return options;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing command line: {ex.Message}");
                return options;
            }
        }
    }
}