namespace Ink_Canvas.Services
{
    public static class SettingsDefaults
    {
        public static Settings CreateRecommended(Settings currentSettings = null)
        {
            Settings current = Normalize(currentSettings ?? new Settings());
            Settings settings = new Settings();

            settings.Advanced.IsSpecialScreen = true;
            settings.Advanced.IsQuadIR = false;
            settings.Advanced.TouchMultiplier = 0.3;
            settings.Advanced.NibModeBoundsWidth = 5;
            settings.Advanced.FingerModeBoundsWidth = 20;
            settings.Advanced.NibModeBoundsWidthThresholdValue = 2.5;
            settings.Advanced.FingerModeBoundsWidthThresholdValue = 2.5;
            settings.Advanced.NibModeBoundsWidthEraserSize = 0.8;
            settings.Advanced.FingerModeBoundsWidthEraserSize = 0.8;
            settings.Advanced.IsLogEnabled = true;
            settings.Advanced.IsSecondConfimeWhenShutdownApp = false;
            settings.Advanced.IsEnableEdgeGestureUtil = false;

            settings.Appearance.IsEnableDisPlayFloatBarText = false;
            settings.Appearance.IsEnableDisPlayNibModeToggler = false;
            settings.Appearance.IsColorfulViewboxFloatingBar = false;
            settings.Appearance.FloatingBarScale = 100.0;
            settings.Appearance.BlackboardScale = 100.0;
            settings.Appearance.IsTransparentButtonBackground = true;
            settings.Appearance.IsShowExitButton = true;
            settings.Appearance.IsShowEraserButton = true;
            settings.Appearance.IsShowHideControlButton = false;
            settings.Appearance.IsShowLRSwitchButton = false;
            settings.Appearance.IsShowModeFingerToggleSwitch = true;
            settings.Appearance.Theme = 0;

            settings.Automation.IsAutoFoldInEasiNote = true;
            settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno = true;
            settings.Automation.IsAutoFoldInEasiCamera = true;
            settings.Automation.IsAutoFoldInEasiNote3C = false;
            settings.Automation.IsAutoFoldInSeewoPincoTeacher = false;
            settings.Automation.IsAutoFoldInHiteTouchPro = false;
            settings.Automation.IsAutoFoldInHiteCamera = false;
            settings.Automation.IsAutoFoldInWxBoardMain = false;
            settings.Automation.IsAutoFoldInOldZyBoard = false;
            settings.Automation.IsAutoFoldInMSWhiteboard = false;
            settings.Automation.IsAutoFoldInPPTSlideShow = false;
            settings.Automation.IsAutoKillPptService = false;
            settings.Automation.IsAutoKillEasiNote = false;
            settings.Automation.IsSaveScreenshotsInDateFolders = false;
            settings.Automation.IsAutoSaveStrokesAtScreenshot = true;
            settings.Automation.IsAutoSaveStrokesAtClear = true;
            settings.Automation.IsAutoClearWhenExitingWritingMode = false;
            settings.Automation.MinimumAutomationStrokeNumber = 0;
            settings.Automation.AutoSavedStrokesLocation = @"D:\Ink Canvas";
            settings.Automation.AutoDelSavedFiles = current.Automation.AutoDelSavedFiles;
            settings.Automation.AutoDelSavedFilesDaysThreshold = current.Automation.AutoDelSavedFilesDaysThreshold;

            settings.PowerPointSettings.IsShowPPTNavigationBottom = false;
            settings.PowerPointSettings.IsShowPPTNavigationSides = true;
            settings.PowerPointSettings.IsShowBottomPPTNavigationPanel = true;
            settings.PowerPointSettings.IsShowSidePPTNavigationPanel = true;
            settings.PowerPointSettings.PowerPointSupport = true;
            settings.PowerPointSettings.IsShowCanvasAtNewSlideShow = true;
            settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint = true;
            settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint = false;
            settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint = true;
            settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint = true;
            settings.PowerPointSettings.IsNotifyPreviousPage = false;
            settings.PowerPointSettings.IsNotifyHiddenPage = false;
            settings.PowerPointSettings.IsNotifyAutoPlayPresentation = true;
            settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode = false;
            settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl = false;
            settings.PowerPointSettings.IsSupportWPS = true;

            settings.Canvas.InkWidth = 2.5;
            settings.Canvas.InkAlpha = 80;
            settings.Canvas.IsShowCursor = false;
            settings.Canvas.InkStyle = 0;
            settings.Canvas.EraserSize = 1;
            settings.Canvas.EraserType = 0;
            settings.Canvas.HideStrokeWhenSelecting = false;
            settings.Canvas.UsingWhiteboard = false;
            settings.Canvas.HyperbolaAsymptoteOption = OptionalOperation.Yes;

            settings.Gesture.MatrixTransformCenterPoint = MatrixTransformCenterPointOptions.CanvasCenterPoint;
            settings.Gesture.AutoSwitchTwoFingerGesture = true;
            settings.Gesture.IsEnableTwoFingerTranslate = true;
            settings.Gesture.IsEnableTwoFingerZoom = false;
            settings.Gesture.IsEnableTwoFingerRotation = false;
            settings.Gesture.IsEnableTwoFingerRotationOnSelection = true;

            settings.InkToShape.IsInkToShapeEnabled = true;

            settings.Startup.IsEnableNibMode = false;
            settings.Startup.IsAutoUpdate = true;
            settings.Startup.IsAutoUpdateWithSilence = true;
            settings.Startup.AutoUpdateWithSilenceStartTime = "18:20";
            settings.Startup.AutoUpdateWithSilenceEndTime = "07:40";
            settings.Startup.IsFoldAtStartup = false;

            return Normalize(settings);
        }

        public static Settings Normalize(Settings settings)
        {
            settings ??= new Settings();

            settings.Advanced ??= new Advanced();
            settings.Appearance ??= new Appearance();
            settings.Automation ??= new Automation();
            settings.PowerPointSettings ??= new PowerPointSettings();
            settings.Canvas ??= new Canvas();
            settings.Gesture ??= new Gesture();
            settings.InkToShape ??= new InkToShape();
            settings.Startup ??= new Startup();
            settings.RandSettings ??= new RandSettings();

            settings.Automation.AutoSavedStrokesLocation ??= @"D:\Ink Canvas";
            settings.Startup.AutoUpdateWithSilenceStartTime ??= "00:00";
            settings.Startup.AutoUpdateWithSilenceEndTime ??= "00:00";

            return settings;
        }
    }
}
