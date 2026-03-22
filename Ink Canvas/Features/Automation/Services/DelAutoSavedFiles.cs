using Ink_Canvas.Services.Logging;
using System;
using System.IO;

namespace Ink_Canvas.Features.Automation.Services
{
    internal sealed class DelAutoSavedFiles
    {
        private readonly IAppLogger logger;

        public DelAutoSavedFiles(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(DelAutoSavedFiles));
        }

        public void DeleteFilesOlder(string directoryPath, int daysThreshold)
        {
            string[] extensionsToDel = { ".icstk", ".icart", ".png" };
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
            foreach (string subDirectory in subDirectories)
            {
                try
                {
                    string[] files = Directory.GetFiles(subDirectory);
                    foreach (string filePath in files)
                    {
                        DateTime creationDate = File.GetCreationTime(filePath);
                        string fileExtension = Path.GetExtension(filePath);
                        if (creationDate < DateTime.Now.AddDays(-daysThreshold)
                            && (Array.Exists(extensionsToDel, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                                || Path.GetFileName(filePath).Equals("Position", StringComparison.OrdinalIgnoreCase)))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
                catch (IOException ex)
                {
                    logger.Error($"DelAutoSavedFiles | 处理文件时出错: {ex}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.Error($"DelAutoSavedFiles | 处理文件时出错: {ex}");
                }
            }

            try
            {
                DeleteEmptyFolders(directoryPath);
            }
            catch (IOException ex)
            {
                logger.Error($"DelAutoSavedFiles | 处理文件时出错: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error($"DelAutoSavedFiles | 处理文件时出错: {ex}");
            }
        }

        private static void DeleteEmptyFolders(string directoryPath)
        {
            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                DeleteEmptyFolders(dir);
                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                {
                    Directory.Delete(dir, false);
                }
            }
        }
    }
}
