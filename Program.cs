using System;

namespace QuietShell
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                var options = CommandLineParser.Parse(args);
                if (options == null)
                {
                    return 1; // Error in parsing
                }

                // Initialize logger
                var logger = new CMTraceLogger(options.LogPath, 2);
                logger.LogInfo("QuietShell desktop edition started", "Main");
                logger.LogInfo($"Command line: {string.Join(" ", args)}", "Main");

                // Create and run PowerShell runner
                var runner = new PowerShellRunner(logger, options);
                var result = runner.Execute();

                logger.LogInfo($"PowerShell Script Runner completed with exit code: {result}", "Main");
                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    var logger = new CMTraceLogger();
                    logger.LogError($"Unhandled exception: {ex.Message}", "Main", ex);
                }
                catch
                {
                    // If logging fails, write to console as last resort
                    Console.WriteLine($"Fatal error: {ex.Message}");
                }
                return -1;
            }
        }
    }
}