using Ink_Canvas.Features.Ink.Services;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using File = System.IO.File;

namespace Ink_Canvas.Features.Presentation.Services
{
    internal sealed class PresentationInkArchiveService
    {
        private readonly InkArchiveService inkArchiveService;
        private readonly IAppLogger logger;

        public PresentationInkArchiveService(InkArchiveService inkArchiveService, IAppLogger logger)
        {
            this.inkArchiveService = inkArchiveService ?? throw new ArgumentNullException(nameof(inkArchiveService));
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(PresentationInkArchiveService));
        }

        public string GetPresentationStoragePath(string autoSavedStrokesLocation, string presentationName, int slideCount)
        {
            string safeFolderName = PathSafetyHelper.NormalizeLeafName(
                $"{presentationName}_{slideCount}",
                $"Presentation_{slideCount}");
            return PathSafetyHelper.ResolveRelativePath(
                autoSavedStrokesLocation,
                "Auto Saved - Presentations",
                safeFolderName);
        }

        public bool TryReadPosition(string folderPath, out int page)
        {
            page = 0;
            string positionFilePath = PathSafetyHelper.ResolveRelativePath(folderPath, "Position");

            try
            {
                return File.Exists(positionFilePath)
                    && int.TryParse(File.ReadAllText(positionFilePath), out page)
                    && page > 0;
            }
            catch (IOException ex)
            {
                logger.Error(ex, "PowerPoint | Failed to read saved slide position");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, "PowerPoint | Access denied while reading saved slide position");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "PowerPoint | Failed to resolve saved slide position path");
            }

            return false;
        }

        public IReadOnlyDictionary<int, byte[]> LoadInkBuffers(string folderPath)
        {
            Dictionary<int, byte[]> slideInkBuffers = [];
            if (!Directory.Exists(folderPath))
            {
                return slideInkBuffers;
            }

            Dictionary<int, string> selectedFiles = SelectBestSlideFiles(folderPath);
            foreach ((int slideIndex, string filePath) in selectedFiles)
            {
                try
                {
                    slideInkBuffers[slideIndex] = inkArchiveService.LoadStrokeData(filePath);
                }
                catch (IOException ex)
                {
                    logger.Error(ex, $"PowerPoint | Failed to load saved strokes for slide {slideIndex}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.Error(ex, $"PowerPoint | Failed to load saved strokes for slide {slideIndex}");
                }
                catch (ArgumentException ex)
                {
                    logger.Error(ex, $"PowerPoint | Failed to load saved strokes for slide {slideIndex}");
                }
                catch (InvalidDataException ex)
                {
                    logger.Error(ex, $"PowerPoint | Failed to load saved strokes for slide {slideIndex}");
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, $"PowerPoint | Failed to load saved strokes for slide {slideIndex}");
                }
            }

            return slideInkBuffers;
        }

        public void SaveSession(string folderPath, PresentationInkSessionState state)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
            }
            catch (IOException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to create presentation storage folder '{folderPath}'");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"PowerPoint | Access denied while creating presentation storage folder '{folderPath}'");
                return;
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to resolve presentation storage folder '{folderPath}'");
                return;
            }

            foreach ((int slideIndex, byte[] inkData) in state.EnumerateSavedInk())
            {
                TrySaveSlideInk(folderPath, slideIndex, inkData);
            }
        }

        public void SavePosition(string folderPath, int slideIndex)
        {
            string positionFilePath = PathSafetyHelper.ResolveRelativePath(folderPath, "Position");
            try
            {
                Directory.CreateDirectory(folderPath);
                File.WriteAllText(positionFilePath, slideIndex.ToString());
            }
            catch (IOException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
        }

        private Dictionary<int, string> SelectBestSlideFiles(string folderPath)
        {
            Dictionary<int, (string FilePath, int Priority)> candidates = [];
            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                if (string.Equals(Path.GetFileName(filePath), "Position", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryParseSlideIndex(filePath, out int slideIndex))
                {
                    continue;
                }

                int priority = GetPriority(filePath);
                if (priority < 0)
                {
                    continue;
                }

                if (!candidates.TryGetValue(slideIndex, out (string FilePath, int Priority) current) || priority > current.Priority)
                {
                    candidates[slideIndex] = (filePath, priority);
                }
            }

            Dictionary<int, string> result = [];
            foreach (KeyValuePair<int, (string FilePath, int Priority)> candidate in candidates)
            {
                result[candidate.Key] = candidate.Value.FilePath;
            }

            return result;
        }

        private static bool TryParseSlideIndex(string filePath, out int slideIndex)
        {
            slideIndex = -1;
            try
            {
                return int.TryParse(Path.GetFileNameWithoutExtension(filePath), out slideIndex) && slideIndex > 0;
            }
            catch (ArgumentException)
            {
                slideIndex = -1;
                return false;
            }
        }

        private static int GetPriority(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".icart", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(extension, ".icstk", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return -1;
        }

        private static (string icartFilePath, string icstkFilePath) GetInkFilePaths(string folderPath, int slideIndex)
        {
            string baseFilePath = PathSafetyHelper.ResolveRelativePath(folderPath, slideIndex.ToString("0000"));
            return (baseFilePath + ".icart", baseFilePath + ".icstk");
        }

        private void TrySaveSlideInk(string folderPath, int slideIndex, byte[] inkData)
        {
            (string icartFilePath, string icstkFilePath) = GetInkFilePaths(folderPath, slideIndex);

            try
            {
                if (inkData.Length > 8)
                {
                    inkArchiveService.SaveStrokeOnlyArchive(icartFilePath, inkData);
                    TryDeleteFile(icstkFilePath, slideIndex);
                }
                else
                {
                    TryDeleteFile(icartFilePath, slideIndex);
                    TryDeleteFile(icstkFilePath, slideIndex);
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                TryDeleteFile(icartFilePath, slideIndex);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                TryDeleteFile(icartFilePath, slideIndex);
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                TryDeleteFile(icartFilePath, slideIndex);
            }
            catch (InvalidDataException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                TryDeleteFile(icartFilePath, slideIndex);
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                TryDeleteFile(icartFilePath, slideIndex);
            }
        }

        private void TryDeleteFile(string filePath, int slideIndex)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to clean up strokes for slide {slideIndex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to clean up strokes for slide {slideIndex}");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to clean up strokes for slide {slideIndex}");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, $"PowerPoint | Failed to clean up strokes for slide {slideIndex}");
            }
        }
    }
}
