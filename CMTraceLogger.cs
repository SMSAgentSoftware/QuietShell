using System;
using System.IO;
using System.Text;
using System.Security.Principal;
using System.Threading;

namespace QuietShell
{
    public class CMTraceLogger
    {
        private readonly string _logFilePath;
        private readonly int _maxLogSizeMB;
        private readonly object _lockObject = new object();
        private static readonly string DefaultLogPath = Path.GetTempPath();

        public CMTraceLogger() : this(null, 2) { }

        public CMTraceLogger(string logPath, int maxLogSizeMB = 2)
        {
            _maxLogSizeMB = maxLogSizeMB;

            if (string.IsNullOrEmpty(logPath))
            {
                // Use temp directory by default
                Directory.CreateDirectory(DefaultLogPath);
                _logFilePath = Path.Combine(DefaultLogPath, $"QuietShell.log");
            }
            else
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                _logFilePath = logPath;
            }
        }

        public void LogInfo(string message, string component = "PowerShellRunner")
        {
            WriteLogEntry(message, component, 1);
        }

        public void LogWarning(string message, string component = "PowerShellRunner")
        {
            WriteLogEntry(message, component, 2);
        }

        public void LogError(string message, string component = "PowerShellRunner", Exception exception = null)
        {
            var fullMessage = exception != null ? $"{message}: {exception}" : message;
            WriteLogEntry(fullMessage, component, 3);
        }

        private void WriteLogEntry(string message, string component, int type)
        {
            lock (_lockObject)
            {
                try
                {
                    // Check file size and rollover if necessary
                    CheckAndRolloverLog();

                    var logEntry = CreateCMTraceLogEntry(message, component, type);

                    // Use UTF-8 encoding for compatibility
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Last resort - write to console if logging fails
                    Console.WriteLine($"Logging failed: {ex.Message}");
                }
            }
        }

        private string CreateCMTraceLogEntry(string message, string component, int type)
        {
            var now = DateTime.Now;
            var time = now.ToString("HH:mm:ss.ffffff");
            var date = now.ToString("M-d-yyyy");
            var context = WindowsIdentity.GetCurrent()?.Name ?? "Unknown";
            var thread = Thread.CurrentThread.ManagedThreadId.ToString();

            // Escape special characters in message
            var escapedMessage = EscapeXmlCharacters(message);

            // CMTrace format: <![LOG[message]LOG]!><time="HH:mm:ss.ffffff" date="M-d-yyyy" component="component" context="context" type="type" thread="thread" file="">
            return $"<![LOG[{escapedMessage}]LOG]!>" +
                   $"<time=\"{time}\" " +
                   $"date=\"{date}\" " +
                   $"component=\"{component}\" " +
                   $"context=\"{context}\" " +
                   $"type=\"{type}\" " +
                   $"thread=\"{thread}\" " +
                   $"file=\"\">";
        }

        private string EscapeXmlCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private void CheckAndRolloverLog()
        {
            if (!File.Exists(_logFilePath))
                return;

            var fileInfo = new FileInfo(_logFilePath);
            var maxSizeBytes = _maxLogSizeMB * 1024 * 1024;

            if (fileInfo.Length >= maxSizeBytes)
            {
                RolloverLog();
            }
        }

        private void RolloverLog()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                var fileName = Path.GetFileNameWithoutExtension(_logFilePath);
                var extension = Path.GetExtension(_logFilePath);

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");

                // Move current log to backup
                if (File.Exists(_logFilePath))
                {
                    File.Move(_logFilePath, backupPath);
                }

                // Clean up old log files (keep only last 10)
                CleanupOldLogs(directory, fileName, extension);
            }
            catch (Exception ex)
            {
                // If rollover fails, try to continue logging
                Console.WriteLine($"Log rollover failed: {ex.Message}");
            }
        }

        private void CleanupOldLogs(string directory, string fileName, string extension)
        {
            try
            {
                var pattern = $"{fileName}_*{extension}";
                var oldLogs = Directory.GetFiles(directory, pattern);

                if (oldLogs.Length > 10)
                {
                    // Sort by creation time and delete oldest
                    Array.Sort(oldLogs, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));

                    for (int i = 0; i < oldLogs.Length - 10; i++)
                    {
                        try
                        {
                            File.Delete(oldLogs[i]);
                        }
                        catch
                        {
                            // Ignore individual file deletion errors
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}