using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Xml;

namespace Ink_Canvas.Features.Ink.Services
{
    internal sealed class InkArchiveService
    {
        private const string ManifestEntryName = "manifest.json";
        private const string StrokesEntryName = "strokes.icstk";
        private const string ElementsEntryName = "elements.xaml";
        private const string ArchiveFormatName = "icart";
        private const int CurrentArchiveVersion = 3;

        private static readonly byte[] ZipMagicHeader = [0x50, 0x4B, 0x03, 0x04];

        private readonly IAppLogger logger;
        private readonly InkCanvasArchiveElementsSerializer elementsSerializer;
        private readonly InkDependencyCacheService dependencyCacheService;

        public InkArchiveService(IAppLogger logger, InkDependencyCacheService dependencyCacheService)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkArchiveService));
            this.dependencyCacheService = dependencyCacheService ?? throw new ArgumentNullException(nameof(dependencyCacheService));
            elementsSerializer = new InkCanvasArchiveElementsSerializer(this.logger);
        }

        public string BuildArchiveDirectory(string rootDirectory, bool saveByUser, bool isDesktopAnnotationMode)
        {
            string directoryName = (saveByUser ? "User Saved - " : "Auto Saved - ")
                + (isDesktopAnnotationMode ? "Annotation Strokes" : "BlackBoard Strokes");
            return PathSafetyHelper.ResolveRelativePath(
                rootDirectory,
                directoryName);
        }

        public string BuildArchiveFilePath(string directory, bool isBlackboardMode, int currentWhiteboardIndex, int strokeCount)
        {
            string fileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.InvariantCulture)
                + (isBlackboardMode ? $" Page-{currentWhiteboardIndex} StrokesCount-{strokeCount}.icart" : ".icart");
            return PathSafetyHelper.ResolveRelativePath(
                directory,
                PathSafetyHelper.NormalizeLeafName(fileName, "InkArchive.icart"));
        }

        public void SaveArchive(string filePath, InkCanvas inkCanvas)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(inkCanvas);

            Directory.CreateDirectory(PathSafetyHelper.GetRequiredDirectoryPath(filePath));
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            SaveArchive(fileStream, inkCanvas);
        }

        public void SaveArchive(Stream outputStream, InkCanvas inkCanvas)
        {
            ArgumentNullException.ThrowIfNull(outputStream);
            ArgumentNullException.ThrowIfNull(inkCanvas);

            byte[] strokesBytes = SerializeStrokeCollection(inkCanvas.Strokes);
            using MemoryStream elementsStream = new();
            InkCanvasElementsSaveResult elementsSaveResult = elementsSerializer.SaveElements(inkCanvas, elementsStream);
            byte[] elementsBytes = elementsStream.ToArray();

            InkArchiveSnapshotMode mode = elementsSaveResult.SerializedElementCount > 0
                ? InkArchiveSnapshotMode.FullCanvas
                : InkArchiveSnapshotMode.StrokesOnly;

            SaveArchive(outputStream, strokesBytes, mode, elementsBytes, elementsSaveResult.DependencyFilePaths);
        }

        public void SaveStrokeOnlyArchive(string filePath, byte[] strokeData)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            Directory.CreateDirectory(PathSafetyHelper.GetRequiredDirectoryPath(filePath));
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            SaveStrokeOnlyArchive(fileStream, strokeData);
        }

        public void SaveStrokeOnlyArchive(Stream outputStream, byte[] strokeData)
        {
            ArgumentNullException.ThrowIfNull(outputStream);
            ValidateStrokeData(strokeData);
            SaveArchive(outputStream, strokeData, InkArchiveSnapshotMode.StrokesOnly, null, []);
        }

        public InkArchiveLoadResult LoadArchive(string filePath, string dependencyRoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(dependencyRoot);

            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadArchive(fileStream, dependencyRoot, Path.GetFileName(filePath));
        }

        public InkArchiveLoadResult LoadArchive(Stream inputStream, string? dependencyRoot, string? sourceName = null)
        {
            using MemoryStream bufferedStream = CopyToMemoryStream(inputStream);
            byte[] archiveBytes = bufferedStream.ToArray();
            if (!IsZipArchive(archiveBytes))
            {
                return new InkArchiveLoadResult(CreateStrokeCollection(archiveBytes), [], 0, null);
            }

            string archiveHash = ComputeArchiveHash(archiveBytes);
            bufferedStream.Position = 0;
            using ZipArchive archive = new(bufferedStream, ZipArchiveMode.Read, leaveOpen: true);

            ZipArchiveEntry? manifestEntry = archive.GetEntry(ManifestEntryName);
            InkArchiveManifest? manifest = manifestEntry != null ? ReadAndValidateManifest(manifestEntry) : null;
            byte[] strokesBytes = ReadStrokeBytes(archive, manifest);
            StrokeCollection strokes = CreateStrokeCollection(strokesBytes);

            if (dependencyRoot == null)
            {
                return new InkArchiveLoadResult(strokes, [], 0, null);
            }

            return LoadElementsFromArchive(archive, manifest, strokes, dependencyRoot, sourceName, archiveHash);
        }

        public byte[] LoadStrokeData(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadStrokeData(fileStream);
        }

        public byte[] LoadStrokeData(Stream inputStream)
        {
            using MemoryStream bufferedStream = CopyToMemoryStream(inputStream);
            byte[] archiveBytes = bufferedStream.ToArray();
            if (!IsZipArchive(archiveBytes))
            {
                ValidateStrokeData(archiveBytes);
                return archiveBytes;
            }

            bufferedStream.Position = 0;
            using ZipArchive archive = new(bufferedStream, ZipArchiveMode.Read, leaveOpen: true);
            ZipArchiveEntry? manifestEntry = archive.GetEntry(ManifestEntryName);
            InkArchiveManifest? manifest = manifestEntry != null ? ReadAndValidateManifest(manifestEntry) : null;
            return ReadStrokeBytes(archive, manifest);
        }

        private void SaveArchive(
            Stream outputStream,
            byte[] strokesBytes,
            InkArchiveSnapshotMode mode,
            byte[]? elementsBytes,
            IReadOnlyList<string> dependencyFilePaths)
        {
            ValidateStrokeData(strokesBytes);

            InkArchiveManifest manifest = CreateManifest(mode);
            using ZipArchive archive = new(outputStream, ZipArchiveMode.Create, leaveOpen: true);
            WriteManifest(archive, manifest);
            WriteEntry(archive, StrokesEntryName, strokesBytes);

            if (mode == InkArchiveSnapshotMode.FullCanvas && elementsBytes is { Length: > 0 })
            {
                WriteEntry(archive, ElementsEntryName, elementsBytes);
                SaveRelatedDependencies(archive, dependencyFilePaths);
            }
        }

        private InkArchiveLoadResult LoadElementsFromArchive(
            ZipArchive archive,
            InkArchiveManifest? manifest,
            StrokeCollection strokes,
            string dependencyRoot,
            string? sourceName,
            string archiveHash)
        {
            string dependencyFolderName = manifest != null
                ? InkCanvasArchiveElementsSerializer.DependencyFolderName
                : InkCanvasArchiveElementsSerializer.LegacyDependencyFolderName;
            bool expectElements = manifest == null || ParseMode(manifest.Mode) == InkArchiveSnapshotMode.FullCanvas;

            ZipArchiveEntry? elementsEntry = archive.GetEntry(ElementsEntryName);
            if (elementsEntry == null)
            {
                string? warningMessage = expectElements ? "部分元素未恢复，已跳过部分元素" : null;
                return new InkArchiveLoadResult(strokes, [], 0, warningMessage);
            }

            dependencyCacheService.InitializeSession(dependencyRoot);
            string extractionRoot = dependencyCacheService.GetImportedArchiveDirectory(sourceName ?? "archive.icart", archiveHash);
            ExtractDependencies(archive, extractionRoot, dependencyFolderName);
            string dependencyDirectory = PathSafetyHelper.ResolveRelativePath(extractionRoot, dependencyFolderName);

            try
            {
                using Stream elementsStream = elementsEntry.Open();
                InkCanvasElementsLoadResult elementsLoadResult = elementsSerializer.LoadElements(elementsStream, dependencyDirectory);
                string? warningMessage = elementsLoadResult.WarningMessage;
                if (string.IsNullOrWhiteSpace(warningMessage) && expectElements && elementsLoadResult.Elements.Count == 0)
                {
                    warningMessage = "部分元素未恢复，已跳过部分元素";
                }

                return new InkArchiveLoadResult(
                    strokes,
                    elementsLoadResult.Elements,
                    elementsLoadResult.SkippedElementCount,
                    warningMessage);
            }
            catch (Exception ex) when (IsRecoverableElementArchiveException(ex))
            {
                logger.Error(ex, "Elements Load | Failed to restore elements from archive");
                return new InkArchiveLoadResult(strokes, [], 0, "部分元素未恢复，已跳过部分元素");
            }
        }

        private void SaveRelatedDependencies(ZipArchive archive, IReadOnlyList<string> dependencyFilePaths)
        {
            foreach (string dependencyPath in dependencyFilePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                AddFileToArchive(archive, dependencyPath, InkCanvasArchiveElementsSerializer.DependencyFolderName);
            }
        }

        private void AddFileToArchive(ZipArchive archive, string filePath, string folderName)
        {
            if (!File.Exists(filePath))
            {
                logger.Error($"Elements Save | Dependency file was missing during archive save: {filePath}");
                return;
            }

            string fileName = PathSafetyHelper.NormalizeLeafName(Path.GetFileName(filePath), "dependency.bin");
            ZipArchiveEntry fileEntry = archive.CreateEntry(folderName + "/" + fileName, CompressionLevel.Optimal);
            using Stream entryStream = fileEntry.Open();
            using FileStream sourceStream = File.OpenRead(filePath);
            sourceStream.CopyTo(entryStream);
        }

        private void ExtractDependencies(ZipArchive archive, string extractionRoot, string dependencyFolderName)
        {
            string normalizedPrefix = dependencyFolderName + "/";
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                try
                {
                    string relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    string outputPath = PathSafetyHelper.ResolveRelativePath(extractionRoot, relativePath);
                    string directoryPath = PathSafetyHelper.GetRequiredDirectoryPath(outputPath);
                    Directory.CreateDirectory(directoryPath);
                    entry.ExtractToFile(outputPath, overwrite: true);
                }
                catch (ArgumentException ex)
                {
                    logger.Error(ex, $"Elements Load | Invalid dependency entry '{entry.FullName}'");
                }
                catch (IOException ex)
                {
                    logger.Error(ex, $"Elements Load | Failed to extract dependency entry '{entry.FullName}'");
                }
                catch (InvalidDataException ex)
                {
                    logger.Error(ex, $"Elements Load | Invalid dependency entry '{entry.FullName}'");
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.Error(ex, $"Elements Load | Access denied while extracting dependency entry '{entry.FullName}'");
                }
                catch (SecurityException ex)
                {
                    logger.Error(ex, $"Elements Load | Security error while extracting dependency entry '{entry.FullName}'");
                }
                catch (NotSupportedException ex)
                {
                    logger.Error(ex, $"Elements Load | Unsupported dependency entry '{entry.FullName}'");
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, $"Elements Load | Rejected dependency entry '{entry.FullName}'");
                }
            }
        }

        private static byte[] SerializeStrokeCollection(StrokeCollection strokes)
        {
            using MemoryStream memoryStream = new();
            strokes.Save(memoryStream);
            return memoryStream.ToArray();
        }

        private static StrokeCollection CreateStrokeCollection(byte[] strokeData)
        {
            ValidateStrokeData(strokeData);
            using MemoryStream memoryStream = new(strokeData);
            return new StrokeCollection(memoryStream);
        }

        private static void ValidateStrokeData(byte[] strokeData)
        {
            if (strokeData == null || strokeData.Length == 0)
            {
                throw new InvalidDataException("Ink stroke payload is empty.");
            }
        }

        private static MemoryStream CopyToMemoryStream(Stream inputStream)
        {
            ArgumentNullException.ThrowIfNull(inputStream);
            MemoryStream memoryStream = new();
            inputStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static bool IsZipArchive(byte[] payload)
        {
            if (payload.Length < ZipMagicHeader.Length)
            {
                return false;
            }

            for (int i = 0; i < ZipMagicHeader.Length; i++)
            {
                if (payload[i] != ZipMagicHeader[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string ComputeArchiveHash(byte[] archiveBytes)
        {
            byte[] hash = System.Security.Cryptography.SHA256.HashData(archiveBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static InkArchiveManifest CreateManifest(InkArchiveSnapshotMode mode)
        {
            return new InkArchiveManifest(
                ArchiveFormatName,
                CurrentArchiveVersion,
                ToModeValue(mode),
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                new InkArchiveManifestEntries(
                    StrokesEntryName,
                    mode == InkArchiveSnapshotMode.FullCanvas ? ElementsEntryName : null,
                    mode == InkArchiveSnapshotMode.FullCanvas ? InkCanvasArchiveElementsSerializer.DependencyFolderName + "/" : null));
        }

        private static string ToModeValue(InkArchiveSnapshotMode mode)
        {
            return mode == InkArchiveSnapshotMode.FullCanvas ? "full-canvas" : "strokes-only";
        }

        private static InkArchiveSnapshotMode ParseMode(string value)
        {
            return string.Equals(value, "full-canvas", StringComparison.OrdinalIgnoreCase)
                ? InkArchiveSnapshotMode.FullCanvas
                : InkArchiveSnapshotMode.StrokesOnly;
        }

        private static void WriteManifest(ZipArchive archive, InkArchiveManifest manifest)
        {
            string payload = JsonSerializer.Serialize(manifest);
            WriteEntry(archive, ManifestEntryName, Encoding.UTF8.GetBytes(payload));
        }

        private static void WriteEntry(ZipArchive archive, string entryName, byte[] payload)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            entryStream.Write(payload, 0, payload.Length);
        }

        private InkArchiveManifest ReadAndValidateManifest(ZipArchiveEntry manifestEntry)
        {
            try
            {
                using Stream manifestStream = manifestEntry.Open();
                using StreamReader reader = new(manifestStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
                string payload = reader.ReadToEnd();
                InkArchiveManifest? manifest = JsonSerializer.Deserialize<InkArchiveManifest>(payload);
                if (manifest == null)
                {
                    throw new InvalidDataException("Archive manifest is empty.");
                }

                if (!string.Equals(manifest.Format, ArchiveFormatName, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Unsupported archive format '{manifest.Format}'.");
                }

                if (manifest.Version != CurrentArchiveVersion)
                {
                    throw new InvalidDataException($"Unsupported archive version '{manifest.Version}'.");
                }

                if (!string.Equals(manifest.Mode, "strokes-only", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(manifest.Mode, "full-canvas", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Unsupported archive mode '{manifest.Mode}'.");
                }

                if (manifest.Entries == null
                    || !string.Equals(manifest.Entries.Strokes, StrokesEntryName, StringComparison.Ordinal)
                    || (string.IsNullOrWhiteSpace(manifest.CreatedAtUtc)))
                {
                    throw new InvalidDataException("Archive manifest is incomplete.");
                }

                if (ParseMode(manifest.Mode) == InkArchiveSnapshotMode.StrokesOnly)
                {
                    return manifest;
                }

                if (manifest.Entries.Elements != null
                    && !string.Equals(manifest.Entries.Elements, ElementsEntryName, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Unsupported elements entry '{manifest.Entries.Elements}'.");
                }

                if (manifest.Entries.DependenciesPrefix != null
                    && !string.Equals(
                        manifest.Entries.DependenciesPrefix,
                        InkCanvasArchiveElementsSerializer.DependencyFolderName + "/",
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Unsupported dependency prefix '{manifest.Entries.DependenciesPrefix}'.");
                }

                return manifest;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Archive manifest is invalid.", ex);
            }
        }

        private static byte[] ReadStrokeBytes(ZipArchive archive, InkArchiveManifest? manifest)
        {
            string strokeEntryName = manifest?.Entries.Strokes ?? StrokesEntryName;
            ZipArchiveEntry? strokesEntry = archive.GetEntry(strokeEntryName);
            if (strokesEntry == null)
            {
                throw new InvalidDataException("Ink archive is missing strokes.icstk.");
            }

            using Stream strokesStream = strokesEntry.Open();
            using MemoryStream buffer = new();
            strokesStream.CopyTo(buffer);
            byte[] strokeBytes = buffer.ToArray();
            ValidateStrokeData(strokeBytes);
            return strokeBytes;
        }

        private static bool IsRecoverableElementArchiveException(Exception exception)
        {
            return exception is ArgumentException
                or DirectoryNotFoundException
                or FileNotFoundException
                or InvalidDataException
                or InvalidOperationException
                or IOException
                or NotSupportedException
                or SecurityException
                or UnauthorizedAccessException
                or XmlException;
        }
    }
}
