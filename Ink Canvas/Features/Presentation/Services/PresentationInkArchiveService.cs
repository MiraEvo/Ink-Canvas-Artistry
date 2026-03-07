using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using File = System.IO.File;

namespace Ink_Canvas.Features.Presentation.Services
{
    internal sealed class PresentationInkArchiveService
    {
        public string GetPresentationStoragePath(string autoSavedStrokesLocation, string presentationName, int slideCount)
        {
            return Path.Combine(
                autoSavedStrokesLocation,
                "Auto Saved - Presentations",
                $"{presentationName}_{slideCount}");
        }

        public bool TryReadPosition(string folderPath, out int page)
        {
            page = 0;
            string positionFilePath = Path.Combine(folderPath, "Position");

            try
            {
                return File.Exists(positionFilePath)
                    && int.TryParse(File.ReadAllText(positionFilePath), out page)
                    && page > 0;
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to read saved slide position");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Access denied while reading saved slide position");
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to resolve saved slide position path");
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

            try
            {
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

                    slideInkBuffers[slideIndex] = File.ReadAllBytes(filePath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to load saved presentation strokes");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Access denied while loading presentation strokes");
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
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to create presentation storage folder '{folderPath}'");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Access denied while creating presentation storage folder '{folderPath}'");
                return;
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to resolve presentation storage folder '{folderPath}'");
                return;
            }

            foreach ((int slideIndex, byte[] inkData) in state.EnumerateSavedInk())
            {
                TrySaveSlideInk(folderPath, slideIndex, inkData);
            }
        }

        public void SavePosition(string folderPath, int slideIndex)
        {
            string positionFilePath = Path.Combine(folderPath, "Position");
            try
            {
                Directory.CreateDirectory(folderPath);
                File.WriteAllText(positionFilePath, slideIndex.ToString());
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
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

        private static (string icartFilePath, string icstkFilePath) GetInkFilePaths(string folderPath, int slideIndex)
        {
            string baseFilePath = Path.Combine(folderPath, slideIndex.ToString("0000"));
            return (baseFilePath + ".icart", baseFilePath + ".icstk");
        }

        private static void TrySaveSlideInk(string folderPath, int slideIndex, byte[] inkData)
        {
            (string icartFilePath, string icstkFilePath) = GetInkFilePaths(folderPath, slideIndex);

            try
            {
                if (inkData.Length > 8)
                {
                    if (File.Exists(icartFilePath))
                    {
                        File.WriteAllBytes(icartFilePath, inkData);
                    }
                    else
                    {
                        File.WriteAllBytes(icstkFilePath, inkData);
                    }
                }
                else
                {
                    File.Delete(icartFilePath);
                    File.Delete(icstkFilePath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
        }
    }
}

