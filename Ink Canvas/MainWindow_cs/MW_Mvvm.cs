using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private static readonly ISettingsService settingsService = new JsonSettingsService(() =>
            System.IO.Path.Combine(App.RootPath, settingsFileName));
        private static readonly IPathPickerService pathPickerService = new PathPickerService();

        private MainWindowViewModel mainWindowViewModel;

        private SettingsViewModel SettingsViewModel => mainWindowViewModel.Settings;

        private ShellViewModel ShellViewModel => mainWindowViewModel.Shell;

        private void InitializeMvvm()
        {
            mainWindowViewModel = new MainWindowViewModel(
                new SettingsViewModel(
                    settingsService,
                    pathPickerService,
                    settings => Settings = settings),
                new ShellViewModel(),
                new InputStateViewModel());
            mainWindowViewModel.Settings.PropertyChanged += SettingsViewModel_PropertyChanged;
            mainWindowViewModel.Settings.ReloadRequested += SettingsViewModel_ReloadRequested;
            mainWindowViewModel.Shell.WorkspaceModeChanged += ShellViewModel_WorkspaceModeChanged;
            mainWindowViewModel.Shell.ToolModeChanged += ShellViewModel_ToolModeChanged;
            mainWindowViewModel.Shell.ActiveSubPanelChanged += ShellViewModel_ActiveSubPanelChanged;

            DataContext = mainWindowViewModel;
            InitializeInputController();

            ConfigureSettingsBindings();
        }

        private void ConfigureSettingsBindings()
        {
            BindVisibility(IsAutoUpdateWithSilenceBlock, "Settings.IsAutoUpdateWithSilenceBlockVisibility");
            BindVisibility(AutoUpdateTimePeriodBlock, "Settings.AutoUpdateTimePeriodVisibility");
            BindVisibility(TouchMultiplierSlider, "Settings.TouchMultiplierVisibility");

            BindToggle(ToggleSwitchIsAutoUpdate, "Settings.IsAutoUpdate", ToggleSwitchIsAutoUpdate_Toggled);
            BindToggle(ToggleSwitchIsAutoUpdateWithSilence, "Settings.IsAutoUpdateWithSilence", ToggleSwitchIsAutoUpdateWithSilence_Toggled);
            BindComboBoxSelectedItem(AutoUpdateWithSilenceStartTimeComboBox, "Settings.AutoUpdateWithSilenceStartTime", AutoUpdateWithSilenceStartTimeComboBox_SelectionChanged);
            BindComboBoxSelectedItem(AutoUpdateWithSilenceEndTimeComboBox, "Settings.AutoUpdateWithSilenceEndTime", AutoUpdateWithSilenceEndTimeComboBox_SelectionChanged);
            BindToggle(ToggleSwitchRunAtStartup, "Settings.RunAtStartup", ToggleSwitchRunAtStartup_Toggled);
            BindToggle(ToggleSwitchFoldAtStartup, "Settings.IsFoldAtStartup", ToggleSwitchFoldAtStartup_Toggled);

            BindComboBoxSelectedIndex(ComboBoxTheme, "Settings.Theme", ComboBoxTheme_SelectionChanged);
            BindToggle(ToggleSwitchEnableDisPlayFloatBarText, "Settings.IsEnableDisplayFloatBarText", ToggleSwitchEnableDisPlayFloatBarText_Toggled);
            BindToggle(ToggleSwitchEnableDisPlayNibModeToggle, "Settings.IsEnableDisplayNibModeToggle", ToggleSwitchEnableDisPlayNibModeToggle_Toggled);
            BindToggle(ToggleSwitchColorfulViewboxFloatingBar, "Settings.IsColorfulViewboxFloatingBar", ToggleSwitchIsColorfulViewboxFloatingBar_Toggled);
            BindSlider(SliderFloatingBarBottomMargin, "Settings.FloatingBarBottomMargin", SliderFloatingBarBottomMargin_ValueChanged);
            BindSlider(SliderFloatingBarScale, "Settings.FloatingBarScale", SliderFloatingBarScale_ValueChanged);
            BindSlider(SliderBlackboardScale, "Settings.BlackboardScale", SliderBlackboardScale_ValueChanged);

            BindToggle(ToggleSwitchShowButtonPPTNavigationBottom, "Settings.IsShowPptNavigationBottom", ToggleSwitchShowButtonPPTNavigationBottom_OnToggled);
            BindToggle(ToggleSwitchShowButtonPPTNavigationSides, "Settings.IsShowPptNavigationSides", ToggleSwitchShowButtonPPTNavigationSides_OnToggled);
            BindToggle(ToggleSwitchShowPPTNavigationPanelBottom, "Settings.IsShowBottomPptNavigationPanel", ToggleSwitchShowPPTNavigationPanelBottom_OnToggled);
            BindToggle(ToggleSwitchShowPPTNavigationPanelSide, "Settings.IsShowSidePptNavigationPanel", ToggleSwitchShowPPTNavigationPanelSide_OnToggled);
            BindToggle(ToggleSwitchSupportPowerPoint, "Settings.IsSupportPowerPoint", ToggleSwitchSupportPowerPoint_Toggled);
            BindToggle(ToggleSwitchShowCanvasAtNewSlideShow, "Settings.IsShowCanvasAtNewSlideShow", ToggleSwitchShowCanvasAtNewSlideShow_Toggled);
            BindToggle(ToggleSwitchEnableTwoFingerGestureInPresentationMode, "Settings.IsEnableTwoFingerGestureInPresentationMode", ToggleSwitchEnableTwoFingerGestureInPresentationMode_Toggled);
            BindToggle(ToggleSwitchEnableFingerGestureSlideShowControl, "Settings.IsEnableFingerGestureSlideShowControl", ToggleSwitchEnableFingerGestureSlideShowControl_Toggled);
            BindToggle(ToggleSwitchAutoSaveScreenShotInPowerPoint, "Settings.IsAutoSaveScreenShotInPowerPoint", ToggleSwitchAutoSaveScreenShotInPowerPoint_Toggled);
            BindToggle(ToggleSwitchAutoSaveStrokesInPowerPoint, "Settings.IsAutoSaveStrokesInPowerPoint", ToggleSwitchAutoSaveStrokesInPowerPoint_Toggled);
            BindToggle(ToggleSwitchNotifyPreviousPage, "Settings.IsNotifyPreviousPage", ToggleSwitchNotifyPreviousPage_Toggled);
            BindToggle(ToggleSwitchNotifyHiddenPage, "Settings.IsNotifyHiddenPage", ToggleSwitchNotifyHiddenPage_Toggled);
            BindToggle(ToggleSwitchNotifyAutoPlayPresentation, "Settings.IsNotifyAutoPlayPresentation", ToggleSwitchNotifyAutoPlayPresentation_Toggled);

            BindToggle(ToggleSwitchCompressPicturesUploaded, "Settings.IsCompressPicturesUploaded", ToggleSwitchCompressPicturesUploaded_Toggled);
            BindToggle(ToggleSwitchShowCursor, "Settings.IsShowCursor", ToggleSwitchShowCursor_Toggled);
            BindComboBoxSelectedIndex(ComboBoxEraserSize, "Settings.EraserSize", ComboBoxEraserSize_SelectionChanged);
            BindToggle(ToggleSwitchHideStrokeWhenSelecting, "Settings.HideStrokeWhenSelecting", ToggleSwitchHideStrokeWhenSelecting_Toggled);
            BindComboBoxSelectedIndex(ComboBoxHyperbolaAsymptoteOption, "Settings.HyperbolaAsymptoteOption", ComboBoxHyperbolaAsymptoteOption_SelectionChanged);
            BindToggle(ToggleSwitchEnableInkToShape, "Settings.IsEnableInkToShape", ToggleSwitchEnableInkToShape_Toggled);

            BindToggle(ToggleSwitchIsSpecialScreen, "Settings.IsSpecialScreen", ToggleSwitchIsSpecialScreen_OnToggled);
            BindSlider(TouchMultiplierSlider, "Settings.TouchMultiplier", TouchMultiplierSlider_ValueChanged);
            BindSlider(NibModeBoundsWidthSlider, "Settings.NibModeBoundsWidth", NibModeBoundsWidthSlider_ValueChanged);
            BindSlider(FingerModeBoundsWidthSlider, "Settings.FingerModeBoundsWidth", FingerModeBoundsWidthSlider_ValueChanged);
            BindSlider(NibModeBoundsWidthThresholdValueSlider, "Settings.NibModeBoundsWidthThresholdValue", NibModeBoundsWidthThresholdValueSlider_ValueChanged);
            BindSlider(FingerModeBoundsWidthThresholdValueSlider, "Settings.FingerModeBoundsWidthThresholdValue", FingerModeBoundsWidthThresholdValueSlider_ValueChanged);
            BindSlider(NibModeBoundsWidthEraserSizeSlider, "Settings.NibModeBoundsWidthEraserSize", NibModeBoundsWidthEraserSizeSlider_ValueChanged);
            BindSlider(FingerModeBoundsWidthEraserSizeSlider, "Settings.FingerModeBoundsWidthEraserSize", FingerModeBoundsWidthEraserSizeSlider_ValueChanged);
            BindToggle(ToggleSwitchIsQuadIR, "Settings.IsQuadIR", ToggleSwitchIsQuadIR_Toggled);
            BindToggle(ToggleSwitchIsLogEnabled, "Settings.IsLogEnabled", ToggleSwitchIsLogEnabled_Toggled);
            BindToggle(ToggleSwitchIsSecondConfimeWhenShutdownApp, "Settings.IsSecondConfimeWhenShutdownApp", ToggleSwitchIsSecondConfimeWhenShutdownApp_Toggled);
            BindToggle(ToggleSwitchIsEnableEdgeGestureUtil, "Settings.IsEnableEdgeGestureUtil", ToggleSwitchIsEnableEdgeGestureUtil_Toggled);

            BindToggle(ToggleSwitchAutoFoldInEasiNote, "Settings.IsAutoFoldInEasiNote", ToggleSwitchAutoFoldInEasiNote_Toggled);
            BindToggle(ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno, "Settings.IsAutoFoldInEasiNoteIgnoreDesktopAnno", ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno_Toggled);
            BindToggle(ToggleSwitchAutoFoldInEasiCamera, "Settings.IsAutoFoldInEasiCamera", ToggleSwitchAutoFoldInEasiCamera_Toggled);
            BindToggle(ToggleSwitchAutoFoldInEasiNote3C, "Settings.IsAutoFoldInEasiNote3C", ToggleSwitchAutoFoldInEasiNote3C_Toggled);
            BindToggle(ToggleSwitchAutoFoldInSeewoPincoTeacher, "Settings.IsAutoFoldInSeewoPincoTeacher", ToggleSwitchAutoFoldInSeewoPincoTeacher_Toggled);
            BindToggle(ToggleSwitchAutoFoldInHiteTouchPro, "Settings.IsAutoFoldInHiteTouchPro", ToggleSwitchAutoFoldInHiteTouchPro_Toggled);
            BindToggle(ToggleSwitchAutoFoldInHiteCamera, "Settings.IsAutoFoldInHiteCamera", ToggleSwitchAutoFoldInHiteCamera_Toggled);
            BindToggle(ToggleSwitchAutoFoldInWxBoardMain, "Settings.IsAutoFoldInWxBoardMain", ToggleSwitchAutoFoldInWxBoardMain_Toggled);
            BindToggle(ToggleSwitchAutoFoldInOldZyBoard, "Settings.IsAutoFoldInOldZyBoard", ToggleSwitchAutoFoldInOldZyBoard_Toggled);
            BindToggle(ToggleSwitchAutoFoldInMSWhiteboard, "Settings.IsAutoFoldInMSWhiteboard", ToggleSwitchAutoFoldInMSWhiteboard_Toggled);
            BindToggle(ToggleSwitchAutoFoldInPPTSlideShow, "Settings.IsAutoFoldInPPTSlideShow", ToggleSwitchAutoFoldInPPTSlideShow_Toggled);
            BindToggle(ToggleSwitchAutoKillPptService, "Settings.IsAutoKillPptService", ToggleSwitchAutoKillPptService_Toggled);
            BindToggle(ToggleSwitchAutoKillEasiNote, "Settings.IsAutoKillEasiNote", ToggleSwitchAutoKillEasiNote_Toggled);
            BindToggle(ToggleSwitchSaveScreenshotsInDateFolders, "Settings.IsSaveScreenshotsInDateFolders", ToggleSwitchSaveScreenshotsInDateFolders_Toggled);
            BindToggle(ToggleSwitchAutoSaveStrokesAtScreenshot, "Settings.IsAutoSaveStrokesAtScreenshot", ToggleSwitchAutoSaveStrokesAtScreenshot_Toggled);
            BindToggle(ToggleSwitchAutoSaveStrokesAtClear, "Settings.IsAutoSaveStrokesAtClear", ToggleSwitchAutoSaveStrokesAtClear_Toggled);
            BindSlider(SideControlMinimumAutomationSlider, "Settings.MinimumAutomationStrokeNumber", SideControlMinimumAutomationSlider_ValueChanged);
            BindToggle(ToggleSwitchAutoDelSavedFiles, "Settings.AutoDelSavedFiles", ToggleSwitchAutoDelSavedFiles_Toggled);
            BindComboBoxSelectedIndex(ComboBoxAutoDelSavedFilesDaysThreshold, "Settings.AutoDelSavedFilesDaysThresholdIndex", ComboBoxAutoDelSavedFilesDaysThreshold_SelectionChanged);
        }

        private void SettingsViewModel_ReloadRequested(string notificationMessage)
        {
            bool previousIsLoaded = isLoaded;

            try
            {
                isLoaded = false;
                LoadSettings();
            }
            finally
            {
                isLoaded = previousIsLoaded;
            }

            if (!string.IsNullOrWhiteSpace(notificationMessage))
            {
                ShowNotificationAsync(notificationMessage);
            }
        }

        private void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!isLoaded)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(SettingsViewModel.RunAtStartup):
                    ApplyRunAtStartup();
                    break;
                case nameof(SettingsViewModel.IsSupportPowerPoint):
                    ApplyPowerPointSupport();
                    break;
                case nameof(SettingsViewModel.IsEnableNibMode):
                case nameof(SettingsViewModel.NibModeBoundsWidth):
                case nameof(SettingsViewModel.FingerModeBoundsWidth):
                    ApplyNibModeBounds();
                    break;
                case nameof(SettingsViewModel.IsEnableDisplayFloatBarText):
                    ApplyFloatBarTextVisibility();
                    break;
                case nameof(SettingsViewModel.Theme):
                    SystemEvents_UserPreferenceChanged(null, null);
                    break;
                case nameof(SettingsViewModel.IsEnableDisplayNibModeToggle):
                    ApplyNibModeToggleVisibility();
                    break;
                case nameof(SettingsViewModel.IsColorfulViewboxFloatingBar):
                    ApplyFloatingBarBackground();
                    break;
                case nameof(SettingsViewModel.FloatingBarBottomMargin):
                    ViewboxFloatingBarMarginAnimation();
                    break;
                case nameof(SettingsViewModel.FloatingBarScale):
                case nameof(SettingsViewModel.BlackboardScale):
                    ApplyScaling();
                    break;
                case nameof(SettingsViewModel.IsShowPptNavigationBottom):
                    PptNavigationBottomBtn.Visibility = Settings.PowerPointSettings.IsShowPPTNavigationBottom ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(SettingsViewModel.IsShowPptNavigationSides):
                    PptNavigationSidesBtn.Visibility = Settings.PowerPointSettings.IsShowPPTNavigationSides ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(SettingsViewModel.IsShowBottomPptNavigationPanel):
                    ApplyPptBottomNavigationVisibility();
                    break;
                case nameof(SettingsViewModel.IsShowSidePptNavigationPanel):
                    ApplyPptSideNavigationVisibility();
                    break;
                case nameof(SettingsViewModel.IsShowCursor):
                    inkCanvas_EditingModeChanged(inkCanvas, null);
                    break;
                case nameof(SettingsViewModel.IsSpecialScreen):
                    TouchMultiplierSlider.Visibility = Settings.Advanced.IsSpecialScreen ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(SettingsViewModel.IsEnableEdgeGestureUtil):
                    if (OperatingSystem.IsWindowsVersionAtLeast(10))
                    {
                        EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, Settings.Advanced.IsEnableEdgeGestureUtil);
                    }
                    break;
                case nameof(SettingsViewModel.IsEnableMultiTouchMode):
                    ApplyMultiTouchMode();
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(SettingsViewModel.IsEnableTwoFingerTranslate):
                case nameof(SettingsViewModel.IsEnableTwoFingerZoom):
                case nameof(SettingsViewModel.IsEnableTwoFingerRotation):
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(SettingsViewModel.IsAutoFoldInEasiNote):
                case nameof(SettingsViewModel.IsAutoFoldInEasiCamera):
                case nameof(SettingsViewModel.IsAutoFoldInEasiNote3C):
                case nameof(SettingsViewModel.IsAutoFoldInSeewoPincoTeacher):
                case nameof(SettingsViewModel.IsAutoFoldInHiteTouchPro):
                case nameof(SettingsViewModel.IsAutoFoldInHiteCamera):
                case nameof(SettingsViewModel.IsAutoFoldInWxBoardMain):
                case nameof(SettingsViewModel.IsAutoFoldInOldZyBoard):
                case nameof(SettingsViewModel.IsAutoFoldInMSWhiteboard):
                case nameof(SettingsViewModel.IsAutoFoldInPPTSlideShow):
                    StartOrStoptimerCheckAutoFold();
                    break;
                case nameof(SettingsViewModel.IsAutoKillPptService):
                case nameof(SettingsViewModel.IsAutoKillEasiNote):
                    ApplyProcessKillTimer();
                    break;
                case nameof(SettingsViewModel.IsAutoSaveStrokesAtScreenshot):
                    ToggleSwitchAutoSaveStrokesAtClear.Header = Settings.Automation.IsAutoSaveStrokesAtScreenshot ? "清屏时自动截图并保存墨迹" : "清屏时自动截图";
                    break;
            }
        }

        private void ApplyRunAtStartup()
        {
            if (SettingsViewModel.RunAtStartup)
            {
                StartAutomaticallyDel("InkCanvas");
                StartAutomaticallyDel("Ink Canvas Annotation");
                StartAutomaticallyCreate("Ink Canvas Artistry");
            }
            else
            {
                StartAutomaticallyDel("InkCanvas");
                StartAutomaticallyDel("Ink Canvas Annotation");
                StartAutomaticallyDel("Ink Canvas Artistry");
            }
        }

        private void ApplyPowerPointSupport()
        {
            if (Settings.PowerPointSettings.PowerPointSupport)
            {
                timerCheckPPT.Start();
            }
            else
            {
                timerCheckPPT.Stop();
            }
        }

        private void ApplyNibModeBounds()
        {
            BoundsWidth = Settings.Startup.IsEnableNibMode
                ? Settings.Advanced.NibModeBoundsWidth
                : Settings.Advanced.FingerModeBoundsWidth;
        }

        private void ApplyFloatBarTextVisibility()
        {
            if (Settings.Appearance.IsEnableDisPlayFloatBarText)
            {
                FloatBarSelectIconTextBlock.Visibility = Visibility.Visible;
                Icon_Pen.Height = 22;
                Icon_Eraser1.Height = 22;
                Icon_Eraser2.Height = 22;
                Icon_Eraser2.Margin = new Thickness(5, -22, 0, -8);
                Icon_EraserByStrokes1.Height = 22;
                Icon_EraserByStrokes2.Height = 22;
                Icon_EraserByStrokes2.Margin = new Thickness(12, -22, 0, -8);
                Icon_Select1.Height = 22;
                Icon_Select2.Height = 22;
                Icon_Select2.Margin = new Thickness(6, -18, 0, -8);
                Icon_Undo.Margin = new Thickness(0, 1.5, 0, -1.5);
                Icon_Redo.Margin = new Thickness(0, 1.5, 0, -1.5);
            }
            else
            {
                FloatBarSelectIconTextBlock.Visibility = Visibility.Collapsed;
                Icon_Pen.Height = 32;
                Icon_Eraser1.Height = 32;
                Icon_Eraser2.Height = 32;
                Icon_Eraser2.Margin = new Thickness(5, -32, 0, -8);
                Icon_EraserByStrokes1.Height = 32;
                Icon_EraserByStrokes2.Height = 32;
                Icon_EraserByStrokes2.Margin = new Thickness(12, -32, 0, -8);
                Icon_Select1.Height = 32;
                Icon_Select2.Height = 32;
                Icon_Select2.Margin = new Thickness(6, -28, 0, -8);
                Icon_Undo.Margin = new Thickness(0);
                Icon_Redo.Margin = new Thickness(0);
            }
        }

        private void ApplyNibModeToggleVisibility()
        {
            Visibility visibility = Settings.Appearance.IsEnableDisPlayNibModeToggler ? Visibility.Visible : Visibility.Collapsed;
            NibModeSimpleStackPanel.Visibility = visibility;
            BoardNibModeSimpleStackPanel.Visibility = visibility;
        }

        private void ApplyFloatingBarBackground()
        {
            if (Settings.Appearance.IsColorfulViewboxFloatingBar)
            {
                LinearGradientBrush gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 1)
                };
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x95, 0x80, 0xB0, 0xFF), 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x95, 0xC0, 0xFF, 0xC0), 1));

                EnableTwoFingerGestureBorder.Background = gradientBrush;
                BorderFloatingBarMainControls.Background = gradientBrush;
                BorderFloatingBarMoveControls.Background = gradientBrush;
                BtnPPTSlideShowEnd.Background = gradientBrush;
            }
            else
            {
                SystemEvents_UserPreferenceChanged(null, null);
            }
        }

        private void ApplyPptBottomNavigationVisibility()
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
            {
                return;
            }

            Visibility visibility = Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel ? Visibility.Visible : Visibility.Collapsed;
            PPTNavigationBottomLeft.Visibility = visibility;
            PPTNavigationBottomRight.Visibility = visibility;
        }

        private void ApplyPptSideNavigationVisibility()
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
            {
                return;
            }

            Visibility visibility = Settings.PowerPointSettings.IsShowSidePPTNavigationPanel ? Visibility.Visible : Visibility.Collapsed;
            PPTNavigationSidesLeft.Visibility = visibility;
            PPTNavigationSidesRight.Visibility = visibility;
        }

        private void ApplyProcessKillTimer()
        {
            if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService)
            {
                timerKillProcess.Start();
            }
            else
            {
                timerKillProcess.Stop();
            }
        }

        private void ApplyMultiTouchMode()
        {
            if (Settings.Gesture.IsEnableMultiTouchMode)
            {
                if (!isInMultiTouchMode)
                {
                    BorderMultiTouchMode_MouseUp(null, null);
                }
            }
            else if (isInMultiTouchMode)
            {
                BorderMultiTouchMode_MouseUp(null, null);
            }
        }

        private Binding CreateTwoWayBinding(string path)
        {
            return new Binding(path)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
        }

        private void BindVisibility(UIElement element, string path)
        {
            BindingOperations.SetBinding(element, UIElement.VisibilityProperty, new Binding(path));
        }

        private void BindToggle(ToggleSwitch toggleSwitch, string path, RoutedEventHandler handler)
        {
            toggleSwitch.Toggled -= handler;
            BindingOperations.SetBinding(toggleSwitch, ToggleSwitch.IsOnProperty, CreateTwoWayBinding(path));
        }

        private void BindSlider(RangeBase slider, string path, RoutedPropertyChangedEventHandler<double> handler)
        {
            slider.ValueChanged -= handler;
            BindingOperations.SetBinding(slider, RangeBase.ValueProperty, CreateTwoWayBinding(path));
        }

        private void BindComboBoxSelectedIndex(ComboBox comboBox, string path, SelectionChangedEventHandler handler)
        {
            comboBox.SelectionChanged -= handler;
            BindingOperations.SetBinding(comboBox, Selector.SelectedIndexProperty, CreateTwoWayBinding(path));
        }

        private void BindComboBoxSelectedItem(ComboBox comboBox, string path, SelectionChangedEventHandler handler)
        {
            comboBox.SelectionChanged -= handler;
            BindingOperations.SetBinding(comboBox, Selector.SelectedItemProperty, CreateTwoWayBinding(path));
        }

        private void BindTextBox(TextBox textBox, string path, TextChangedEventHandler handler)
        {
            textBox.TextChanged -= handler;
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, CreateTwoWayBinding(path));
        }
    }
}
