using Ink_Canvas.Helpers;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private async void AutoUpdate()
        {
            string availableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (availableLatestVersion != null)
            {
                bool IsDownloadSuccessful = false;
                IsDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(availableLatestVersion);

                if (IsDownloadSuccessful)
                {
                    if (!Settings.Startup.IsAutoUpdateWithSilence)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"Ink Canvas Artistry 新版本 (v{availableLatestVersion}) 安装包已下载完成，是否立即更新？",
                            "Ink Canvas Artistry - 新版本可用",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            AutoUpdateHelper.InstallNewVersionApp(availableLatestVersion, false);
                        }
                    }
                    else
                    {
                        ScheduleSilentUpdate(availableLatestVersion);
                        LogHelper.WriteLogToFile($"AutoUpdate | Silent update timer started for version {availableLatestVersion}.");
                    }
                }
                else
                {
                    CancelSilentUpdate();
                    LogHelper.WriteLogToFile($"AutoUpdate | Download failed for version {availableLatestVersion}.");
                }
            }
            else
            {
                CancelSilentUpdate();
                LogHelper.WriteLogToFile($"AutoUpdate | No new version found or failed to check.");
                AutoUpdateHelper.DeleteUpdatesFolder();
            }
        }
    }
}
