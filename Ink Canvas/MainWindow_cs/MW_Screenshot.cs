using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SaveScreenshot(bool isHideNotification, string fileName = null)
        {
            using Bitmap bitmap = GetScreenshotBitmap();
            string savePath = Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - Screenshots";
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");
            if (Settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                savePath += @"\" + DateTime.Now.ToString("yyyy-MM-dd");
            }
            savePath += @"\" + fileName + ".png";
            string directory = GetRequiredDirectoryPath(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
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
            bitmap.Save(savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);
            ShowNotificationAsync("截图成功保存至【桌面" + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png】");
            inkArchiveCoordinator?.HandleAutoSaveAfterScreenshot();
        }

        private void SavePPTScreenshot(string fileName)
        {
            using Bitmap bitmap = GetScreenshotBitmap();
            string savePath = Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - PPT Screenshots";
            if (Settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                savePath += @"\" + DateTime.Now.ToString("yyyy-MM-dd");
            }
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");
            savePath += @"\" + fileName + ".png";
            string directory = GetRequiredDirectoryPath(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            bitmap.Save(savePath, ImageFormat.Png);
            inkArchiveCoordinator?.HandleAutoSaveAfterScreenshot();
        }

        private Bitmap GetScreenshotBitmap()
        {
            Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            using (Graphics memoryGrahics = Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private static string GetRequiredDirectoryPath(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Path '{filePath}' does not contain a valid directory.");
            }

            return directory;
        }
    }
}
