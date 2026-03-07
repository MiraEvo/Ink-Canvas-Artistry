namespace Ink_Canvas.Features.Settings.Hosts
{
    internal interface ISettingsApplicationHost
    {
        void CancelSilentUpdate();

        void ApplyRunAtStartup();

        void ApplyPowerPointSupport();

        void ApplyNibModeBounds();

        void ApplyFloatBarTextVisibility();

        void ApplyTheme();

        void ApplyNibModeToggleVisibility();

        void ApplyFloatingBarBackground();

        void ApplyScaling();

        void ApplyPptNavigationBottomButtonVisibility();

        void ApplyPptNavigationSideButtonVisibility();

        void ApplyPptBottomNavigationVisibility();

        void ApplyPptSideNavigationVisibility();

        void ApplyCursorVisibility();

        void ApplyTouchMultiplierVisibility();

        void ApplyEdgeGestureSetting();

        void ApplyMultiTouchMode();

        void ApplyLoggingEnabled();

        void CheckEnableTwoFingerGestureBtnColorPrompt();

        void RefreshAutoFoldMonitoring();

        void RefreshProcessKillMonitoring();

        void ApplyAutoSaveStrokesAtClearHeader();
    }
}

