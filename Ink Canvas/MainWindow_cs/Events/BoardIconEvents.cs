using Ink_Canvas.Helpers;
using System.Windows;
using Ink_Canvas.ViewModels;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void BoardChangeBackgroundColorBtn_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleBoardBackgroundToggle();
        }

        private void BoardEraserIcon_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsEraserMode)
            {
                ShellViewModel.ToggleDeletePanelCommand.Execute(null);
            }
            else
            {
                ShellViewModel.SetToolMode(ToolMode.Eraser, true, true);
            }
        }

        private void BoardEraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsEraserByStrokesMode)
            {
                ShellViewModel.ToggleDeletePanelCommand.Execute(null);
            }
            else
            {
                ShellViewModel.SetToolMode(ToolMode.EraserByStrokes, true, true);
            }
        }

        private void BoardSymbolIconDelete_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandlePenRequested();
            toolbarExperienceCoordinator.HandleDeleteRequested();
        }

        private void BoardLaunchEasiCamera_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleBlackboardRequested();
            SoftwareLauncher.LaunchEasiCamera("希沃视频展台");
        }

        private void BoardLaunchDesmos_Click(object sender, RoutedEventArgs e)
        {
            HideSubPanelsImmediately();
            toolbarExperienceCoordinator.HandleToggleBlackboardRequested();
            ProcessHelper.StartWithShell("https://www.desmos.com/calculator?lang=zh-CN");
        }

    }
}
