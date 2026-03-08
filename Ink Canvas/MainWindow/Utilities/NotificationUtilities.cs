using Ink_Canvas.Helpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotificationAsync(notice, isShowImmediately);
        }

        private CancellationTokenSource showNotificationCancellationTokenSource = new();

        public void ShowNotificationAsync(string notice, bool isShowImmediately = true)
        {
            _ = ShowNotificationCoreAsync(notice, isShowImmediately);
        }

        private async Task ShowNotificationCoreAsync(string notice, bool isShowImmediately)
        {
            CancellationToken token = await Dispatcher.InvokeAsync(() =>
            {
                CancellationTokenSource previousTokenSource = showNotificationCancellationTokenSource;
                showNotificationCancellationTokenSource = new CancellationTokenSource();
                previousTokenSource.Cancel();
                previousTokenSource.Dispose();

                TextBlockNotice.Text = notice;
                if (isShowImmediately)
                {
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);
                }
                else
                {
                    GridNotifications.Visibility = Visibility.Visible;
                }

                return showNotificationCancellationTokenSource.Token;
            });

            try
            {
                await Task.Delay(2000, token);
                await Dispatcher.InvokeAsync(() => AnimationsHelper.HideWithSlideAndFade(GridNotifications));
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Notification | Show request was canceled.");
            }
        }
    }
}
