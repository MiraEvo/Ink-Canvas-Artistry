using Ink_Canvas.ViewModels;
using System;
using System.Collections.Generic;

namespace Ink_Canvas.Features.Settings
{
    internal sealed class SettingsApplicationCoordinator
    {
        private readonly IReadOnlyList<ISettingsChangeApplier> appliers;

        public SettingsApplicationCoordinator(ISettingsApplicationHost host, SettingsViewModel settingsViewModel)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(settingsViewModel);

            appliers =
            [
                new AppearanceSettingsChangeApplier(host),
                new PresentationSettingsChangeApplier(host),
                new InputSettingsChangeApplier(host),
                new AutomationSettingsChangeApplier(host, settingsViewModel)
            ];
        }

        public void ApplyAll()
        {
            foreach (ISettingsChangeApplier applier in appliers)
            {
                applier.ApplyAll();
            }
        }

        public void ApplyPropertyChange(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            foreach (ISettingsChangeApplier applier in appliers)
            {
                applier.ApplyPropertyChange(propertyName);
            }
        }
    }

    internal interface ISettingsChangeApplier
    {
        void ApplyAll();

        void ApplyPropertyChange(string propertyName);
    }

    internal sealed class AppearanceSettingsChangeApplier(ISettingsApplicationHost host) : ISettingsChangeApplier
    {
        public void ApplyAll()
        {
            host.ApplyTheme();
            host.ApplyFloatBarTextVisibility();
            host.ApplyNibModeToggleVisibility();
            host.ApplyFloatingBarBackground();
            host.ApplyScaling();
        }

        public void ApplyPropertyChange(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(SettingsViewModel.Theme):
                    host.ApplyTheme();
                    break;
                case nameof(SettingsViewModel.IsEnableDisplayFloatBarText):
                    host.ApplyFloatBarTextVisibility();
                    break;
                case nameof(SettingsViewModel.IsEnableDisplayNibModeToggle):
                    host.ApplyNibModeToggleVisibility();
                    break;
                case nameof(SettingsViewModel.IsColorfulViewboxFloatingBar):
                    host.ApplyFloatingBarBackground();
                    break;
                case nameof(SettingsViewModel.FloatingBarBottomMargin):
                case nameof(SettingsViewModel.FloatingBarScale):
                case nameof(SettingsViewModel.BlackboardScale):
                    host.ApplyScaling();
                    break;
            }
        }
    }

    internal sealed class PresentationSettingsChangeApplier(ISettingsApplicationHost host) : ISettingsChangeApplier
    {
        public void ApplyAll()
        {
            host.ApplyPowerPointSupport();
            host.ApplyPptNavigationBottomButtonVisibility();
            host.ApplyPptNavigationSideButtonVisibility();
            host.ApplyPptBottomNavigationVisibility();
            host.ApplyPptSideNavigationVisibility();
        }

        public void ApplyPropertyChange(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(SettingsViewModel.IsSupportPowerPoint):
                    host.ApplyPowerPointSupport();
                    break;
                case nameof(SettingsViewModel.IsShowPptNavigationBottom):
                    host.ApplyPptNavigationBottomButtonVisibility();
                    break;
                case nameof(SettingsViewModel.IsShowPptNavigationSides):
                    host.ApplyPptNavigationSideButtonVisibility();
                    break;
                case nameof(SettingsViewModel.IsShowBottomPptNavigationPanel):
                    host.ApplyPptBottomNavigationVisibility();
                    break;
                case nameof(SettingsViewModel.IsShowSidePptNavigationPanel):
                    host.ApplyPptSideNavigationVisibility();
                    break;
            }
        }
    }

    internal sealed class InputSettingsChangeApplier(ISettingsApplicationHost host) : ISettingsChangeApplier
    {
        public void ApplyAll()
        {
            host.ApplyNibModeBounds();
            host.ApplyCursorVisibility();
            host.ApplyTouchMultiplierVisibility();
            host.ApplyEdgeGestureSetting();
            host.ApplyMultiTouchMode();
            host.CheckEnableTwoFingerGestureBtnColorPrompt();
        }

        public void ApplyPropertyChange(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(SettingsViewModel.IsEnableNibMode):
                case nameof(SettingsViewModel.NibModeBoundsWidth):
                case nameof(SettingsViewModel.FingerModeBoundsWidth):
                    host.ApplyNibModeBounds();
                    break;
                case nameof(SettingsViewModel.IsShowCursor):
                    host.ApplyCursorVisibility();
                    break;
                case nameof(SettingsViewModel.IsSpecialScreen):
                    host.ApplyTouchMultiplierVisibility();
                    break;
                case nameof(SettingsViewModel.IsEnableEdgeGestureUtil):
                    host.ApplyEdgeGestureSetting();
                    break;
                case nameof(SettingsViewModel.IsEnableMultiTouchMode):
                    host.ApplyMultiTouchMode();
                    host.CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(SettingsViewModel.IsEnableTwoFingerTranslate):
                case nameof(SettingsViewModel.IsEnableTwoFingerZoom):
                case nameof(SettingsViewModel.IsEnableTwoFingerRotation):
                    host.CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
            }
        }
    }

    internal sealed class AutomationSettingsChangeApplier(
        ISettingsApplicationHost host,
        SettingsViewModel settingsViewModel) : ISettingsChangeApplier
    {
        public void ApplyAll()
        {
            host.RefreshAutoFoldMonitoring();
            host.RefreshProcessKillMonitoring();
            host.ApplyAutoSaveStrokesAtClearHeader();
        }

        public void ApplyPropertyChange(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(SettingsViewModel.IsAutoUpdate):
                    if (!settingsViewModel.IsAutoUpdate)
                    {
                        host.CancelSilentUpdate();
                    }
                    break;
                case nameof(SettingsViewModel.IsAutoUpdateWithSilence):
                    if (!settingsViewModel.IsAutoUpdateWithSilence)
                    {
                        host.CancelSilentUpdate();
                    }
                    break;
                case nameof(SettingsViewModel.RunAtStartup):
                    host.ApplyRunAtStartup();
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
                    host.RefreshAutoFoldMonitoring();
                    break;
                case nameof(SettingsViewModel.IsAutoKillPptService):
                case nameof(SettingsViewModel.IsAutoKillEasiNote):
                    host.RefreshProcessKillMonitoring();
                    break;
                case nameof(SettingsViewModel.IsAutoSaveStrokesAtScreenshot):
                    host.ApplyAutoSaveStrokesAtClearHeader();
                    break;
            }
        }
    }
}
