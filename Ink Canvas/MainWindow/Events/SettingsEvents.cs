using Ink_Canvas.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        public void SaveSettingsToFile()
        {
            try
            {
                settingsService.Save(Settings);
            }
            catch (SettingsSaveException ex)
            {
                mainWindowLogger.Error(ex, "Settings Save | Manual settings save failed");
                ShowNotificationAsync("设置未保存成功，当前修改可能不会在下次启动时保留。");
            }
        }

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        private async void SpecialVersionResetToSuggestion_Click()
        {
            await Task.Delay(1000);
            var command = SettingsViewModel.ResetToSpecialVersionRecommendedSettingsCommand;
            if (command is not null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private void BorderCalculateMultiplier_TouchDown(object sender, TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value = Settings.Advanced.IsQuadIR
                ? Math.Sqrt(args.Width * args.Height)
                : args.Width;
            TextBlockShowCalculatedMultiplier.Text = (5 / (value * 1.1)).ToString();
        }

        private void HyperlinkSourceToPresentRepository_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartWithShell("https://github.com/InkCanvas/Ink-Canvas-Artistry");
            HideSubPanels();
        }

        private void HyperlinkSourceToOringinalRepository_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartWithShell("https://github.com/WXRIW/Ink-Canvas");
            HideSubPanels();
        }
    }
}
