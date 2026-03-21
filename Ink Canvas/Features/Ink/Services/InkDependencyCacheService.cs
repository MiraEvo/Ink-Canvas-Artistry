using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Features.Ink.Services
{
    internal sealed class InkDependencyCacheService
    {
        private const string SessionDependencyFolderName = "Session Dependency";
        private const string SessionMetadataFileName = "session.json";
        private const string ImportedArchivesFolderName = "Imported";

        private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        private readonly string appSessionId = Guid.NewGuid().ToString("N");
        private readonly int processId = Environment.ProcessId;
        private readonly DateTime startedAtUtc = DateTime.UtcNow;
        private readonly HashSet<string> cleanedRootDirectories = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> activeSessionDirectories = new(StringComparer.OrdinalIgnoreCase);
        private readonly object syncRoot = new();
        private readonly IAppLogger logger;

        private string? currentRootDirectory;

        public InkDependencyCacheService(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkDependencyCacheService));
        }

        public string AppSessionId => appSessionId;

        public void InitializeSession(string rootDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

            string normalizedRoot = Path.GetFullPath(rootDirectory);
            string sessionRoot = PathSafetyHelper.ResolveRelativePath(normalizedRoot, SessionDependencyFolderName);
            string sessionDirectory = GetSessionDirectory(normalizedRoot);

            Directory.CreateDirectory(sessionRoot);
            if (ShouldCleanupRoot(normalizedRoot))
            {
                CleanupStaleSessions(sessionRoot);
            }

            Directory.CreateDirectory(sessionDirectory);
            WriteSessionMetadata(sessionDirectory, normalizedRoot);

            lock (syncRoot)
            {
                currentRootDirectory = normalizedRoot;
                activeSessionDirectories.Add(sessionDirectory);
            }
        }

        public async Task<string> GetOrCreateDependencyFileAsync(string sourceFilePath, string fallbackFileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackFileName);

            string currentRoot = GetCurrentRootDirectory();
            return await Task.Run(() => GetOrCreateDependencyFileCore(currentRoot, sourceFilePath, fallbackFileName));
        }

        public string GetImportedArchiveDirectory(string archiveDisplayName, string archiveHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(archiveHash);

            string safeBaseName = PathSafetyHelper.NormalizeLeafName(
                Path.GetFileNameWithoutExtension(archiveDisplayName),
                "archive");
            string normalizedHash = archiveHash.Trim().ToLowerInvariant();
            if (normalizedHash.Length < 16)
            {
                throw new ArgumentException("Archive hash must contain at least 16 hexadecimal characters.", nameof(archiveHash));
            }

            string sessionDirectory = GetCurrentSessionDirectory();
            string archiveId = PathSafetyHelper.NormalizeLeafName(
                $"{safeBaseName}_{normalizedHash[..16]}",
                "archive_import");
            string importedDirectory = PathSafetyHelper.ResolveRelativePath(sessionDirectory, ImportedArchivesFolderName, archiveId);
            Directory.CreateDirectory(importedDirectory);
            return importedDirectory;
        }

        public void SwitchSessionRoot(string? previousRoot, string currentRoot, IReadOnlyList<UIElement> referencedElements)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentRoot);
            ArgumentNullException.ThrowIfNull(referencedElements);

            InitializeSession(currentRoot);
            if (string.IsNullOrWhiteSpace(previousRoot))
            {
                return;
            }

            string normalizedPreviousRoot = Path.GetFullPath(previousRoot);
            string normalizedCurrentRoot = Path.GetFullPath(currentRoot);
            if (string.Equals(normalizedPreviousRoot, normalizedCurrentRoot, PathComparison))
            {
                return;
            }

            string previousSessionDirectory = GetSessionDirectory(normalizedPreviousRoot);
            if (!Directory.Exists(previousSessionDirectory))
            {
                return;
            }

            List<ElementDependencyReference> references = CollectMigratableElements(referencedElements, previousSessionDirectory);
            if (references.Count == 0)
            {
                RemoveSessionDirectory(previousSessionDirectory);
                return;
            }

            Dictionary<string, string> migratedPathMap = new(StringComparer.OrdinalIgnoreCase);
            foreach (string sourcePath in references
                .Select(reference => reference.SourcePath)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    string migratedPath = GetOrCreateDependencyFileCore(
                        normalizedCurrentRoot,
                        sourcePath,
                        Path.GetFileName(sourcePath));
                    migratedPathMap[sourcePath] = migratedPath;
                }
                catch (Exception ex) when (IsRecoverableMigrationException(ex))
                {
                    logger.Error(ex, $"Dependency Cache | Failed to migrate dependency '{sourcePath}' to '{normalizedCurrentRoot}'");
                    return;
                }
            }

            List<PreparedElementUpdate> preparedUpdates = [];
            try
            {
                foreach (ElementDependencyReference reference in references)
                {
                    preparedUpdates.Add(PrepareElementUpdate(reference, migratedPathMap[reference.SourcePath]));
                }
            }
            catch (Exception ex) when (IsRecoverableMigrationException(ex))
            {
                logger.Error(ex, $"Dependency Cache | Failed to prepare migrated element sources for root '{normalizedCurrentRoot}'");
                return;
            }

            foreach (PreparedElementUpdate preparedUpdate in preparedUpdates)
            {
                preparedUpdate.Apply();
            }

            RemoveSessionDirectory(previousSessionDirectory);
        }

        public void CleanupCurrentSessions()
        {
            List<string> sessionDirectories;
            lock (syncRoot)
            {
                sessionDirectories = [.. activeSessionDirectories];
            }

            foreach (string sessionDirectory in sessionDirectories)
            {
                RemoveSessionDirectory(sessionDirectory);
            }
        }

        private bool ShouldCleanupRoot(string normalizedRoot)
        {
            lock (syncRoot)
            {
                return cleanedRootDirectories.Add(normalizedRoot);
            }
        }

        private string GetCurrentRootDirectory()
        {
            lock (syncRoot)
            {
                if (string.IsNullOrWhiteSpace(currentRootDirectory))
                {
                    throw new InvalidOperationException("Dependency cache session has not been initialized.");
                }

                return currentRootDirectory;
            }
        }

        private string GetCurrentSessionDirectory()
        {
            return GetSessionDirectory(GetCurrentRootDirectory());
        }

        private string GetSessionDirectory(string rootDirectory)
        {
            string normalizedRoot = Path.GetFullPath(rootDirectory);
            return PathSafetyHelper.ResolveRelativePath(normalizedRoot, SessionDependencyFolderName, appSessionId);
        }

        private List<ElementDependencyReference> CollectMigratableElements(IReadOnlyList<UIElement> referencedElements, string previousSessionDirectory)
        {
            List<ElementDependencyReference> references = [];
            string normalizedPreviousSessionDirectory = PathSafetyHelper.AppendDirectorySeparator(Path.GetFullPath(previousSessionDirectory));

            foreach (UIElement element in referencedElements)
            {
                if (!TryGetElementSourcePath(element, out string? sourcePath) || string.IsNullOrWhiteSpace(sourcePath))
                {
                    continue;
                }

                string normalizedSourcePath = Path.GetFullPath(sourcePath);
                if (!normalizedSourcePath.StartsWith(normalizedPreviousSessionDirectory, PathComparison))
                {
                    continue;
                }

                references.Add(new ElementDependencyReference(element, normalizedSourcePath));
            }

            return references;
        }

        private PreparedElementUpdate PrepareElementUpdate(ElementDependencyReference reference, string migratedPath)
        {
            return reference.Element switch
            {
                Image image => PrepareImageUpdate(image, migratedPath),
                MediaElement mediaElement => PrepareMediaElementUpdate(mediaElement, migratedPath),
                _ => throw new InvalidOperationException($"Unsupported element type '{reference.Element.GetType().Name}' during dependency migration.")
            };
        }

        private PreparedElementUpdate PrepareImageUpdate(Image image, string migratedPath)
        {
            ImageSource? preparedSource = image.Source switch
            {
                BitmapImage => CreateBitmapImage(migratedPath),
                TransformedBitmap transformedBitmap => new TransformedBitmap(CreateBitmapImage(migratedPath), transformedBitmap.Transform),
                _ => throw new InvalidOperationException($"Unsupported image source type '{image.Source?.GetType().Name ?? "null"}' during dependency migration.")
            };

            return new PreparedElementUpdate(() => image.Source = preparedSource);
        }

        private static PreparedElementUpdate PrepareMediaElementUpdate(MediaElement mediaElement, string migratedPath)
        {
            Uri migratedUri = new(migratedPath);
            return new PreparedElementUpdate(() => mediaElement.Source = migratedUri);
        }

        private static BitmapImage CreateBitmapImage(string filePath)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private static bool TryGetElementSourcePath(UIElement element, out string? sourcePath)
        {
            switch (element)
            {
                case Image image:
                    return TryGetImageSourcePath(image.Source, out sourcePath);
                case MediaElement mediaElement when mediaElement.Source?.IsFile is true:
                    sourcePath = mediaElement.Source.LocalPath;
                    return !string.IsNullOrWhiteSpace(sourcePath);
                default:
                    sourcePath = null;
                    return false;
            }
        }

        private static bool TryGetImageSourcePath(ImageSource? imageSource, out string? sourcePath)
        {
            switch (imageSource)
            {
                case BitmapImage bitmapImage when bitmapImage.UriSource?.IsFile is true:
                    sourcePath = bitmapImage.UriSource.LocalPath;
                    return !string.IsNullOrWhiteSpace(sourcePath);
                case TransformedBitmap transformedBitmap:
                    return TryGetImageSourcePath(transformedBitmap.Source, out sourcePath);
                default:
                    sourcePath = null;
                    return false;
            }
        }

        private string GetOrCreateDependencyFileCore(string rootDirectory, string sourceFilePath, string fallbackFileName)
        {
            string sessionDirectory = GetSessionDirectory(rootDirectory);
            string fileExtension = NormalizeExtension(Path.GetExtension(sourceFilePath), fallbackFileName);
            string fileHash = ComputeFileHash(sourceFilePath);
            string cachedFileName = PathSafetyHelper.NormalizeLeafName(fileHash + fileExtension, fallbackFileName);
            string cachedFilePath = PathSafetyHelper.ResolveRelativePath(sessionDirectory, cachedFileName);

            if (!File.Exists(cachedFilePath))
            {
                Directory.CreateDirectory(sessionDirectory);
                File.Copy(sourceFilePath, cachedFilePath, overwrite: false);
            }

            return cachedFilePath;
        }

        private void CleanupStaleSessions(string sessionRoot)
        {
            foreach (string sessionDirectory in Directory.GetDirectories(sessionRoot))
            {
                if (string.Equals(Path.GetFileName(sessionDirectory), appSessionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string sessionMetadataPath = PathSafetyHelper.ResolveRelativePath(sessionDirectory, SessionMetadataFileName);
                if (!File.Exists(sessionMetadataPath))
                {
                    RemoveSessionDirectory(sessionDirectory);
                    continue;
                }

                SessionMetadata? metadata;
                try
                {
                    metadata = JsonSerializer.Deserialize<SessionMetadata>(File.ReadAllText(sessionMetadataPath));
                }
                catch (IOException ex)
                {
                    logger.Error(ex, $"Dependency Cache | Failed to read session metadata '{sessionMetadataPath}'");
                    continue;
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.Error(ex, $"Dependency Cache | Failed to read session metadata '{sessionMetadataPath}'");
                    continue;
                }
                catch (JsonException ex)
                {
                    logger.Error(ex, $"Dependency Cache | Invalid session metadata '{sessionMetadataPath}'");
                    RemoveSessionDirectory(sessionDirectory);
                    continue;
                }

                if (metadata == null
                    || metadata.ProcessId <= 0
                    || string.IsNullOrWhiteSpace(metadata.SessionId)
                    || string.IsNullOrWhiteSpace(metadata.RootDirectory))
                {
                    RemoveSessionDirectory(sessionDirectory);
                    continue;
                }

                if (!IsProcessAlive(metadata.ProcessId))
                {
                    RemoveSessionDirectory(sessionDirectory);
                }
            }
        }

        private void WriteSessionMetadata(string sessionDirectory, string rootDirectory)
        {
            string metadataPath = PathSafetyHelper.ResolveRelativePath(sessionDirectory, SessionMetadataFileName);
            SessionMetadata metadata = new(appSessionId, processId, startedAtUtc.ToString("O"), rootDirectory);
            string payload = JsonSerializer.Serialize(metadata);
            File.WriteAllText(metadataPath, payload);
        }

        private void RemoveSessionDirectory(string sessionDirectory)
        {
            TryDeleteDirectory(sessionDirectory, "Dependency Cache | Failed to delete session directory");
            lock (syncRoot)
            {
                activeSessionDirectories.Remove(sessionDirectory);
            }
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string NormalizeExtension(string? extension, string fallbackFileName)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                string fallbackExtension = Path.GetExtension(fallbackFileName);
                return string.IsNullOrWhiteSpace(fallbackExtension) ? ".bin" : fallbackExtension;
            }

            string normalizedExtension = Path.GetFileName(extension);
            if (string.IsNullOrWhiteSpace(normalizedExtension) || normalizedExtension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return ".bin";
            }

            return normalizedExtension.StartsWith('.') ? normalizedExtension : "." + normalizedExtension;
        }

        private static string ComputeFileHash(string filePath)
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] hash = SHA256.HashData(fileStream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private void TryDeleteDirectory(string directoryPath, string context)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                    logger.Trace($"{context}: {directoryPath}");
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, $"{context}: {directoryPath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"{context}: {directoryPath}");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, $"{context}: {directoryPath}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, $"{context}: {directoryPath}");
            }
        }

        private static bool IsRecoverableMigrationException(Exception exception)
        {
            return exception is ArgumentException
                or DirectoryNotFoundException
                or FileNotFoundException
                or InvalidOperationException
                or IOException
                or NotSupportedException
                or UnauthorizedAccessException
                or UriFormatException;
        }

        private sealed record SessionMetadata(string SessionId, int ProcessId, string StartedAtUtc, string RootDirectory);

        private sealed record ElementDependencyReference(UIElement Element, string SourcePath);

        private sealed record PreparedElementUpdate(Action Apply);
    }
}
