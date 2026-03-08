using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ink_Canvas.Services.Logging;

namespace Ink_Canvas.Features.Automation.Services
{
    internal sealed class AutoUpdateHelper
    {
        private const string UpdateServerBaseUrl = "http://8.134.100.248:8080";

        private static readonly Regex VersionPattern = new(@"^\d+(\.\d+){1,3}$", RegexOptions.Compiled);
        private static readonly HttpClient HttpClient = new();
        private static readonly TimeSpan VersionRequestTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan DownloadRequestTimeout = TimeSpan.FromMinutes(5);
        private static readonly string updatesFolderPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Ink Canvas Artistry",
            "AutoUpdate");
        private readonly IAppLogger logger;

        public AutoUpdateHelper(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(AutoUpdateHelper));
        }

        public async Task<string?> CheckForUpdates()
        {
            try
            {
                string localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
                string? remoteVersion = ValidateVersionOrNull(
                    await GetRemoteVersion($"{UpdateServerBaseUrl}/version").ConfigureAwait(false));

                if (remoteVersion is null)
                {
                    logger.Error("AutoUpdate | Failed to retrieve remote version.");
                    return null;
                }

                Version local = new(localVersion);
                Version remote = new(remoteVersion);
                if (remote > local)
                {
                    logger.Info($"AutoUpdate | New version Available: {remoteVersion}");
                    return remoteVersion;
                }

                logger.Info("AutoUpdate | Local version is up-to-date or newer.");
                return null;
            }
            catch (FormatException ex)
            {
                logger.Error($"AutoUpdate | Version format error: {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                logger.Error($"AutoUpdate | Error checking for updates: {ex.Message}");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error checking for updates: {ex.Message}");
                return null;
            }
        }

        public Task<string?> GetRemoteVersion(string fileUrl) =>
            TryGetStringAsync(fileUrl, VersionRequestTimeout, "getting version");

        public async Task<bool> DownloadSetupFileAndSaveStatus(string version)
        {
            string? validatedVersion = null;
            string? statusFilePath = null;

            try
            {
                validatedVersion = ValidateVersionOrThrow(version);
                statusFilePath = GetStatusFilePath(validatedVersion);

                if (IsSetupFileMarkedAsDownloaded(statusFilePath))
                {
                    logger.Info("AutoUpdate | Setup file already downloaded.");
                    return true;
                }

                string setupFileName = GetSetupFileName(validatedVersion);
                string destinationPath = ResolvePathWithinUpdatesFolder(setupFileName);
                string downloadUrl = $"{UpdateServerBaseUrl}/download/{setupFileName}";

                logger.Info($"AutoUpdate | Attempting download from: {downloadUrl} to {destinationPath}");

                SaveDownloadStatus(statusFilePath, false);
                bool isDownloaded = await TryDownloadFile(downloadUrl, destinationPath).ConfigureAwait(false);
                SaveDownloadStatus(statusFilePath, isDownloaded);

                if (!isDownloaded)
                {
                    CleanupIncompleteDownload(validatedVersion);
                    return false;
                }

                logger.Info("AutoUpdate | Setup file successfully downloaded.");
                return true;
            }
            catch (ArgumentException ex)
            {
                HandleDownloadFailure(ex, version, validatedVersion, statusFilePath);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                HandleDownloadFailure(ex, version, validatedVersion, statusFilePath);
                return false;
            }
            catch (IOException ex)
            {
                HandleDownloadFailure(ex, version, validatedVersion, statusFilePath);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                HandleDownloadFailure(ex, version, validatedVersion, statusFilePath);
                return false;
            }
        }

        public void InstallNewVersionApp(string version, bool isInSilence)
        {
            try
            {
                string validatedVersion = ValidateVersionOrThrow(version);
                string setupFilePath = ResolvePathWithinUpdatesFolder(GetSetupFileName(validatedVersion));

                if (!File.Exists(setupFilePath))
                {
                    logger.Error($"AutoUpdate | Setup file not found: {setupFilePath}");
                    return;
                }

                logger.Info($"AutoUpdate | Starting installer: {setupFilePath}");
                StartInstaller(setupFilePath, isInSilence);
            }
            catch (ArgumentException ex)
            {
                logger.Error($"AutoUpdate | Error installing update: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error installing update: {ex.Message}");
            }
            catch (IOException ex)
            {
                logger.Error($"AutoUpdate | Error installing update: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"AutoUpdate | Error installing update: {ex.Message}");
            }
        }

        public void DeleteUpdatesFolder()
        {
            try
            {
                if (Directory.Exists(updatesFolderPath))
                {
                    Directory.Delete(updatesFolderPath, true);
                    logger.Info($"AutoUpdate | Deleted updates folder: {updatesFolderPath}");
                }
            }
            catch (IOException ex)
            {
                logger.Error($"AutoUpdate clearing| Error deleting updates folder: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"AutoUpdate clearing| Error deleting updates folder: {ex.Message}");
            }
        }

        private async Task<string?> TryGetStringAsync(string fileUrl, TimeSpan timeout, string operationName)
        {
            using CancellationTokenSource timeoutCts = new(timeout);

            try
            {
                using HttpResponseMessage response = await HttpClient.GetAsync(fileUrl, timeoutCts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return (await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false)).Trim();
            }
            catch (HttpRequestException ex)
            {
                logger.Error($"AutoUpdate | HTTP request error {operationName} from {fileUrl}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                logger.Error($"AutoUpdate | Timeout {operationName} from {fileUrl}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error {operationName} from {fileUrl}: {ex.Message}");
            }

            return null;
        }

        private async Task<bool> TryDownloadFile(string fileUrl, string destinationPath)
        {
            using CancellationTokenSource timeoutCts = new(DownloadRequestTimeout);

            try
            {
                EnsureDirectoryExists(destinationPath);

                using HttpResponseMessage response = await HttpClient.GetAsync(
                    fileUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using FileStream fileStream = File.Create(destinationPath);
                await response.Content.CopyToAsync(fileStream, timeoutCts.Token).ConfigureAwait(false);

                logger.Info($"AutoUpdate | File downloaded successfully to {destinationPath}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                logger.Error($"AutoUpdate | HTTP request error downloading from {fileUrl}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                logger.Error($"AutoUpdate | Timeout downloading from {fileUrl}: {ex.Message}");
            }
            catch (IOException ex)
            {
                logger.Error($"AutoUpdate | IO error saving to {destinationPath}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"AutoUpdate | Access denied saving to {destinationPath}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Invalid operation downloading from {fileUrl}: {ex.Message}");
            }

            return false;
        }

        private void HandleDownloadFailure(Exception ex, string version, string? validatedVersion, string? statusFilePath)
        {
            logger.Error($"AutoUpdate | Error downloading setup file for version {version}: {ex.Message}");

            if (!string.IsNullOrWhiteSpace(statusFilePath))
            {
                SaveDownloadStatus(statusFilePath, false);
            }

            if (!string.IsNullOrWhiteSpace(validatedVersion))
            {
                CleanupIncompleteDownload(validatedVersion);
            }
        }

        private static bool IsSetupFileMarkedAsDownloaded(string statusFilePath)
        {
            return File.Exists(statusFilePath)
                && bool.TryParse(File.ReadAllText(statusFilePath).Trim(), out bool isDownloaded)
                && isDownloaded;
        }

        private void SaveDownloadStatus(string statusFilePath, bool isSuccess)
        {
            try
            {
                EnsureDirectoryExists(statusFilePath);
                File.WriteAllText(statusFilePath, isSuccess.ToString());
                logger.Info($"AutoUpdate | Saved download status ({isSuccess}) to {statusFilePath}");
            }
            catch (IOException ex)
            {
                logger.Error($"AutoUpdate | Error saving download status: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"AutoUpdate | Error saving download status: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error saving download status: {ex.Message}");
            }
        }

        private void StartInstaller(string setupFilePath, bool isInSilence)
        {
            string arguments = isInSilence ? "/SILENT /VERYSILENT" : "/SILENT";
            ProcessStartInfo processStartInfo = new()
            {
                FileName = setupFilePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = GetRequiredDirectoryPath(setupFilePath)
            };

            try
            {
                using Process process = new() { StartInfo = processStartInfo };
                process.Start();
                logger.Info($"AutoUpdate | Started installer with arguments: {arguments}");
                ShutdownCurrentApplication();
            }
            catch (Win32Exception ex)
            {
                logger.Error($"AutoUpdate | Error starting installer '{setupFilePath}': {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error starting installer '{setupFilePath}': {ex.Message}");
            }
        }

        private void ShutdownCurrentApplication()
        {
            Application? application = Application.Current;
            if (application?.Dispatcher is not { } dispatcher)
            {
                return;
            }

            try
            {
                if (dispatcher.CheckAccess())
                {
                    logger.Info("AutoUpdate | Shutting down application for update.");
                    application.Shutdown();
                    return;
                }

                dispatcher.Invoke(() =>
                {
                    logger.Info("AutoUpdate | Shutting down application for update.");
                    application.Shutdown();
                });
            }
            catch (TaskCanceledException ex)
            {
                logger.Error($"AutoUpdate | Error shutting down application for update: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error shutting down application for update: {ex.Message}");
            }
        }

        private static string GetStatusFilePath(string version) =>
            ResolvePathWithinUpdatesFolder($"DownloadV{version}Status.txt");

        private static string GetSetupFileName(string version) =>
            $"Ink.Canvas.Artistry.V{ValidateVersionOrThrow(version)}.Setup.exe";

        private static string ValidateVersionOrThrow(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Version cannot be empty.", nameof(version));
            }

            string trimmedVersion = version.Trim();
            if (!VersionPattern.IsMatch(trimmedVersion))
            {
                throw new ArgumentException($"Unsupported version format '{version}'.", nameof(version));
            }

            _ = new Version(trimmedVersion);
            return trimmedVersion;
        }

        private string? ValidateVersionOrNull(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            try
            {
                return ValidateVersionOrThrow(version);
            }
            catch (ArgumentException ex)
            {
                logger.Error($"AutoUpdate | Invalid remote version '{version}': {ex.Message}");
                return null;
            }
            catch (FormatException ex)
            {
                logger.Error($"AutoUpdate | Invalid remote version '{version}': {ex.Message}");
                return null;
            }
            catch (OverflowException ex)
            {
                logger.Error($"AutoUpdate | Invalid remote version '{version}': {ex.Message}");
                return null;
            }
        }

        private void CleanupIncompleteDownload(string validatedVersion)
        {
            try
            {
                string destinationPath = ResolvePathWithinUpdatesFolder(GetSetupFileName(validatedVersion));
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
            }
            catch (IOException ex)
            {
                logger.Error($"AutoUpdate | Error deleting incomplete download: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"AutoUpdate | Error deleting incomplete download: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                logger.Error($"AutoUpdate | Error deleting incomplete download: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error($"AutoUpdate | Error deleting incomplete download: {ex.Message}");
            }
        }

        private static string ResolvePathWithinUpdatesFolder(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            if (Path.IsPathRooted(fileName))
            {
                throw new InvalidOperationException("Update file name must be a relative path.");
            }

            string relativePath = fileName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relativePath.Length != fileName.Length)
            {
                throw new InvalidOperationException("Update file name cannot start with a directory separator.");
            }

            string rootPath = AppendDirectorySeparator(Path.GetFullPath(updatesFolderPath));
            string fullPath = Path.GetFullPath(Path.Join(rootPath, relativePath));

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resolved update path escaped the updates directory.");
            }

            return fullPath;
        }

        private void EnsureDirectoryExists(string filePath)
        {
            string directory = GetRequiredDirectoryPath(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                logger.Info($"AutoUpdate | Created directory: {directory}");
            }
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string GetRequiredDirectoryPath(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Path '{filePath}' does not contain a valid directory.");
            }

            return directory;
        }
    }

    internal static class AutoUpdateWithSilenceTimeComboBox
    {
        public static ObservableCollection<string> Hours { get; } = [];
        public static ObservableCollection<string> Minutes { get; } = [];

        private static string[]? timeOptions;

        public static void InitializeAutoUpdateWithSilenceTimeComboBoxOptions(ComboBox startTimeComboBox, ComboBox endTimeComboBox)
        {
            ArgumentNullException.ThrowIfNull(startTimeComboBox);
            ArgumentNullException.ThrowIfNull(endTimeComboBox);

            EnsureTimeOptionsInitialized();
            startTimeComboBox.ItemsSource = timeOptions;
            endTimeComboBox.ItemsSource = timeOptions;
        }

        public static bool CheckIsInSilencePeriod(string startTime, string endTime)
        {
            if (string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
            {
                return false;
            }

            if (startTime == endTime)
            {
                return true;
            }

            if (!TryParseTime(startTime, out TimeSpan startTimeOfDay)
                || !TryParseTime(endTime, out TimeSpan endTimeOfDay))
            {
                Debug.WriteLine($"AutoUpdate | Invalid time format for silence period: Start='{startTime}', End='{endTime}'");
                return false;
            }

            TimeSpan currentTimeOfDay = DateTime.Now.TimeOfDay;
            return startTimeOfDay <= endTimeOfDay
                ? currentTimeOfDay >= startTimeOfDay && currentTimeOfDay < endTimeOfDay
                : currentTimeOfDay >= startTimeOfDay || currentTimeOfDay < endTimeOfDay;
        }

        private static void EnsureTimeOptionsInitialized()
        {
            if (timeOptions is not null)
            {
                return;
            }

            PopulateTimeParts();
            timeOptions = [.. Hours.SelectMany(hour => Minutes.Select(minute => $"{hour}:{minute}"))];
        }

        private static void PopulateTimeParts()
        {
            if (Hours.Count > 0 || Minutes.Count > 0)
            {
                return;
            }

            for (int hour = 0; hour <= 23; hour++)
            {
                Hours.Add(hour.ToString("00", CultureInfo.InvariantCulture));
            }

            for (int minute = 0; minute <= 59; minute += 20)
            {
                Minutes.Add(minute.ToString("00", CultureInfo.InvariantCulture));
            }
        }

        private static bool TryParseTime(string value, out TimeSpan time)
        {
            if (DateTime.TryParseExact(
                    value,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedTime))
            {
                time = parsedTime.TimeOfDay;
                return true;
            }

            time = default;
            return false;
        }
    }
}

