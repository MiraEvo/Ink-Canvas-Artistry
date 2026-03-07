using Ink_Canvas.Features.Settings;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private static readonly ISettingsService settingsService = new JsonSettingsService(() =>
            System.IO.Path.Combine(App.RootPath, settingsFileName));
        private static readonly IPathPickerService pathPickerService = new PathPickerService();

        private MainWindowViewModel mainWindowViewModel = null!;
        private SettingsApplicationCoordinator settingsApplicationCoordinator = null!;

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
                new InputStateViewModel(),
                new PresentationSessionViewModel(),
                new AutomationStateViewModel(),
                new WorkspaceSessionViewModel());
            mainWindowViewModel.Settings.PropertyChanged += SettingsViewModel_PropertyChanged;
            mainWindowViewModel.Settings.ReloadRequested += SettingsViewModel_ReloadRequested;
            mainWindowViewModel.Shell.WorkspaceModeChanged += ShellViewModel_WorkspaceModeChanged;
            mainWindowViewModel.Shell.ToolModeChanged += ShellViewModel_ToolModeChanged;
            mainWindowViewModel.Shell.ActiveSubPanelChanged += ShellViewModel_ActiveSubPanelChanged;
            mainWindowViewModel.Presentation.PropertyChanged += PresentationViewModel_PropertyChanged;

            DataContext = mainWindowViewModel;
            InitializeInputController();
            InitializePresentationController();
            InitializeWorkspaceSessionController();
            InitializeAutomationControllers();

            settingsApplicationCoordinator = new SettingsApplicationCoordinator(this, mainWindowViewModel.Settings);
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

        private void SettingsViewModel_ReloadRequested(string? notificationMessage)
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

        private void SettingsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!isLoaded)
            {
                return;
            }

            settingsApplicationCoordinator.ApplyPropertyChange(e.PropertyName);
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
