using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Ink_Canvas.Services.Settings;
using Ink_Canvas.Services.System;
using SettingsModel = global::Ink_Canvas.Settings;

namespace Ink_Canvas.ViewModels.Settings
{
    public sealed partial class SettingsViewModel : ObservableObject
    {
        private static readonly int[] AutoDeleteDaysOptions = [1, 3, 5, 7, 15, 30, 60, 100, 365];
        private static readonly string[] AllPropertyNames =
        [
            .. typeof(SettingsViewModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => property.Name)
        ];
        private static readonly string[] AutoUpdateVisibilityProperties = [nameof(IsAutoUpdateWithSilenceBlockVisibility)];
        private static readonly string[] AutoUpdateTimePeriodProperties = [nameof(AutoUpdateTimePeriodVisibility)];
        private static readonly string[] TouchMultiplierVisibilityProperties = [nameof(TouchMultiplierVisibility)];

        private readonly ISettingsService settingsService;
        private readonly IPathPickerService pathPickerService;
        private readonly Action<SettingsModel> settingsModelChanged;

        private SettingsModel settings = SettingsDefaults.CreateRecommended();
        private bool runAtStartup;
        private bool isHydrating = true;

        public SettingsViewModel(
            ISettingsService settingsService,
            IPathPickerService pathPickerService,
            Action<SettingsModel> settingsModelChanged)
        {
            ArgumentNullException.ThrowIfNull(settingsService);
            ArgumentNullException.ThrowIfNull(pathPickerService);
            ArgumentNullException.ThrowIfNull(settingsModelChanged);

            this.settingsService = settingsService;
            this.pathPickerService = pathPickerService;
            this.settingsModelChanged = settingsModelChanged;
        }

        public event Action<string?>? ReloadRequested;

        public SettingsModel Model => settings;

        public void Load(SettingsModel loadedSettings, bool isRunAtStartup)
        {
            ArgumentNullException.ThrowIfNull(loadedSettings);

            isHydrating = true;
            try
            {
                settings = SettingsDefaults.Normalize(loadedSettings);
                runAtStartup = isRunAtStartup;
                settingsModelChanged(settings);
                RaiseAllPropertiesChanged();
            }
            finally
            {
                isHydrating = false;
            }
        }

        public bool IsAutoUpdate
        {
            get => settings.Startup.IsAutoUpdate;
            set => SetSetting(settings.Startup.IsAutoUpdate, value, v => settings.Startup.IsAutoUpdate = v, nameof(IsAutoUpdate), AutoUpdateVisibilityProperties);
        }

        public bool IsAutoUpdateWithSilence
        {
            get => settings.Startup.IsAutoUpdateWithSilence;
            set => SetSetting(settings.Startup.IsAutoUpdateWithSilence, value, v => settings.Startup.IsAutoUpdateWithSilence = v, nameof(IsAutoUpdateWithSilence), AutoUpdateTimePeriodProperties);
        }

        public string AutoUpdateWithSilenceStartTime
        {
            get => settings.Startup.AutoUpdateWithSilenceStartTime;
            set => SetStringSetting(settings.Startup.AutoUpdateWithSilenceStartTime, value, v => settings.Startup.AutoUpdateWithSilenceStartTime = v, nameof(AutoUpdateWithSilenceStartTime));
        }

        public string AutoUpdateWithSilenceEndTime
        {
            get => settings.Startup.AutoUpdateWithSilenceEndTime;
            set => SetStringSetting(settings.Startup.AutoUpdateWithSilenceEndTime, value, v => settings.Startup.AutoUpdateWithSilenceEndTime = v, nameof(AutoUpdateWithSilenceEndTime));
        }

        public bool RunAtStartup
        {
            get => runAtStartup;
            set => SetProperty(ref runAtStartup, value);
        }

        public bool IsFoldAtStartup
        {
            get => settings.Startup.IsFoldAtStartup;
            set => SetSetting(settings.Startup.IsFoldAtStartup, value, v => settings.Startup.IsFoldAtStartup = v, nameof(IsFoldAtStartup));
        }

        public bool IsEnableNibMode
        {
            get => settings.Startup.IsEnableNibMode;
            set => SetSetting(settings.Startup.IsEnableNibMode, value, v => settings.Startup.IsEnableNibMode = v, nameof(IsEnableNibMode));
        }

        public int Theme
        {
            get => settings.Appearance.Theme;
            set => SetSetting(settings.Appearance.Theme, value, v => settings.Appearance.Theme = v, nameof(Theme));
        }

        public bool IsEnableDisplayFloatBarText
        {
            get => settings.Appearance.IsEnableDisPlayFloatBarText;
            set => SetSetting(settings.Appearance.IsEnableDisPlayFloatBarText, value, v => settings.Appearance.IsEnableDisPlayFloatBarText = v, nameof(IsEnableDisplayFloatBarText));
        }

        public bool IsEnableDisplayNibModeToggle
        {
            get => settings.Appearance.IsEnableDisPlayNibModeToggler;
            set => SetSetting(settings.Appearance.IsEnableDisPlayNibModeToggler, value, v => settings.Appearance.IsEnableDisPlayNibModeToggler = v, nameof(IsEnableDisplayNibModeToggle));
        }

        public bool IsColorfulViewboxFloatingBar
        {
            get => settings.Appearance.IsColorfulViewboxFloatingBar;
            set => SetSetting(settings.Appearance.IsColorfulViewboxFloatingBar, value, v => settings.Appearance.IsColorfulViewboxFloatingBar = v, nameof(IsColorfulViewboxFloatingBar));
        }

        public double FloatingBarBottomMargin
        {
            get => settings.Appearance.FloatingBarBottomMargin;
            set => SetSetting(settings.Appearance.FloatingBarBottomMargin, value, v => settings.Appearance.FloatingBarBottomMargin = v, nameof(FloatingBarBottomMargin));
        }

        public double FloatingBarScale
        {
            get => settings.Appearance.FloatingBarScale;
            set => SetSetting(settings.Appearance.FloatingBarScale, value, v => settings.Appearance.FloatingBarScale = v, nameof(FloatingBarScale));
        }

        public double BlackboardScale
        {
            get => settings.Appearance.BlackboardScale;
            set => SetSetting(settings.Appearance.BlackboardScale, value, v => settings.Appearance.BlackboardScale = v, nameof(BlackboardScale));
        }

        public bool IsShowPptNavigationBottom
        {
            get => settings.PowerPointSettings.IsShowPPTNavigationBottom;
            set => SetSetting(settings.PowerPointSettings.IsShowPPTNavigationBottom, value, v => settings.PowerPointSettings.IsShowPPTNavigationBottom = v, nameof(IsShowPptNavigationBottom));
        }

        public bool IsShowPptNavigationSides
        {
            get => settings.PowerPointSettings.IsShowPPTNavigationSides;
            set => SetSetting(settings.PowerPointSettings.IsShowPPTNavigationSides, value, v => settings.PowerPointSettings.IsShowPPTNavigationSides = v, nameof(IsShowPptNavigationSides));
        }

        public bool IsShowBottomPptNavigationPanel
        {
            get => settings.PowerPointSettings.IsShowBottomPPTNavigationPanel;
            set => SetSetting(settings.PowerPointSettings.IsShowBottomPPTNavigationPanel, value, v => settings.PowerPointSettings.IsShowBottomPPTNavigationPanel = v, nameof(IsShowBottomPptNavigationPanel));
        }

        public bool IsShowSidePptNavigationPanel
        {
            get => settings.PowerPointSettings.IsShowSidePPTNavigationPanel;
            set => SetSetting(settings.PowerPointSettings.IsShowSidePPTNavigationPanel, value, v => settings.PowerPointSettings.IsShowSidePPTNavigationPanel = v, nameof(IsShowSidePptNavigationPanel));
        }

        public bool IsSupportPowerPoint
        {
            get => settings.PowerPointSettings.PowerPointSupport;
            set => SetSetting(settings.PowerPointSettings.PowerPointSupport, value, v => settings.PowerPointSettings.PowerPointSupport = v, nameof(IsSupportPowerPoint));
        }

        public bool IsSupportWps
        {
            get => settings.PowerPointSettings.IsSupportWPS;
            set => SetSetting(settings.PowerPointSettings.IsSupportWPS, value, v => settings.PowerPointSettings.IsSupportWPS = v, nameof(IsSupportWps));
        }

        public bool IsShowCanvasAtNewSlideShow
        {
            get => settings.PowerPointSettings.IsShowCanvasAtNewSlideShow;
            set => SetSetting(settings.PowerPointSettings.IsShowCanvasAtNewSlideShow, value, v => settings.PowerPointSettings.IsShowCanvasAtNewSlideShow = v, nameof(IsShowCanvasAtNewSlideShow));
        }

        public bool IsEnableTwoFingerGestureInPresentationMode
        {
            get => settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode;
            set => SetSetting(settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode, value, v => settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode = v, nameof(IsEnableTwoFingerGestureInPresentationMode));
        }

        public bool IsEnableFingerGestureSlideShowControl
        {
            get => settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl;
            set => SetSetting(settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl, value, v => settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl = v, nameof(IsEnableFingerGestureSlideShowControl));
        }

        public bool IsAutoSaveScreenShotInPowerPoint
        {
            get => settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint;
            set => SetSetting(settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint, value, v => settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint = v, nameof(IsAutoSaveScreenShotInPowerPoint));
        }

        public bool IsAutoSaveStrokesInPowerPoint
        {
            get => settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint;
            set => SetSetting(settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint, value, v => settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint = v, nameof(IsAutoSaveStrokesInPowerPoint));
        }

        public bool IsNotifyPreviousPage
        {
            get => settings.PowerPointSettings.IsNotifyPreviousPage;
            set => SetSetting(settings.PowerPointSettings.IsNotifyPreviousPage, value, v => settings.PowerPointSettings.IsNotifyPreviousPage = v, nameof(IsNotifyPreviousPage));
        }

        public bool IsNotifyHiddenPage
        {
            get => settings.PowerPointSettings.IsNotifyHiddenPage;
            set => SetSetting(settings.PowerPointSettings.IsNotifyHiddenPage, value, v => settings.PowerPointSettings.IsNotifyHiddenPage = v, nameof(IsNotifyHiddenPage));
        }

        public bool IsNotifyAutoPlayPresentation
        {
            get => settings.PowerPointSettings.IsNotifyAutoPlayPresentation;
            set => SetSetting(settings.PowerPointSettings.IsNotifyAutoPlayPresentation, value, v => settings.PowerPointSettings.IsNotifyAutoPlayPresentation = v, nameof(IsNotifyAutoPlayPresentation));
        }

        public bool IsCompressPicturesUploaded
        {
            get => settings.Canvas.IsCompressPicturesUploaded;
            set => SetSetting(settings.Canvas.IsCompressPicturesUploaded, value, v => settings.Canvas.IsCompressPicturesUploaded = v, nameof(IsCompressPicturesUploaded));
        }

        public bool IsShowCursor
        {
            get => settings.Canvas.IsShowCursor;
            set => SetSetting(settings.Canvas.IsShowCursor, value, v => settings.Canvas.IsShowCursor = v, nameof(IsShowCursor));
        }

        public int InkStyle
        {
            get => settings.Canvas.InkStyle;
            set => SetSetting(settings.Canvas.InkStyle, value, v => settings.Canvas.InkStyle = v, nameof(InkStyle));
        }

        public int EraserSize
        {
            get => settings.Canvas.EraserSize;
            set => SetSetting(settings.Canvas.EraserSize, value, v => settings.Canvas.EraserSize = v, nameof(EraserSize));
        }

        public int HyperbolaAsymptoteOption
        {
            get => (int)settings.Canvas.HyperbolaAsymptoteOption;
            set => SetProjectedSetting((int)settings.Canvas.HyperbolaAsymptoteOption, value, static v => (OptionalOperation)v, v => settings.Canvas.HyperbolaAsymptoteOption = v, nameof(HyperbolaAsymptoteOption));
        }

        public bool HideStrokeWhenSelecting
        {
            get => settings.Canvas.HideStrokeWhenSelecting;
            set => SetSetting(settings.Canvas.HideStrokeWhenSelecting, value, v => settings.Canvas.HideStrokeWhenSelecting = v, nameof(HideStrokeWhenSelecting));
        }

        public int MatrixTransformCenterPoint
        {
            get => (int)settings.Gesture.MatrixTransformCenterPoint;
            set => SetProjectedSetting((int)settings.Gesture.MatrixTransformCenterPoint, value, static v => (MatrixTransformCenterPointOptions)v, v => settings.Gesture.MatrixTransformCenterPoint = v, nameof(MatrixTransformCenterPoint));
        }

        public bool AutoSwitchTwoFingerGesture
        {
            get => settings.Gesture.AutoSwitchTwoFingerGesture;
            set => SetSetting(settings.Gesture.AutoSwitchTwoFingerGesture, value, v => settings.Gesture.AutoSwitchTwoFingerGesture = v, nameof(AutoSwitchTwoFingerGesture));
        }

        public bool IsEnableMultiTouchMode
        {
            get => settings.Gesture.IsEnableMultiTouchMode;
            set => SetIsEnableMultiTouchMode(value);
        }

        public bool IsEnableTwoFingerZoom
        {
            get => settings.Gesture.IsEnableTwoFingerZoom;
            set => SetSetting(settings.Gesture.IsEnableTwoFingerZoom, value, v => settings.Gesture.IsEnableTwoFingerZoom = v, nameof(IsEnableTwoFingerZoom));
        }

        public bool IsEnableTwoFingerTranslate
        {
            get => settings.Gesture.IsEnableTwoFingerTranslate;
            set => SetIsEnableTwoFingerTranslate(value);
        }

        public bool IsEnableTwoFingerRotation
        {
            get => settings.Gesture.IsEnableTwoFingerRotation;
            set => SetSetting(settings.Gesture.IsEnableTwoFingerRotation, value, v => settings.Gesture.IsEnableTwoFingerRotation = v, nameof(IsEnableTwoFingerRotation));
        }

        public bool IsEnableTwoFingerRotationOnSelection
        {
            get => settings.Gesture.IsEnableTwoFingerRotationOnSelection;
            set => SetSetting(settings.Gesture.IsEnableTwoFingerRotationOnSelection, value, v => settings.Gesture.IsEnableTwoFingerRotationOnSelection = v, nameof(IsEnableTwoFingerRotationOnSelection));
        }

        public bool IsEnableInkToShape
        {
            get => settings.InkToShape.IsInkToShapeEnabled;
            set => SetSetting(settings.InkToShape.IsInkToShapeEnabled, value, v => settings.InkToShape.IsInkToShapeEnabled = v, nameof(IsEnableInkToShape));
        }

        public bool IsSpecialScreen
        {
            get => settings.Advanced.IsSpecialScreen;
            set => SetSetting(settings.Advanced.IsSpecialScreen, value, v => settings.Advanced.IsSpecialScreen = v, nameof(IsSpecialScreen), TouchMultiplierVisibilityProperties);
        }

        public double TouchMultiplier
        {
            get => settings.Advanced.TouchMultiplier;
            set => SetSetting(settings.Advanced.TouchMultiplier, value, v => settings.Advanced.TouchMultiplier = v, nameof(TouchMultiplier));
        }

        public double NibModeBoundsWidth
        {
            get => settings.Advanced.NibModeBoundsWidth;
            set => SetProjectedSetting((double)settings.Advanced.NibModeBoundsWidth, value, static v => (int)v, v => settings.Advanced.NibModeBoundsWidth = v, nameof(NibModeBoundsWidth));
        }

        public double FingerModeBoundsWidth
        {
            get => settings.Advanced.FingerModeBoundsWidth;
            set => SetProjectedSetting((double)settings.Advanced.FingerModeBoundsWidth, value, static v => (int)v, v => settings.Advanced.FingerModeBoundsWidth = v, nameof(FingerModeBoundsWidth));
        }

        public double NibModeBoundsWidthThresholdValue
        {
            get => settings.Advanced.NibModeBoundsWidthThresholdValue;
            set => SetSetting(settings.Advanced.NibModeBoundsWidthThresholdValue, value, v => settings.Advanced.NibModeBoundsWidthThresholdValue = v, nameof(NibModeBoundsWidthThresholdValue));
        }

        public double FingerModeBoundsWidthThresholdValue
        {
            get => settings.Advanced.FingerModeBoundsWidthThresholdValue;
            set => SetSetting(settings.Advanced.FingerModeBoundsWidthThresholdValue, value, v => settings.Advanced.FingerModeBoundsWidthThresholdValue = v, nameof(FingerModeBoundsWidthThresholdValue));
        }

        public double NibModeBoundsWidthEraserSize
        {
            get => settings.Advanced.NibModeBoundsWidthEraserSize;
            set => SetSetting(settings.Advanced.NibModeBoundsWidthEraserSize, value, v => settings.Advanced.NibModeBoundsWidthEraserSize = v, nameof(NibModeBoundsWidthEraserSize));
        }

        public double FingerModeBoundsWidthEraserSize
        {
            get => settings.Advanced.FingerModeBoundsWidthEraserSize;
            set => SetSetting(settings.Advanced.FingerModeBoundsWidthEraserSize, value, v => settings.Advanced.FingerModeBoundsWidthEraserSize = v, nameof(FingerModeBoundsWidthEraserSize));
        }

        public bool IsQuadIR
        {
            get => settings.Advanced.IsQuadIR;
            set => SetSetting(settings.Advanced.IsQuadIR, value, v => settings.Advanced.IsQuadIR = v, nameof(IsQuadIR));
        }

        public bool IsLogEnabled
        {
            get => settings.Advanced.IsLogEnabled;
            set => SetSetting(settings.Advanced.IsLogEnabled, value, v => settings.Advanced.IsLogEnabled = v, nameof(IsLogEnabled));
        }

        public bool IsSecondConfimeWhenShutdownApp
        {
            get => settings.Advanced.IsSecondConfimeWhenShutdownApp;
            set => SetSetting(settings.Advanced.IsSecondConfimeWhenShutdownApp, value, v => settings.Advanced.IsSecondConfimeWhenShutdownApp = v, nameof(IsSecondConfimeWhenShutdownApp));
        }

        public bool IsEnableEdgeGestureUtil
        {
            get => settings.Advanced.IsEnableEdgeGestureUtil;
            set => SetSetting(settings.Advanced.IsEnableEdgeGestureUtil, value, v => settings.Advanced.IsEnableEdgeGestureUtil = v, nameof(IsEnableEdgeGestureUtil));
        }

        public bool IsAutoFoldInEasiNote
        {
            get => settings.Automation.IsAutoFoldInEasiNote;
            set => SetSetting(settings.Automation.IsAutoFoldInEasiNote, value, v => settings.Automation.IsAutoFoldInEasiNote = v, nameof(IsAutoFoldInEasiNote));
        }

        public bool IsAutoFoldInEasiNoteIgnoreDesktopAnno
        {
            get => settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno;
            set => SetSetting(settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno, value, v => settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno = v, nameof(IsAutoFoldInEasiNoteIgnoreDesktopAnno));
        }

        public bool IsAutoFoldInEasiCamera
        {
            get => settings.Automation.IsAutoFoldInEasiCamera;
            set => SetSetting(settings.Automation.IsAutoFoldInEasiCamera, value, v => settings.Automation.IsAutoFoldInEasiCamera = v, nameof(IsAutoFoldInEasiCamera));
        }

        public bool IsAutoFoldInEasiNote3C
        {
            get => settings.Automation.IsAutoFoldInEasiNote3C;
            set => SetSetting(settings.Automation.IsAutoFoldInEasiNote3C, value, v => settings.Automation.IsAutoFoldInEasiNote3C = v, nameof(IsAutoFoldInEasiNote3C));
        }

        public bool IsAutoFoldInSeewoPincoTeacher
        {
            get => settings.Automation.IsAutoFoldInSeewoPincoTeacher;
            set => SetSetting(settings.Automation.IsAutoFoldInSeewoPincoTeacher, value, v => settings.Automation.IsAutoFoldInSeewoPincoTeacher = v, nameof(IsAutoFoldInSeewoPincoTeacher));
        }

        public bool IsAutoFoldInHiteTouchPro
        {
            get => settings.Automation.IsAutoFoldInHiteTouchPro;
            set => SetSetting(settings.Automation.IsAutoFoldInHiteTouchPro, value, v => settings.Automation.IsAutoFoldInHiteTouchPro = v, nameof(IsAutoFoldInHiteTouchPro));
        }

        public bool IsAutoFoldInHiteCamera
        {
            get => settings.Automation.IsAutoFoldInHiteCamera;
            set => SetSetting(settings.Automation.IsAutoFoldInHiteCamera, value, v => settings.Automation.IsAutoFoldInHiteCamera = v, nameof(IsAutoFoldInHiteCamera));
        }

        public bool IsAutoFoldInWxBoardMain
        {
            get => settings.Automation.IsAutoFoldInWxBoardMain;
            set => SetSetting(settings.Automation.IsAutoFoldInWxBoardMain, value, v => settings.Automation.IsAutoFoldInWxBoardMain = v, nameof(IsAutoFoldInWxBoardMain));
        }

        public bool IsAutoFoldInOldZyBoard
        {
            get => settings.Automation.IsAutoFoldInOldZyBoard;
            set => SetSetting(settings.Automation.IsAutoFoldInOldZyBoard, value, v => settings.Automation.IsAutoFoldInOldZyBoard = v, nameof(IsAutoFoldInOldZyBoard));
        }

        public bool IsAutoFoldInMSWhiteboard
        {
            get => settings.Automation.IsAutoFoldInMSWhiteboard;
            set => SetSetting(settings.Automation.IsAutoFoldInMSWhiteboard, value, v => settings.Automation.IsAutoFoldInMSWhiteboard = v, nameof(IsAutoFoldInMSWhiteboard));
        }

        public bool IsAutoFoldInPPTSlideShow
        {
            get => settings.Automation.IsAutoFoldInPPTSlideShow;
            set => SetSetting(settings.Automation.IsAutoFoldInPPTSlideShow, value, v => settings.Automation.IsAutoFoldInPPTSlideShow = v, nameof(IsAutoFoldInPPTSlideShow));
        }

        public bool IsAutoKillPptService
        {
            get => settings.Automation.IsAutoKillPptService;
            set => SetSetting(settings.Automation.IsAutoKillPptService, value, v => settings.Automation.IsAutoKillPptService = v, nameof(IsAutoKillPptService));
        }

        public bool IsAutoKillEasiNote
        {
            get => settings.Automation.IsAutoKillEasiNote;
            set => SetSetting(settings.Automation.IsAutoKillEasiNote, value, v => settings.Automation.IsAutoKillEasiNote = v, nameof(IsAutoKillEasiNote));
        }

        public bool IsSaveScreenshotsInDateFolders
        {
            get => settings.Automation.IsSaveScreenshotsInDateFolders;
            set => SetSetting(settings.Automation.IsSaveScreenshotsInDateFolders, value, v => settings.Automation.IsSaveScreenshotsInDateFolders = v, nameof(IsSaveScreenshotsInDateFolders));
        }

        public bool IsAutoSaveStrokesAtScreenshot
        {
            get => settings.Automation.IsAutoSaveStrokesAtScreenshot;
            set => SetSetting(settings.Automation.IsAutoSaveStrokesAtScreenshot, value, v => settings.Automation.IsAutoSaveStrokesAtScreenshot = v, nameof(IsAutoSaveStrokesAtScreenshot));
        }

        public bool IsAutoSaveStrokesAtClear
        {
            get => settings.Automation.IsAutoSaveStrokesAtClear;
            set => SetSetting(settings.Automation.IsAutoSaveStrokesAtClear, value, v => settings.Automation.IsAutoSaveStrokesAtClear = v, nameof(IsAutoSaveStrokesAtClear));
        }

        public bool IsAutoClearWhenExitingWritingMode
        {
            get => settings.Automation.IsAutoClearWhenExitingWritingMode;
            set => SetSetting(settings.Automation.IsAutoClearWhenExitingWritingMode, value, v => settings.Automation.IsAutoClearWhenExitingWritingMode = v, nameof(IsAutoClearWhenExitingWritingMode));
        }

        public double MinimumAutomationStrokeNumber
        {
            get => settings.Automation.MinimumAutomationStrokeNumber;
            set => SetProjectedSetting((double)settings.Automation.MinimumAutomationStrokeNumber, value, static v => (int)v, v => settings.Automation.MinimumAutomationStrokeNumber = v, nameof(MinimumAutomationStrokeNumber));
        }

        public string AutoSavedStrokesLocation
        {
            get => settings.Automation.AutoSavedStrokesLocation;
            set => SetStringSetting(settings.Automation.AutoSavedStrokesLocation, value, v => settings.Automation.AutoSavedStrokesLocation = v, nameof(AutoSavedStrokesLocation));
        }

        public bool AutoDelSavedFiles
        {
            get => settings.Automation.AutoDelSavedFiles;
            set => SetSetting(settings.Automation.AutoDelSavedFiles, value, v => settings.Automation.AutoDelSavedFiles = v, nameof(AutoDelSavedFiles));
        }

        public int AutoDelSavedFilesDaysThresholdIndex
        {
            get => Math.Max(0, Array.IndexOf(AutoDeleteDaysOptions, settings.Automation.AutoDelSavedFilesDaysThreshold));
            set
            {
                int safeIndex = Math.Max(0, Math.Min(value, AutoDeleteDaysOptions.Length - 1));
                int selectedDays = AutoDeleteDaysOptions[safeIndex];
                SetSetting(settings.Automation.AutoDelSavedFilesDaysThreshold, selectedDays, v => settings.Automation.AutoDelSavedFilesDaysThreshold = v, nameof(AutoDelSavedFilesDaysThresholdIndex));
            }
        }

        public Visibility IsAutoUpdateWithSilenceBlockVisibility => IsAutoUpdate ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AutoUpdateTimePeriodVisibility => IsAutoUpdateWithSilence ? Visibility.Visible : Visibility.Collapsed;

        public Visibility TouchMultiplierVisibility => IsSpecialScreen ? Visibility.Visible : Visibility.Collapsed;

        public void SetIsEnableMultiTouchMode(bool value, bool persist = true)
        {
            SetSetting(settings.Gesture.IsEnableMultiTouchMode, value, v => settings.Gesture.IsEnableMultiTouchMode = v, nameof(IsEnableMultiTouchMode), persist);
        }

        public void SetIsEnableTwoFingerTranslate(bool value, bool persist = true)
        {
            SetSetting(settings.Gesture.IsEnableTwoFingerTranslate, value, v => settings.Gesture.IsEnableTwoFingerTranslate = v, nameof(IsEnableTwoFingerTranslate), persist);
        }

        private void RaiseAllPropertiesChanged()
        {
            foreach (string propertyName in AllPropertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        [RelayCommand]
        private void ApplyFloatingBarScalePreset(string? rawValue)
        {
            ApplyDoublePreset(rawValue, value => FloatingBarScale = value);
        }

        [RelayCommand]
        private void ApplyBlackboardScalePreset(string? rawValue)
        {
            ApplyDoublePreset(rawValue, value => BlackboardScale = value);
        }

        [RelayCommand]
        private void ApplyFloatingBarMarginPreset(string? rawValue)
        {
            ApplyDoublePreset(rawValue, value => FloatingBarBottomMargin = value);
        }

        private void ApplyDoublePreset(string? rawValue, Action<double> apply)
        {
            ArgumentNullException.ThrowIfNull(apply);

            if (TryParseDouble(rawValue, out double value))
            {
                apply(value);
            }
        }

        [RelayCommand]
        private void PickAutoSavedStrokesLocation()
        {
            string? selectedPath = pathPickerService.PickFolder(AutoSavedStrokesLocation);
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                AutoSavedStrokesLocation = selectedPath;
            }
        }

        [RelayCommand]
        private void SetAutoSavedStrokesLocationToDiskD()
        {
            AutoSavedStrokesLocation = @"D:\Ink Canvas";
        }

        [RelayCommand]
        private void SetAutoSavedStrokesLocationToDocuments()
        {
            AutoSavedStrokesLocation = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Ink Canvas");
        }

        [RelayCommand]
        private void ResetToRecommendedSettings()
        {
            ReplaceSettings(SettingsDefaults.CreateRecommended(settings), true);
            ReloadRequested?.Invoke("设置已重置为默认推荐设置~");
        }

        [RelayCommand]
        private void ResetToSpecialVersionRecommendedSettings()
        {
            SettingsModel recommendedSettings = SettingsDefaults.CreateRecommended(settings);
            recommendedSettings.Automation.AutoDelSavedFiles = true;
            recommendedSettings.Automation.AutoDelSavedFilesDaysThreshold = 15;
            recommendedSettings.Appearance.IsEnableDisPlayFloatBarText = true;
            recommendedSettings.Automation.AutoSavedStrokesLocation = @"D:\Ink Canvas";

            ReplaceSettings(recommendedSettings, null);
            ReloadRequested?.Invoke(null);
        }

        private void ReplaceSettings(SettingsModel newSettings, bool? newRunAtStartup)
        {
            isHydrating = true;
            try
            {
                settings = SettingsDefaults.Normalize(newSettings);
                if (newRunAtStartup.HasValue)
                {
                    runAtStartup = newRunAtStartup.Value;
                }

                settingsModelChanged(settings);
                RaiseAllPropertiesChanged();
            }
            finally
            {
                isHydrating = false;
            }

            PersistSettings();
        }

        private static bool TryParseDouble(string? rawValue, out double value)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                value = default;
                return false;
            }

            return double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private void PersistIfNeeded()
        {
            if (isHydrating)
            {
                return;
            }

            PersistSettings();
        }

        private void SetSetting<T>(T currentValue, T newValue, Action<T> setter, string propertyName, params string[] dependentProperties)
        {
            SetSetting(currentValue, newValue, setter, propertyName, true, dependentProperties);
        }

        private void SetSetting<T>(T currentValue, T newValue, Action<T> setter, string propertyName, bool persist, params string[] dependentProperties)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return;
            }

            setter(newValue);
            OnPropertyChanged(propertyName);
            OnDependentPropertiesChanged(dependentProperties);

            if (persist)
            {
                PersistIfNeeded();
            }
        }

        private void SetStringSetting(string currentValue, string? newValue, Action<string> setter, string propertyName, bool persist = true, params string[] dependentProperties)
        {
            SetSetting(currentValue, newValue ?? string.Empty, setter, propertyName, persist, dependentProperties);
        }

        private void SetProjectedSetting<TSource, TTarget>(
            TSource currentValue,
            TSource newValue,
            Func<TSource, TTarget> projector,
            Action<TTarget> setter,
            string propertyName,
            bool persist = true,
            params string[] dependentProperties)
        {
            ArgumentNullException.ThrowIfNull(projector);
            ArgumentNullException.ThrowIfNull(setter);

            SetSetting(currentValue, newValue, value => setter(projector(value)), propertyName, persist, dependentProperties);
        }

        private void OnDependentPropertiesChanged(IEnumerable<string> dependentProperties)
        {
            foreach (string dependentProperty in dependentProperties)
            {
                OnPropertyChanged(dependentProperty);
            }
        }

        private void PersistSettings() => settingsService.Save(settings);
    }
}

