using Ink_Canvas.Features.Settings;
using Ink_Canvas.Features.Shell;
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

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private ISettingsService settingsService = null!;
        private readonly IPathPickerService pathPickerService = new PathPickerService();

        private MainWindowViewModel mainWindowViewModel = null!;
        private SettingsApplicationCoordinator settingsApplicationCoordinator = null!;
        private ShellExperienceCoordinator shellExperienceCoordinator = null!;
        private ToolbarExperienceCoordinator toolbarExperienceCoordinator = null!;

        private SettingsViewModel SettingsViewModel => mainWindowViewModel.Settings;

        private ShellViewModel ShellViewModel => mainWindowViewModel.Shell;

        private void InitializeMvvm()
        {
            settingsService = new JsonSettingsService(
                () => PathSafetyHelper.ResolveRelativePath(App.RootPath, settingsFileName),
                appLogger);

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
            shellExperienceCoordinator = new ShellExperienceCoordinator(
                this,
                mainWindowViewModel.Shell,
                mainWindowViewModel.Settings,
                mainWindowViewModel.WorkspaceSession,
                mainWindowViewModel.Input);
            toolbarExperienceCoordinator = new ToolbarExperienceCoordinator(
                this,
                mainWindowViewModel.Shell);
            ConfigureSettingsBindings();
        }

        private void ConfigureSettingsBindings()
        {
            BindVisibility(IsAutoUpdateWithSilenceBlock, "Settings.IsAutoUpdateWithSilenceBlockVisibility");
            BindVisibility(AutoUpdateTimePeriodBlock, "Settings.AutoUpdateTimePeriodVisibility");
            BindVisibility(TouchMultiplierSlider, "Settings.TouchMultiplierVisibility");

            BindToggle(ToggleSwitchIsAutoUpdate, "Settings.IsAutoUpdate");
            BindToggle(ToggleSwitchIsAutoUpdateWithSilence, "Settings.IsAutoUpdateWithSilence");
            BindComboBoxSelectedItem(AutoUpdateWithSilenceStartTimeComboBox, "Settings.AutoUpdateWithSilenceStartTime");
            BindComboBoxSelectedItem(AutoUpdateWithSilenceEndTimeComboBox, "Settings.AutoUpdateWithSilenceEndTime");
            BindToggle(ToggleSwitchRunAtStartup, "Settings.RunAtStartup");
            BindToggle(ToggleSwitchFoldAtStartup, "Settings.IsFoldAtStartup");

            BindComboBoxSelectedIndex(ComboBoxTheme, "Settings.Theme");
            BindToggle(ToggleSwitchEnableDisPlayFloatBarText, "Settings.IsEnableDisplayFloatBarText");
            BindToggle(ToggleSwitchEnableDisPlayNibModeToggle, "Settings.IsEnableDisplayNibModeToggle");
            BindToggle(ToggleSwitchColorfulViewboxFloatingBar, "Settings.IsColorfulViewboxFloatingBar");
            BindSlider(SliderFloatingBarBottomMargin, "Settings.FloatingBarBottomMargin");
            BindSlider(SliderFloatingBarScale, "Settings.FloatingBarScale");
            BindSlider(SliderBlackboardScale, "Settings.BlackboardScale");

            BindToggle(ToggleSwitchShowButtonPPTNavigationBottom, "Settings.IsShowPptNavigationBottom");
            BindToggle(ToggleSwitchShowButtonPPTNavigationSides, "Settings.IsShowPptNavigationSides");
            BindToggle(ToggleSwitchShowPPTNavigationPanelBottom, "Settings.IsShowBottomPptNavigationPanel");
            BindToggle(ToggleSwitchShowPPTNavigationPanelSide, "Settings.IsShowSidePptNavigationPanel");
            BindToggle(ToggleSwitchSupportPowerPoint, "Settings.IsSupportPowerPoint");
            BindToggle(ToggleSwitchShowCanvasAtNewSlideShow, "Settings.IsShowCanvasAtNewSlideShow");
            BindToggle(ToggleSwitchEnableTwoFingerGestureInPresentationMode, "Settings.IsEnableTwoFingerGestureInPresentationMode");
            BindToggle(ToggleSwitchEnableFingerGestureSlideShowControl, "Settings.IsEnableFingerGestureSlideShowControl");
            BindToggle(ToggleSwitchAutoSaveScreenShotInPowerPoint, "Settings.IsAutoSaveScreenShotInPowerPoint");
            BindToggle(ToggleSwitchAutoSaveStrokesInPowerPoint, "Settings.IsAutoSaveStrokesInPowerPoint");
            BindToggle(ToggleSwitchNotifyPreviousPage, "Settings.IsNotifyPreviousPage");
            BindToggle(ToggleSwitchNotifyHiddenPage, "Settings.IsNotifyHiddenPage");
            BindToggle(ToggleSwitchNotifyAutoPlayPresentation, "Settings.IsNotifyAutoPlayPresentation");

            BindToggle(ToggleSwitchCompressPicturesUploaded, "Settings.IsCompressPicturesUploaded");
            BindToggle(ToggleSwitchShowCursor, "Settings.IsShowCursor");
            BindComboBoxSelectedIndex(ComboBoxEraserSize, "Settings.EraserSize");
            BindToggle(ToggleSwitchHideStrokeWhenSelecting, "Settings.HideStrokeWhenSelecting");
            BindComboBoxSelectedIndex(ComboBoxHyperbolaAsymptoteOption, "Settings.HyperbolaAsymptoteOption");
            BindToggle(ToggleSwitchEnableInkToShape, "Settings.IsEnableInkToShape");

            BindToggle(ToggleSwitchIsSpecialScreen, "Settings.IsSpecialScreen");
            BindSlider(TouchMultiplierSlider, "Settings.TouchMultiplier");
            BindSlider(NibModeBoundsWidthSlider, "Settings.NibModeBoundsWidth");
            BindSlider(FingerModeBoundsWidthSlider, "Settings.FingerModeBoundsWidth");
            BindSlider(NibModeBoundsWidthThresholdValueSlider, "Settings.NibModeBoundsWidthThresholdValue");
            BindSlider(FingerModeBoundsWidthThresholdValueSlider, "Settings.FingerModeBoundsWidthThresholdValue");
            BindSlider(NibModeBoundsWidthEraserSizeSlider, "Settings.NibModeBoundsWidthEraserSize");
            BindSlider(FingerModeBoundsWidthEraserSizeSlider, "Settings.FingerModeBoundsWidthEraserSize");
            BindToggle(ToggleSwitchIsQuadIR, "Settings.IsQuadIR");
            BindToggle(ToggleSwitchIsLogEnabled, "Settings.IsLogEnabled");
            BindToggle(ToggleSwitchIsSecondConfimeWhenShutdownApp, "Settings.IsSecondConfimeWhenShutdownApp");
            BindToggle(ToggleSwitchIsEnableEdgeGestureUtil, "Settings.IsEnableEdgeGestureUtil");

            BindToggle(ToggleSwitchAutoFoldInEasiNote, "Settings.IsAutoFoldInEasiNote");
            BindToggle(ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno, "Settings.IsAutoFoldInEasiNoteIgnoreDesktopAnno");
            BindToggle(ToggleSwitchAutoFoldInEasiCamera, "Settings.IsAutoFoldInEasiCamera");
            BindToggle(ToggleSwitchAutoFoldInEasiNote3C, "Settings.IsAutoFoldInEasiNote3C");
            BindToggle(ToggleSwitchAutoFoldInSeewoPincoTeacher, "Settings.IsAutoFoldInSeewoPincoTeacher");
            BindToggle(ToggleSwitchAutoFoldInHiteTouchPro, "Settings.IsAutoFoldInHiteTouchPro");
            BindToggle(ToggleSwitchAutoFoldInHiteCamera, "Settings.IsAutoFoldInHiteCamera");
            BindToggle(ToggleSwitchAutoFoldInWxBoardMain, "Settings.IsAutoFoldInWxBoardMain");
            BindToggle(ToggleSwitchAutoFoldInOldZyBoard, "Settings.IsAutoFoldInOldZyBoard");
            BindToggle(ToggleSwitchAutoFoldInMSWhiteboard, "Settings.IsAutoFoldInMSWhiteboard");
            BindToggle(ToggleSwitchAutoFoldInPPTSlideShow, "Settings.IsAutoFoldInPPTSlideShow");
            BindToggle(ToggleSwitchAutoKillPptService, "Settings.IsAutoKillPptService");
            BindToggle(ToggleSwitchAutoKillEasiNote, "Settings.IsAutoKillEasiNote");
            BindToggle(ToggleSwitchSaveScreenshotsInDateFolders, "Settings.IsSaveScreenshotsInDateFolders");
            BindToggle(ToggleSwitchAutoSaveStrokesAtScreenshot, "Settings.IsAutoSaveStrokesAtScreenshot");
            BindToggle(ToggleSwitchAutoSaveStrokesAtClear, "Settings.IsAutoSaveStrokesAtClear");
            if (FindName("ToggleSwitchClearExitingWritingMode") is ToggleSwitch clearExitingWritingModeToggle)
            {
                BindToggle(clearExitingWritingModeToggle, "Settings.IsAutoClearWhenExitingWritingMode");
            }
            BindSlider(SideControlMinimumAutomationSlider, "Settings.MinimumAutomationStrokeNumber");
            BindToggle(ToggleSwitchAutoDelSavedFiles, "Settings.AutoDelSavedFiles");
            BindComboBoxSelectedIndex(ComboBoxAutoDelSavedFilesDaysThreshold, "Settings.AutoDelSavedFilesDaysThresholdIndex");
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

        private void BindToggle(ToggleSwitch toggleSwitch, string path)
        {
            BindingOperations.SetBinding(toggleSwitch, ToggleSwitch.IsOnProperty, CreateTwoWayBinding(path));
        }

        private void BindSlider(RangeBase slider, string path)
        {
            BindingOperations.SetBinding(slider, RangeBase.ValueProperty, CreateTwoWayBinding(path));
        }

        private void BindComboBoxSelectedIndex(ComboBox comboBox, string path)
        {
            BindingOperations.SetBinding(comboBox, Selector.SelectedIndexProperty, CreateTwoWayBinding(path));
        }

        private void BindComboBoxSelectedItem(ComboBox comboBox, string path)
        {
            BindingOperations.SetBinding(comboBox, Selector.SelectedItemProperty, CreateTwoWayBinding(path));
        }

        private void BindTextBox(TextBox textBox, string path, TextChangedEventHandler handler)
        {
            textBox.TextChanged -= handler;
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, CreateTwoWayBinding(path));
        }
    }
}
