using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using System;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void LoadSettings(bool isStartup = false)
        {
            if (isStartup)
            {
                CursorIcon_Click(null, null);
            }

            Settings = LoadSettingsModel();
            bool runAtStartup = ReadRunAtStartupState();

            AutoUpdateWithSilenceTimeComboBox.InitializeAutoUpdateWithSilenceTimeComboBoxOptions(
                AutoUpdateWithSilenceStartTimeComboBox,
                AutoUpdateWithSilenceEndTimeComboBox);

            SettingsViewModel.Load(Settings, runAtStartup);

            ApplyStartupSettings(isStartup);
            ApplyLegacyGestureSettings();
            ApplyLegacyCanvasSettings();
            settingsApplicationCoordinator.ApplyAll();
        }

        private Settings LoadSettingsModel()
        {
            try
            {
                return SettingsDefaults.Normalize(settingsService.Load());
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Invalid settings payload");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Failed to normalize settings");
            }

            return SettingsDefaults.Normalize(new Settings());
        }

        private bool ReadRunAtStartupState()
        {
            try
            {
                return NormalizeStartupRegistration();
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Failed to normalize startup registration");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Access denied while normalizing startup registration");
            }
            catch (SecurityException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Security error while normalizing startup registration");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Failed to normalize startup registration");
            }

            return false;
        }

        private void ApplyStartupSettings(bool isStartup)
        {
            if (Settings.Startup.IsAutoUpdate)
            {
                AutoUpdate();
            }

            if (!isStartup)
            {
                return;
            }

            if (Settings.Automation.AutoDelSavedFiles)
            {
                DelAutoSavedFiles.DeleteFilesOlder(
                    Settings.Automation.AutoSavedStrokesLocation,
                    Settings.Automation.AutoDelSavedFilesDaysThreshold);
            }

            if (Settings.Startup.IsFoldAtStartup)
            {
                FoldFloatingBar_Click(Fold_Icon, null);
            }
        }

        private void ApplyLegacyGestureSettings()
        {
            if (!Settings.Gesture.AutoSwitchTwoFingerGesture)
            {
                return;
            }

            if (Topmost)
            {
                SettingsViewModel.SetIsEnableTwoFingerTranslate(false, false);
                SettingsViewModel.SetIsEnableMultiTouchMode(true, false);
            }
            else
            {
                SettingsViewModel.SetIsEnableTwoFingerTranslate(true, false);
                SettingsViewModel.SetIsEnableMultiTouchMode(false, false);
            }
        }

        private void ApplyLegacyCanvasSettings()
        {
            drawingAttributes.Height = Settings.Canvas.InkWidth;
            drawingAttributes.Width = Settings.Canvas.InkWidth;

            InkWidthSlider.Value = Settings.Canvas.InkWidth * 2;
            BoardInkWidthSlider.Value = Settings.Canvas.InkWidth * 2;
            InkAlphaSlider.Value = Settings.Canvas.InkAlpha;
            BoardInkAlphaSlider.Value = Settings.Canvas.InkAlpha;

            ComboBoxPenStyle.SelectedIndex = Settings.Canvas.InkStyle;
            BoardComboBoxPenStyle.SelectedIndex = Settings.Canvas.InkStyle;

            if (Settings.Canvas.UsingWhiteboard)
            {
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
                lastBoardInkColor = 0;
            }
            else
            {
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FF1F1F1F"));
                lastBoardInkColor = 5;
            }
        }
    }
}
