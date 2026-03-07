using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Ink_Canvas.Services.Logging
{
    public sealed class FileAppLogger : IAppLogger
    {
        private sealed class SharedState
        {
            public SharedState(LogOptions options)
            {
                Options = options ?? throw new ArgumentNullException(nameof(options));
            }

            public object SyncRoot { get; } = new();

            public LogOptions Options { get; }
        }

        private readonly SharedState sharedState;
        private readonly string category;

        public FileAppLogger(LogOptions options)
            : this(new SharedState(options.Clone()), string.Empty)
        {
        }

        private FileAppLogger(SharedState sharedState, string category)
        {
            this.sharedState = sharedState ?? throw new ArgumentNullException(nameof(sharedState));
            this.category = category ?? string.Empty;
        }

        public IAppLogger ForCategory(string category)
        {
            string normalizedCategory = NormalizeCategory(category);
            if (string.IsNullOrWhiteSpace(normalizedCategory))
            {
                return this;
            }

            if (string.IsNullOrWhiteSpace(this.category))
            {
                return new FileAppLogger(sharedState, normalizedCategory);
            }

            return new FileAppLogger(sharedState, $"{this.category}/{normalizedCategory}");
        }

        public void Trace(string message) => Write(AppLogLevel.Trace, message, force: false);

        public void Info(string message) => Write(AppLogLevel.Info, message, force: false);

        public void Event(string message) => Write(AppLogLevel.Event, message, force: false);

        public void Error(string message, bool force = false) => Write(AppLogLevel.Error, message, force);

        public void Error(Exception exception, string? context = null, bool force = false)
        {
            if (exception == null)
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(context)
                ? exception.ToString()
                : $"{context} | {exception}";
            Write(AppLogLevel.Error, message, force);
        }

        public void SetEnabled(bool enabled)
        {
            lock (sharedState.SyncRoot)
            {
                sharedState.Options.Enabled = enabled;
            }
        }

        private void Write(AppLogLevel level, string? message, bool force)
        {
            if (!force && !IsEnabled())
            {
                return;
            }

            string line = FormatLine(level, message ?? string.Empty);
            try
            {
                lock (sharedState.SyncRoot)
                {
                    string directoryPath = sharedState.Options.DirectoryPath;
                    if (string.IsNullOrWhiteSpace(directoryPath))
                    {
                        return;
                    }

                    Directory.CreateDirectory(directoryPath);
                    string activeFilePath = Path.Combine(directoryPath, sharedState.Options.ActiveFileName);
                    RotateIfNeeded(activeFilePath, GetEncodedLineLength(line));

                    using StreamWriter writer = new(activeFilePath, append: true, new UTF8Encoding(false));
                    writer.WriteLine(line);
                }
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException)
            {
                Debug.WriteLine($"FileAppLogger write failure: {ex}");
            }
        }

        private bool IsEnabled()
        {
            lock (sharedState.SyncRoot)
            {
                return sharedState.Options.Enabled;
            }
        }

        private string FormatLine(AppLogLevel level, string message)
        {
            string normalizedMessage = NormalizeMessage(message);
            if (string.IsNullOrWhiteSpace(category))
            {
                return $"{DateTime.Now:O} [{GetLevelName(level)}] {normalizedMessage}";
            }

            return $"{DateTime.Now:O} [{GetLevelName(level)}] [{category}] {normalizedMessage}";
        }

        private void RotateIfNeeded(string activeFilePath, int incomingLineBytes)
        {
            FileInfo activeFile = new(activeFilePath);
            if (!activeFile.Exists || activeFile.Length <= 0)
            {
                return;
            }

            if (activeFile.Length + incomingLineBytes <= sharedState.Options.MaxFileSizeBytes)
            {
                return;
            }

            string directoryPath = activeFile.DirectoryName ?? sharedState.Options.DirectoryPath;
            string archiveName = $"Log.{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            string archivePath = Path.Combine(directoryPath, archiveName);
            if (File.Exists(archivePath))
            {
                archivePath = Path.Combine(directoryPath, $"Log.{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.txt");
            }

            File.Move(activeFilePath, archivePath);
            TrimArchives(directoryPath);
        }

        private void TrimArchives(string directoryPath)
        {
            int retainedArchiveCount = Math.Max(0, sharedState.Options.RetainedArchiveCount);
            string activeFilePath = Path.Combine(directoryPath, sharedState.Options.ActiveFileName);

            string[] archiveFiles = Directory.GetFiles(directoryPath, "Log.*.txt")
                .Where(path => !string.Equals(path, activeFilePath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(Path.GetFileName)
                .ToArray();

            for (int i = retainedArchiveCount; i < archiveFiles.Length; i++)
            {
                File.Delete(archiveFiles[i]);
            }
        }

        private static int GetEncodedLineLength(string line)
        {
            return Encoding.UTF8.GetByteCount(line + Environment.NewLine);
        }

        private static string GetLevelName(AppLogLevel level)
        {
            return level switch
            {
                AppLogLevel.Event => "Event",
                AppLogLevel.Trace => "Trace",
                AppLogLevel.Error => "Error",
                _ => "Info"
            };
        }

        private static string NormalizeCategory(string category)
        {
            return NormalizeMessage(category).Replace(" ", string.Empty);
        }

        private static string NormalizeMessage(string message)
        {
            return message
                .Replace("\r\n", " | ", StringComparison.Ordinal)
                .Replace("\n", " | ", StringComparison.Ordinal)
                .Replace("\r", " | ", StringComparison.Ordinal);
        }
    }
}
