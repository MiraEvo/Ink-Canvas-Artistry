using Ink_Canvas.Helpers;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void AutoUpdate()
        {
            taskGuard.Forget(
                AutoUpdateAsync(),
                new AppErrorContext(nameof(MainWindow), "AutoUpdate")
                {
                    AllowRateLimit = true,
                    RateLimitKey = "MainWindow|AutoUpdate"
                });
        }

        private async Task AutoUpdateAsync()
        {
            string? availableLatestVersion = await autoUpdateHelper.CheckForUpdates();

            if (availableLatestVersion is not null)
            {
                bool isDownloadSuccessful = await autoUpdateHelper.DownloadSetupFileAndSaveStatus(availableLatestVersion);

                if (isDownloadSuccessful)
                {
                    if (!Settings.Startup.IsAutoUpdateWithSilence)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"Ink Canvas Modern 新版本 (v{availableLatestVersion}) 安装包已下载完成，是否立即更新？",
                            "Ink Canvas Modern - 新版本可用",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            autoUpdateHelper.InstallNewVersionApp(availableLatestVersion, false);
                        }
                    }
                    else
                    {
                        ScheduleSilentUpdate(availableLatestVersion);
                        mainWindowLogger.Info($"AutoUpdate | Silent update timer started for version {availableLatestVersion}.");
                    }
                }
                else
                {
                    CancelSilentUpdate();
                    mainWindowLogger.Info($"AutoUpdate | Download failed for version {availableLatestVersion}.");
                }
            }
            else
            {
                CancelSilentUpdate();
                mainWindowLogger.Info("AutoUpdate | No new version found or failed to check.");
                autoUpdateHelper.DeleteUpdatesFolder();
            }
        }
    }
}

