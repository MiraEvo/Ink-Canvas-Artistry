using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private bool isApplyingShellSubPanelState;

        private bool isFloatingBarFolded
        {
            get => ShellViewModel?.IsFloatingBarFolded == true;
            set => ShellViewModel?.SetFloatingBarFolded(value, false);
        }

        private bool isFloatingBarChangingHideMode
        {
            get => ShellViewModel?.IsFloatingBarTransitioning == true;
            set => ShellViewModel?.SetFloatingBarTransitioning(value);
        }

        private bool isDisplayingOrHidingBlackboard
        {
            get => ShellViewModel?.IsBlackboardTransitioning == true;
            set => ShellViewModel?.SetBlackboardTransitioning(value);
        }

        private void ShellViewModel_WorkspaceModeChanged(WorkspaceMode mode)
        {
            ApplyWorkspaceModeChange(mode);
        }

        private void ShellViewModel_ToolModeChanged(ToolMode mode)
        {
            ApplyToolModeChange(mode);
        }

        private void ShellViewModel_ActiveSubPanelChanged(SubPanelKind panel)
        {
            ApplySubPanelState(panel);
        }

        private void ApplyWorkspaceModeChange(WorkspaceMode workspaceMode)
        {
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                ShellViewModel.SetToolMode(ToolMode.Pen, true, true);
            }

            if (workspaceMode == WorkspaceMode.Blackboard)
            {
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewboxFloatingBarMarginAnimation();
                    });
                })).Start();

                if (Pen_Icon.Background == null)
                {
                    ShellViewModel.SetToolMode(ToolMode.Pen, true, true);
                }

                if (Settings.Gesture.AutoSwitchTwoFingerGesture)
                {
                    SettingsViewModel.SetIsEnableTwoFingerTranslate(true, false);
                    SettingsViewModel.SetIsEnableMultiTouchMode(false, false);
                }
            }
            else
            {
                HideSubPanelsImmediately();

                if (IsPresentationSlideShowRunning)
                {
                    if (Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel)
                    {
                        AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                        AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
                    }
                    if (Settings.PowerPointSettings.IsShowSidePPTNavigationPanel)
                    {
                        AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                        AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
                    }
                }

                if (Settings.Automation.IsAutoSaveStrokesAtClear
                    && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    SaveScreenshot(true);
                }

                ViewboxFloatingBarMarginAnimation();

                if (Pen_Icon.Background == null)
                {
                    ShellViewModel.SetToolMode(ToolMode.Pen, true, true);
                }

                if (Settings.Gesture.AutoSwitchTwoFingerGesture)
                {
                    SettingsViewModel.SetIsEnableTwoFingerTranslate(false, false);
                    SettingsViewModel.SetIsEnableMultiTouchMode(true, false);
                }
            }

            ApplyWorkspaceVisualState(workspaceMode);

            if (workspaceMode == WorkspaceMode.DesktopAnnotation
                && inkCanvas.Strokes.Count == 0
                && !IsPresentationSlideShowRunning)
            {
                ShellViewModel.SetToolMode(ToolMode.Cursor, true, true);
            }

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    isDisplayingOrHidingBlackboard = false;
                });
            })).Start();

            CheckColorTheme(true);
        }

        private void ApplyToolModeChange(ToolMode mode)
        {
            switch (mode)
            {
                case ToolMode.Cursor:
                    ApplyCursorToolMode();
                    break;
                case ToolMode.Pen:
                    ApplyPenToolMode();
                    break;
                case ToolMode.Eraser:
                    ApplyPointEraserToolMode();
                    break;
                case ToolMode.EraserByStrokes:
                    ApplyStrokeEraserToolMode();
                    break;
                case ToolMode.Select:
                    ApplySelectionToolMode();
                    break;
            }
        }

        private async void ApplyCursorToolMode()
        {
            InputStateViewModel.SetActiveShapeTool(ShapeToolKind.None, false);
            InputStateViewModel.SetForceEraser(false, false);
            SetCursorInteractionMode();

            if (inkCanvas.Strokes.Count > 0
                && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                if (IsPresentationSlideShowRunning)
                {
                    SavePPTScreenshot($"{CurrentPresentationName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                }
                else
                {
                    SaveScreenshot(true);
                }
            }

            if (!IsPresentationSlideShowRunning)
            {
                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    inkCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    inkCanvas.IsHitTestVisible = false;
                    inkCanvas.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                {
                    inkCanvas.Visibility = Visibility.Visible;
                    inkCanvas.IsHitTestVisible = true;
                }
                else if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    inkCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    inkCanvas.IsHitTestVisible = false;
                    inkCanvas.Visibility = Visibility.Visible;
                }
            }

            Main_Grid.Background = Brushes.Transparent;

            GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
            inkCanvas.Select(new StrokeCollection());
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (ShellViewModel.IsBlackboardMode)
            {
                SaveStrokes();
                RestoreStrokes(true);
            }

            CheckEnableTwoFingerGestureBtnVisibility(false);

            StackPanelCanvasControls.Visibility = Visibility.Collapsed;

            if (!isFloatingBarFolded)
            {
                HideSubPanels("cursor", true);
                await Task.Delay(50);
                ViewboxFloatingBarMarginAnimation();
            }
        }

        private void ApplyPenToolMode()
        {
            if (isFloatingBarFolded)
            {
                UnFoldFloatingBar_MouseUp(LeftSidePanel, null);
            }

            ExitShapeDrawingMode(false);
            forceEraser = false;
            ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            inkCanvas.IsHitTestVisible = true;
            inkCanvas.Visibility = Visibility.Visible;

            GridBackgroundCoverHolder.Visibility = Visibility.Visible;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            StackPanelCanvasControls.Visibility = Visibility.Visible;

            CheckEnableTwoFingerGestureBtnVisibility(true);
            ColorSwitchCheck();
            HideSubPanels("pen", true);
        }

        private void ApplyPointEraserToolMode()
        {
            forceEraser = true;
            forcePointEraser = true;
            double k = 1;
            switch (Settings.Canvas.EraserSize)
            {
                case 0:
                    k = 0.5;
                    break;
                case 1:
                    k = 0.8;
                    break;
                case 3:
                    k = 1.25;
                    break;
                case 4:
                    k = 1.8;
                    break;
            }

            ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByPoint, k * 90);
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraser");
        }

        private void ApplyStrokeEraserToolMode()
        {
            forceEraser = true;
            forcePointEraser = false;

            ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByStroke, strokeEraserDiameter: 5);
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraserByStrokes");
        }

        private void ApplySelectionToolMode()
        {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                if (inkCanvas.GetSelectedStrokes().Count == inkCanvas.Strokes.Count
                    && inkCanvas.GetSelectedElements().Count == inkCanvas.Children.Count)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    inkCanvas.EditingMode = InkCanvasEditingMode.Select;
                }
                else
                {
                    StrokeCollection selectedStrokes = new StrokeCollection();
                    foreach (Stroke stroke in inkCanvas.Strokes)
                    {
                        if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                        {
                            selectedStrokes.Add(stroke);
                        }
                    }
                    var selectedElements = InkCanvasElementsHelper.GetAllElements(inkCanvas);
                    inkCanvas.Select(selectedStrokes, selectedElements);
                }
            }
            else
            {
                ApplyCanvasInteractionMode(CanvasInteractionMode.Select);
            }

            HideSubPanels("select");
        }

        private void ApplySubPanelState(SubPanelKind panel)
        {
            isApplyingShellSubPanelState = true;
            try
            {
                SetPanelVisibility(BorderTools, panel == SubPanelKind.Tools);
                SetPanelVisibility(BoardBorderTools, panel == SubPanelKind.Tools);
                SetPanelVisibility(PenPalette, panel == SubPanelKind.PenPalette);
                SetPanelVisibility(BoardPenPalette, panel == SubPanelKind.PenPalette);
                SetSettingsPanelVisibility(BorderSettings, panel == SubPanelKind.Settings);
                SetPanelVisibility(TwoFingerGestureBorder, panel == SubPanelKind.TwoFingerGesture);
                SetPanelVisibility(BoardTwoFingerGestureBorder, panel == SubPanelKind.TwoFingerGesture);
                SetPanelVisibility(BorderDrawShape, panel == SubPanelKind.ShapePanel);
                SetPanelVisibility(BoardBorderDrawShape, panel == SubPanelKind.ShapePanel);
                SetPanelVisibility(BoardDeleteIcon, panel == SubPanelKind.DeletePanel);
            }
            finally
            {
                isApplyingShellSubPanelState = false;
            }
        }

        private static void SetPanelVisibility(FrameworkElement element, bool isVisible)
        {
            if (isVisible)
            {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(element);
            }
            else
            {
                AnimationsHelper.HideWithSlideAndFade(element);
            }
        }

        private static void SetSettingsPanelVisibility(FrameworkElement element, bool isVisible)
        {
            if (isVisible)
            {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(element, 0.5);
            }
            else
            {
                AnimationsHelper.HideWithSlideAndFade(element, 0.5);
            }
        }
    }
}
