using Ink_Canvas.Helpers;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SymbolIconSaveStrokes_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.Visibility != Visibility.Visible)
            {
                return;
            }

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            GridNotifications.Visibility = Visibility.Collapsed;
            SaveInkCanvasFile(true, true);
        }

        private void SaveInkCanvasFile(bool newNotice = true, bool saveByUser = false)
        {
            inkArchiveCoordinator?.SaveCurrentCanvas(newNotice, saveByUser);
        }

        private async void SymbolIconOpenInkCanvasFile_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            if (inkArchiveCoordinator != null)
            {
                await inkArchiveCoordinator.OpenArchiveFromDialogAsync();
            }
        }
    }
}
