using Ink_Canvas.Helpers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SaveScreenshot(bool isHideNotification, string? fileName = null)
        {
            using Bitmap bitmap = GetScreenshotBitmap();
            string savePath = BuildScreenshotPath("Auto Saved - Screenshots", fileName);
            Directory.CreateDirectory(PathSafetyHelper.GetRequiredDirectoryPath(savePath));
            bitmap.Save(savePath, ImageFormat.Png);
            inkArchiveCoordinator?.HandleAutoSaveAfterScreenshot();
            if (!isHideNotification)
            {
                ShowNotificationAsync("截图成功保存至 " + savePath);
            }
        }

        private void SaveScreenShotToDesktop()
        {
            using Bitmap bitmap = GetScreenshotBitmap();
            string savePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fileName = DateTime.Now.ToString("u").Replace(':', '-') + ".png";
            string fullPath = PathSafetyHelper.ResolveRelativePath(savePath, PathSafetyHelper.NormalizeLeafName(fileName, "screenshot.png"));
            bitmap.Save(fullPath, ImageFormat.Png);
            ShowNotificationAsync("截图成功保存至【桌面" + @"\" + Path.GetFileName(fullPath) + "】");
            inkArchiveCoordinator?.HandleAutoSaveAfterScreenshot();
        }

        private void SavePPTScreenshot(string? fileName)
        {
            using Bitmap bitmap = GetScreenshotBitmap();
            string savePath = BuildScreenshotPath("Auto Saved - PPT Screenshots", fileName);
            Directory.CreateDirectory(PathSafetyHelper.GetRequiredDirectoryPath(savePath));
            bitmap.Save(savePath, ImageFormat.Png);
            inkArchiveCoordinator?.HandleAutoSaveAfterScreenshot();
        }

        private Bitmap GetScreenshotBitmap()
        {
            Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            using Graphics memoryGrahics = Graphics.FromImage(bitmap);
            memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);
            return bitmap;
        }

        private string BuildScreenshotPath(string folderName, string? fileName)
        {
            string safeFileName = PathSafetyHelper.NormalizeLeafName(
                (fileName ?? DateTime.Now.ToString("u").Replace(":", "-")) + ".png",
                "screenshot.png");

            if (Settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                string dateFolder = PathSafetyHelper.NormalizeLeafName(DateTime.Now.ToString("yyyy-MM-dd"), "screenshots");
                return PathSafetyHelper.ResolveRelativePath(
                    Settings.Automation.AutoSavedStrokesLocation,
                    folderName,
                    dateFolder,
                    safeFileName);
            }

            return PathSafetyHelper.ResolveRelativePath(
                Settings.Automation.AutoSavedStrokesLocation,
                folderName,
                safeFileName);
        }
    }
}
