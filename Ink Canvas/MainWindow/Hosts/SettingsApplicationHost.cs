using Ink_Canvas.Features.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : ISettingsApplicationHost
    {
        void ISettingsApplicationHost.CancelSilentUpdate() => CancelSilentUpdate();

        void ISettingsApplicationHost.ApplyRunAtStartup() => ApplyRunAtStartup();

        void ISettingsApplicationHost.ApplyPowerPointSupport() => ApplyPowerPointSupport();

        void ISettingsApplicationHost.ApplyNibModeBounds() => ApplyNibModeBounds();

        void ISettingsApplicationHost.ApplyFloatBarTextVisibility() => ApplyFloatBarTextVisibility();

        void ISettingsApplicationHost.ApplyTheme() => ApplyTheme();

        void ISettingsApplicationHost.ApplyNibModeToggleVisibility() => ApplyNibModeToggleVisibility();

        void ISettingsApplicationHost.ApplyFloatingBarBackground() => ApplyFloatingBarBackground();

        void ISettingsApplicationHost.ApplyScaling() => ApplyScaling();

        void ISettingsApplicationHost.ApplyPptNavigationBottomButtonVisibility() => ApplyPptNavigationBottomButtonVisibility();

        void ISettingsApplicationHost.ApplyPptNavigationSideButtonVisibility() => ApplyPptNavigationSideButtonVisibility();

        void ISettingsApplicationHost.ApplyPptBottomNavigationVisibility() => ApplyPptBottomNavigationVisibility();

        void ISettingsApplicationHost.ApplyPptSideNavigationVisibility() => ApplyPptSideNavigationVisibility();

        void ISettingsApplicationHost.ApplyCursorVisibility() => ApplyCursorVisibility();

        void ISettingsApplicationHost.ApplyTouchMultiplierVisibility() => ApplyTouchMultiplierVisibility();

        void ISettingsApplicationHost.ApplyEdgeGestureSetting() => ApplyEdgeGestureSetting();

        void ISettingsApplicationHost.ApplyMultiTouchMode() => ApplyMultiTouchMode();

        void ISettingsApplicationHost.ApplyLoggingEnabled() => ApplyLoggingEnabled();

        void ISettingsApplicationHost.CheckEnableTwoFingerGestureBtnColorPrompt() => CheckEnableTwoFingerGestureBtnColorPrompt();

        void ISettingsApplicationHost.RefreshAutoFoldMonitoring() => RefreshAutoFoldMonitoring();

        void ISettingsApplicationHost.RefreshProcessKillMonitoring() => RefreshProcessKillMonitoring();

        void ISettingsApplicationHost.ApplyAutoSaveStrokesAtClearHeader() => ApplyAutoSaveStrokesAtClearHeader();

        void ISettingsApplicationHost.ApplyAutoSavedStrokesLocationChanged(string? previousRoot, string currentRoot) =>
            ApplyAutoSavedStrokesLocationChanged(previousRoot, currentRoot);

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
                StartPresentationMonitoring();
            }
            else
            {
                StopPresentationMonitoring();
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

        private void ApplyTheme()
        {
            SystemEvents_UserPreferenceChanged(null, null);
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
                LinearGradientBrush gradientBrush = new()
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
                ApplyTheme();
            }
        }

        private void ApplyScaling()
        {
            double floatingBarScaleFactor = Settings.Appearance.FloatingBarScale / 100.0;
            ViewboxFloatingBarScaleTransform.ScaleX = floatingBarScaleFactor;
            ViewboxFloatingBarScaleTransform.ScaleY = floatingBarScaleFactor;

            double blackboardScaleFactor = Settings.Appearance.BlackboardScale / 100.0;
            ViewboxBlackboardLeftSideScaleTransform.ScaleX = blackboardScaleFactor;
            ViewboxBlackboardLeftSideScaleTransform.ScaleY = blackboardScaleFactor;
            ViewboxBlackboardCenterSideScaleTransform.ScaleX = blackboardScaleFactor;
            ViewboxBlackboardCenterSideScaleTransform.ScaleY = blackboardScaleFactor;
            ViewboxBlackboardRightSideScaleTransform.ScaleX = blackboardScaleFactor;
            ViewboxBlackboardRightSideScaleTransform.ScaleY = blackboardScaleFactor;

            ViewboxFloatingBarMarginAnimation();
        }

        private void ApplyPptNavigationBottomButtonVisibility()
        {
            PptNavigationBottomBtn.Visibility = Settings.PowerPointSettings.IsShowPPTNavigationBottom
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ApplyPptNavigationSideButtonVisibility()
        {
            PptNavigationSidesBtn.Visibility = Settings.PowerPointSettings.IsShowPPTNavigationSides
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ApplyPptBottomNavigationVisibility()
        {
            if (!IsPresentationSlideShowRunning)
            {
                return;
            }

            Visibility visibility = Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel ? Visibility.Visible : Visibility.Collapsed;
            PPTNavigationBottomLeft.Visibility = visibility;
            PPTNavigationBottomRight.Visibility = visibility;
            PresentationViewModel.SetNavigationVisibility(
                visibility == Visibility.Visible,
                PresentationViewModel.IsSideNavigationVisible);
        }

        private void ApplyPptSideNavigationVisibility()
        {
            if (!IsPresentationSlideShowRunning)
            {
                return;
            }

            Visibility visibility = Settings.PowerPointSettings.IsShowSidePPTNavigationPanel ? Visibility.Visible : Visibility.Collapsed;
            PPTNavigationSidesLeft.Visibility = visibility;
            PPTNavigationSidesRight.Visibility = visibility;
            PresentationViewModel.SetNavigationVisibility(
                PresentationViewModel.IsBottomNavigationVisible,
                visibility == Visibility.Visible);
        }

        private void ApplyCursorVisibility()
        {
            inkCanvas_EditingModeChanged(inkCanvas, null);
        }

        private void ApplyTouchMultiplierVisibility()
        {
            TouchMultiplierSlider.Visibility = Settings.Advanced.IsSpecialScreen
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ApplyEdgeGestureSetting()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10))
            {
                EdgeGestureUtil.DisableEdgeGestures(
                    new WindowInteropHelper(this).Handle,
                    Settings.Advanced.IsEnableEdgeGestureUtil);
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

        private void ApplyLoggingEnabled()
        {
            appLogger.SetEnabled(Settings.Advanced.IsLogEnabled);
        }

        private void ApplyAutoSaveStrokesAtClearHeader()
        {
            ToggleSwitchAutoSaveStrokesAtClear.Header = Settings.Automation.IsAutoSaveStrokesAtScreenshot
                ? "清屏时自动截图并保存墨迹"
                : "清屏时自动截图";
        }

        private void ApplyAutoSavedStrokesLocationChanged(string? previousRoot, string currentRoot)
        {
            try
            {
                List<UIElement> referencedElements = inkCanvas.Children.Cast<UIElement>().ToList();
                inkDependencyCacheService.SwitchSessionRoot(previousRoot, currentRoot, referencedElements);
            }
            catch (IOException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
            catch (UnauthorizedAccessException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
            catch (ArgumentException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
            catch (InvalidOperationException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
            catch (NotSupportedException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
            catch (UriFormatException ex)
            {
                mainWindowLogger.Error(ex, "Settings Apply | Failed to switch auto saved strokes location");
            }
        }
    }
}
