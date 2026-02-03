using System;
using System.IO;
using System.Text;
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
        // Maximum log size in bytes (1 MB)
        private const int MaxLogSizeBytes = 1 * 1024 * 1024;

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
                    string entry = $"[{timestamp}] [Thread:{threadId}] [{source}] {message}" + Environment.NewLine;

                    // Append with circular buffer enforcement
                    EnforceCircularLog(() => File.AppendAllText(LogPath, entry, Encoding.UTF8), Encoding.UTF8, entry.Length);
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
                    string entry = $"{Environment.NewLine}{separator}{Environment.NewLine}=== NEW SESSION: {timestamp} ==={Environment.NewLine}{separator}{Environment.NewLine}";

                    EnforceCircularLog(() => File.AppendAllText(LogPath, entry, Encoding.UTF8), Encoding.UTF8, entry.Length);
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

        // Ensures the log file does not grow beyond MaxLogSizeBytes using a simple circular buffer strategy.
        // The action parameter should append the new content to the file when invoked.
        private static void EnforceCircularLog(Action appendAction, Encoding encoding, int newContentLength)
        {
            try
            {
                // If file does not exist or is below threshold, just append.
                FileInfo fi = new FileInfo(LogPath);
                if (!fi.Exists)
                {
                    appendAction();
                    return;
                }

                long fileSize = fi.Length;
                if (fileSize + newContentLength <= MaxLogSizeBytes)
                {
                    appendAction();
                    return;
                }

                // Need to trim. Strategy: keep the last (MaxLogSizeBytes * 0.6) bytes and append new entry.
                int keepBytes = (int)(MaxLogSizeBytes * 0.6);
                if (keepBytes <= 0) keepBytes = MaxLogSizeBytes / 2;

                byte[] buffer = new byte[keepBytes];
                using (FileStream fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length <= keepBytes)
                    {
                        // File small enough, just append
                        fs.Close();
                        appendAction();
                        return;
                    }

                    // Seek to position to keep last keepBytes
                    fs.Seek(-keepBytes, SeekOrigin.End);
                    int read = 0;
                    int offset = 0;
                    while (offset < keepBytes && (read = fs.Read(buffer, offset, keepBytes - offset)) > 0)
                    {
                        offset += read;
                    }
                }

                // Overwrite file with kept tail and then append new entry
                using (FileStream fs = new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    fs.Write(buffer, 0, buffer.Length);
                }

                // Finally append new content
                appendAction();
            }
            catch
            {
                // best-effort - ignore logging failures
            }
        }
    }
}
