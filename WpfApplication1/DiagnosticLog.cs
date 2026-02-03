using System;
using System.IO;
using System.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Simple diagnostic logging for culture/localization debugging.
    /// Logs to %USERPROFILE%\Documents\MySSKA\sska_diagnostic.log
    /// </summary>
    public static class DiagnosticLog
    {
        private static readonly object _lock = new object();
        private static string _logPath;

        private static string LogPath
        {
            get
            {
                if (_logPath == null)
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string sskaFolder = Path.Combine(documentsPath, "MySSKA");

                    // Ensure directory exists
                    if (!Directory.Exists(sskaFolder))
                    {
                        try { Directory.CreateDirectory(sskaFolder); } catch { }
                    }

                    _logPath = Path.Combine(sskaFolder, "sska_diagnostic.log");
                }
                return _logPath;
            }
        }

        /// <summary>
        /// Writes a log entry with timestamp and source.
        /// </summary>
        public static void Log(string source, string message)
        {
            try
            {
                lock (_lock)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                    string entry = $"[{timestamp}] [Thread:{threadId}] [{source}] {message}";

                    File.AppendAllText(LogPath, entry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently ignore logging failures
            }
        }

        /// <summary>
        /// Logs the start of a new session with separator.
        /// </summary>
        public static void LogSessionStart()
        {
            try
            {
                lock (_lock)
                {
                    string separator = new string('=', 80);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string entry = $"{Environment.NewLine}{separator}{Environment.NewLine}=== NEW SESSION: {timestamp} ==={Environment.NewLine}{separator}";

                    File.AppendAllText(LogPath, entry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently ignore logging failures
            }
        }

        /// <summary>
        /// Logs current culture state of the thread.
        /// </summary>
        public static void LogCultureState(string source)
        {
            try
            {
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                var currentUICulture = Thread.CurrentThread.CurrentUICulture;
                var resourceCulture = Local.Resource.Culture;

                Log(source, $"CurrentCulture: {currentCulture?.Name ?? "null"}");
                Log(source, $"CurrentUICulture: {currentUICulture?.Name ?? "null"}");
                Log(source, $"Resource.Culture: {resourceCulture?.Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Log(source, $"Error logging culture state: {ex.Message}");
            }
        }
    }
}
