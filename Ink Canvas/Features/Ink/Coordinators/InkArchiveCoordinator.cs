using Ink_Canvas.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Security;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal sealed class InkArchiveCoordinator
    {
        private readonly IInkArchiveHost host;
        private readonly InkHistoryCoordinator historyCoordinator;
        private readonly InkArchiveService archiveService;

        public InkArchiveCoordinator(
            IInkArchiveHost host,
            InkHistoryCoordinator historyCoordinator,
            InkArchiveService archiveService)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.historyCoordinator = historyCoordinator ?? throw new ArgumentNullException(nameof(historyCoordinator));
            this.archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
        }

        public void SaveCurrentCanvas(bool showNotice, bool saveByUser)
        {
            try
            {
                string directory = Path.Combine(
                    host.Settings.Automation.AutoSavedStrokesLocation,
                    saveByUser ? "User Saved - " : "Auto Saved - ",
                    host.IsDesktopAnnotationMode ? "Annotation Strokes" : "BlackBoard Strokes");
                string filePath = archiveService.BuildArchiveFilePath(
                    directory,
                    host.IsBlackboardMode,
                    host.CurrentWhiteboardIndex,
                    host.InkCanvas.Strokes.Count);

                archiveService.SaveArchive(filePath, host.InkCanvas);
                if (showNotice)
                {
                    host.ShowArchiveNotification("墨迹及元素成功保存至 " + filePath);
                }
            }
            catch (ArgumentException ex)
            {
                HandleFailure(ex, "Save | Invalid save path or archive state", "墨迹及元素保存失败！");
            }
            catch (IOException ex)
            {
                HandleFailure(ex, "Save | Failed to persist ink archive", "墨迹及元素保存失败！");
            }
            catch (InvalidOperationException ex)
            {
                HandleFailure(ex, "Save | Failed to create ink archive", "墨迹及元素保存失败！");
            }
            catch (UnauthorizedAccessException ex)
            {
                HandleFailure(ex, "Save | Access denied while saving ink archive", "墨迹及元素保存失败！");
            }
            catch (SecurityException ex)
            {
                HandleFailure(ex, "Save | Security error while saving ink archive", "墨迹及元素保存失败！");
            }
            catch (NotSupportedException ex)
            {
                HandleFailure(ex, "Save | Unsupported save path or archive operation", "墨迹及元素保存失败！");
            }
        }

        public void OpenArchiveFromDialog()
        {
            string? filePath = host.ShowOpenArchiveDialog();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            LogHelper.WriteLogToFile($"Strokes Insert: Name: {filePath}", LogHelper.LogType.Event);

            try
            {
                InkArchiveLoadResult result = archiveService.LoadArchive(filePath, host.Settings.Automation.AutoSavedStrokesLocation);
                host.ClearCanvasForArchiveImport();
                historyCoordinator.ClearHistory();
                host.ReplaceCanvasContent(result.Strokes, result.Elements);
                host.EnsureCanvasVisibleAfterArchiveImport();
            }
            catch (ArgumentException ex)
            {
                HandleFailure(ex, "Open | Invalid ink archive path or entry", "墨迹或元素打开失败");
            }
            catch (IOException ex)
            {
                HandleFailure(ex, "Open | Failed to read ink archive", "墨迹或元素打开失败");
            }
            catch (InvalidDataException ex)
            {
                HandleFailure(ex, "Open | Invalid ink archive data", "墨迹或元素打开失败");
            }
            catch (InvalidOperationException ex)
            {
                HandleFailure(ex, "Open | Failed to process ink archive", "墨迹或元素打开失败");
            }
            catch (UnauthorizedAccessException ex)
            {
                HandleFailure(ex, "Open | Access denied while opening ink archive", "墨迹或元素打开失败");
            }
            catch (SecurityException ex)
            {
                HandleFailure(ex, "Open | Security error while opening ink archive", "墨迹或元素打开失败");
            }
            catch (NotSupportedException ex)
            {
                HandleFailure(ex, "Open | Unsupported ink archive path or operation", "墨迹或元素打开失败");
            }
        }

        public void HandleAutoSaveAfterScreenshot()
        {
            if (host.Settings.Automation.IsAutoSaveStrokesAtScreenshot)
            {
                SaveCurrentCanvas(false, false);
            }
        }

        private void HandleFailure(Exception exception, string context, string notification)
        {
            host.ShowArchiveNotification(notification);
            LogHelper.WriteLogToFile(exception, context);
        }
    }
}

