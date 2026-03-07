using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink
{
    internal sealed class InkArchiveService
    {
        public string BuildArchiveDirectory(string rootDirectory, bool saveByUser, bool isDesktopAnnotationMode)
        {
            return Path.Combine(
                rootDirectory,
                saveByUser ? "User Saved - " : "Auto Saved - ",
                isDesktopAnnotationMode ? "Annotation Strokes" : "BlackBoard Strokes");
        }

        public string BuildArchiveFilePath(string directory, bool isBlackboardMode, int currentWhiteboardIndex, int strokeCount)
        {
            string fileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")
                + (isBlackboardMode ? $" Page-{currentWhiteboardIndex} StrokesCount-{strokeCount}.icart" : ".icart");
            return Path.Combine(directory, fileName);
        }

        public void SaveArchive(string filePath, InkCanvas inkCanvas)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

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
                InkCanvasArchiveElementsSerializer.SaveElements(inkCanvas, elementsStream);
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
                elements = InkCanvasArchiveElementsSerializer.LoadElements(
                    elementsStream,
                    Path.Combine(dependencyRoot, InkCanvasArchiveElementsSerializer.DependencyFolderName));
            }

            return new InkArchiveLoadResult(strokes, elements);
        }

        private static void SaveRelatedDependencies(ZipArchive archive, InkCanvas inkCanvas)
        {
            string dependencyFolder = InkCanvasArchiveElementsSerializer.DependencyFolderName;
            archive.CreateEntry(dependencyFolder + "/");

            foreach (UIElement element in inkCanvas.Children)
            {
                if (InkCanvasArchiveElementsSerializer.TryGetDependencySourcePath(element, out string sourcePath))
                {
                    AddFileToArchive(archive, sourcePath, dependencyFolder);
                }
                else
                {
                    LogHelper.WriteLogToFile("该元素类型暂不支持保存", LogHelper.LogType.Error);
                }
            }
        }

        private static void AddFileToArchive(ZipArchive archive, string filePath, string folderName)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                LogHelper.WriteLogToFile($"Elements Save | Skipped dependency with invalid file name: {filePath}", LogHelper.LogType.Error);
                return;
            }

            var fileEntry = archive.CreateEntry(folderName + "/" + fileName);
            using var entryStream = fileEntry.Open();
            using var sourceStream = File.OpenRead(filePath);
            sourceStream.CopyTo(entryStream);
        }

        private static void ExtractDependencies(ZipArchive archive, string outputDirectory)
        {
            string outputRoot = Path.GetFullPath(outputDirectory);
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
                    string filePath = Path.GetFullPath(Path.Join(outputRoot, relativePath));
                    string normalizedRoot = AppendDirectorySeparator(outputRoot);

                    if (!filePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        LogHelper.WriteLogToFile($"Elements Load | Rejected archive entry outside dependency directory: {entry.FullName}", LogHelper.LogType.Error);
                        continue;
                    }

                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if (string.IsNullOrWhiteSpace(directoryPath))
                    {
                        LogHelper.WriteLogToFile($"Elements Load | Missing target directory for archive entry: {entry.FullName}", LogHelper.LogType.Error);
                        continue;
                    }

                    Directory.CreateDirectory(directoryPath);
                    if (!File.Exists(filePath))
                    {
                        entry.ExtractToFile(filePath, overwrite: false);
                    }
                }
                catch (ArgumentException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Invalid archive entry '{entry.FullName}'");
                }
                catch (IOException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Failed to extract archive entry '{entry.FullName}'");
                }
                catch (InvalidDataException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Invalid archive entry data '{entry.FullName}'");
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Access denied while extracting archive entry '{entry.FullName}'");
                }
                catch (SecurityException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Security error while extracting archive entry '{entry.FullName}'");
                }
                catch (NotSupportedException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"Elements Load | Unsupported archive entry '{entry.FullName}'");
                }
            }
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }

    internal sealed record InkArchiveLoadResult(StrokeCollection Strokes, IReadOnlyList<UIElement> Elements);
}
