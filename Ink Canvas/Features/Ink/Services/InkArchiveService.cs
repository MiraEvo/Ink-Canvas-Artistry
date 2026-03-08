using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Features.Ink.Services
{
    internal sealed class InkArchiveService
    {
        private readonly IAppLogger logger;
        private readonly InkCanvasArchiveElementsSerializer elementsSerializer;

        public InkArchiveService(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkArchiveService));
            elementsSerializer = new InkCanvasArchiveElementsSerializer(this.logger);
        }

        public string BuildArchiveDirectory(string rootDirectory, bool saveByUser, bool isDesktopAnnotationMode)
        {
            return PathSafetyHelper.ResolveRelativePath(
                rootDirectory,
                saveByUser ? "User Saved - " : "Auto Saved - ",
                isDesktopAnnotationMode ? "Annotation Strokes" : "BlackBoard Strokes");
        }

        public string BuildArchiveFilePath(string directory, bool isBlackboardMode, int currentWhiteboardIndex, int strokeCount)
        {
            string fileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")
                + (isBlackboardMode ? $" Page-{currentWhiteboardIndex} StrokesCount-{strokeCount}.icart" : ".icart");
            return PathSafetyHelper.ResolveRelativePath(
                directory,
                PathSafetyHelper.NormalizeLeafName(fileName, "InkArchive.icart"));
        }

        public void SaveArchive(string filePath, InkCanvas inkCanvas)
        {
            Directory.CreateDirectory(PathSafetyHelper.GetRequiredDirectoryPath(filePath));

            using FileStream fileStream = new(filePath, FileMode.Create);
            using ZipArchive archive = new(fileStream, ZipArchiveMode.Create);

            var strokesEntry = archive.CreateEntry("strokes.icstk");
            using (var strokesStream = strokesEntry.Open())
            {
                inkCanvas.Strokes.Save(strokesStream);
            }

            var elementsEntry = archive.CreateEntry("elements.xaml");
            using (var elementsStream = elementsEntry.Open())
            {
                elementsSerializer.SaveElements(inkCanvas, elementsStream);
            }

            SaveRelatedDependencies(archive, inkCanvas);
        }

        public InkArchiveLoadResult LoadArchive(string filePath, string dependencyRoot)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (extension == ".icstk")
            {
                using MemoryStream strokesStream = new();
                fileStream.CopyTo(strokesStream);
                strokesStream.Seek(0, SeekOrigin.Begin);
                return new InkArchiveLoadResult(new StrokeCollection(strokesStream), []);
            }

            if (extension != ".icart")
            {
                throw new InvalidDataException($"Unsupported ink archive extension '{extension}'.");
            }

            using ZipArchive archive = new(fileStream, ZipArchiveMode.Read);
            StrokeCollection strokes = new();
            List<UIElement> elements = [];

            var strokesEntry = archive.GetEntry("strokes.icstk");
            if (strokesEntry != null)
            {
                using var strokesStream = strokesEntry.Open();
                strokes = new StrokeCollection(strokesStream);
            }

            ExtractDependencies(archive, dependencyRoot);

            var elementsEntry = archive.GetEntry("elements.xaml");
            if (elementsEntry != null)
            {
                using var elementsStream = elementsEntry.Open();
                elements = elementsSerializer.LoadElements(
                    elementsStream,
                    PathSafetyHelper.ResolveRelativePath(
                        dependencyRoot,
                        InkCanvasArchiveElementsSerializer.DependencyFolderName));
            }

            return new InkArchiveLoadResult(strokes, elements);
        }

        private void SaveRelatedDependencies(ZipArchive archive, InkCanvas inkCanvas)
        {
            string dependencyFolder = InkCanvasArchiveElementsSerializer.DependencyFolderName;
            archive.CreateEntry(dependencyFolder + "/");

            foreach (UIElement element in inkCanvas.Children)
            {
                if (elementsSerializer.TryGetDependencySourcePath(element, out string sourcePath))
                {
                    AddFileToArchive(archive, sourcePath, dependencyFolder);
                }
                else
                {
                    logger.Error("该元素类型暂不支持保存");
                }
            }
        }

        private void AddFileToArchive(ZipArchive archive, string filePath, string folderName)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                logger.Error($"Elements Save | Skipped dependency with invalid file name: {filePath}");
                return;
            }

            string safeFileName = PathSafetyHelper.NormalizeLeafName(fileName, "dependency.bin");
            var fileEntry = archive.CreateEntry(folderName + "/" + safeFileName);
            using var entryStream = fileEntry.Open();
            using var sourceStream = File.OpenRead(filePath);
            sourceStream.CopyTo(entryStream);
        }

        private void ExtractDependencies(ZipArchive archive, string outputDirectory)
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(InkCanvasArchiveElementsSerializer.DependencyFolderName + "/", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                try
                {
                    string relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    string filePath = PathSafetyHelper.ResolveRelativePath(outputDirectory, relativePath);
                    string directoryPath = PathSafetyHelper.GetRequiredDirectoryPath(filePath);
                    Directory.CreateDirectory(directoryPath);
                    if (!File.Exists(filePath))
                    {
                        entry.ExtractToFile(filePath, overwrite: false);
                    }
                }
                catch (ArgumentException ex)
                {
                    logger.Error(ex, $"Elements Load | Invalid archive entry '{entry.FullName}'");
                }
                catch (IOException ex)
                {
                    logger.Error(ex, $"Elements Load | Failed to extract archive entry '{entry.FullName}'");
                }
                catch (InvalidDataException ex)
                {
                    logger.Error(ex, $"Elements Load | Invalid archive entry data '{entry.FullName}'");
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.Error(ex, $"Elements Load | Access denied while extracting archive entry '{entry.FullName}'");
                }
                catch (SecurityException ex)
                {
                    logger.Error(ex, $"Elements Load | Security error while extracting archive entry '{entry.FullName}'");
                }
                catch (NotSupportedException ex)
                {
                    logger.Error(ex, $"Elements Load | Unsupported archive entry '{entry.FullName}'");
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, $"Elements Load | Rejected archive entry '{entry.FullName}'");
                }
            }
        }
    }

    internal sealed record InkArchiveLoadResult(StrokeCollection Strokes, IReadOnlyList<UIElement> Elements);
}
