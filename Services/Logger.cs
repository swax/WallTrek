using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace WallTrek.Services
{
    /// <summary>
    /// Lightweight file logger that appends timestamped entries to
    /// <c>%APPDATA%\WallTrek\walltrek.log</c> (next to settings.json and the database).
    /// Logging never throws — a failure to log must not break the operation being logged.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private const long MaxBytes = 512 * 1024; // roll over once the log passes ~512 KB

        /// <summary>Full path to the active log file.</summary>
        public static string LogFilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WallTrek",
            "walltrek.log");

        public static void Info(string message, [CallerMemberName] string? source = null)
            => Write("INFO", source, message, null);

        public static void Warn(string message, [CallerMemberName] string? source = null)
            => Write("WARN", source, message, null);

        /// <summary>
        /// Logs an error. When <paramref name="ex"/> is supplied the full exception
        /// (type, message, stack trace and any inner exceptions) is written — this is the
        /// detail that's useful for diagnosing provider/API failures.
        /// </summary>
        public static void Error(string message, Exception? ex = null, [CallerMemberName] string? source = null)
            => Write("ERROR", source, message, ex);

        private static void Write(string level, string? source, string message, Exception? ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                  .Append(" [").Append(level).Append("] ")
                  .Append(source).Append(" - ").Append(message);

                if (ex is not null)
                    sb.Append(Environment.NewLine).Append(ex);

                var line = sb.Append(Environment.NewLine).ToString();

                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                    RotateIfNeeded();
                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
                // Intentionally swallowed — logging must never surface its own failures.
            }
        }

        // Caller already holds _lock.
        private static void RotateIfNeeded()
        {
            try
            {
                var info = new FileInfo(LogFilePath);
                if (!info.Exists || info.Length <= MaxBytes)
                    return;

                var archive = LogFilePath + ".1";
                if (File.Exists(archive))
                    File.Delete(archive);
                File.Move(LogFilePath, archive);
            }
            catch
            {
                // If rotation fails, keep appending to the existing file.
            }
        }
    }
}
