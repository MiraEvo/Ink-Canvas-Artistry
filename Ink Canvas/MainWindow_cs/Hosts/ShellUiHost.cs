using Ink_Canvas.Features.Shell;
using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : IShellUiHost
    {
        bool IShellUiHost.IsCanvasWritingVisible => IsCanvasWritingVisible;

        bool IShellUiHost.IsPresentationSlideShowRunning => IsPresentationSlideShowRunning;

        bool IShellUiHost.IsBlackboardMode => ShellViewModel.IsBlackboardMode;

        bool IShellUiHost.IsFloatingBarFolded => isFloatingBarFolded;

        bool IShellUiHost.IsSelectionEditingMode => inkCanvas.EditingMode == InkCanvasEditingMode.Select;

        bool IShellUiHost.IsPenToolActive => Pen_Icon.Background != null;

        int IShellUiHost.CurrentInkStrokeCount => inkCanvas.Strokes.Count;

        Task IShellUiHost.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay) => ViewboxFloatingBarMarginAnimationAfterDelayAsync(delay);

        void IShellUiHost.AnimateFloatingBarMargin() => ViewboxFloatingBarMarginAnimation();

        void IShellUiHost.HidePresentationNavigation() => HidePresentationNavigation();

        void IShellUiHost.ShowPresentationNavigationIfEnabled() => ShowPresentationNavigationIfNeeded();

        void IShellUiHost.HideSubPanelsImmediately() => HideSubPanelsImmediately();

        void IShellUiHost.HideSubPanels(string? mode, bool autoAlignCenter) => HideSubPanels(mode, autoAlignCenter);

        void IShellUiHost.ApplySubPanelState(SubPanelKind panel) => ApplySubPanelStateCore(panel);

        void IShellUiHost.ApplyWorkspaceVisualState(WorkspaceMode workspaceMode) => ApplyWorkspaceVisualStateCore(workspaceMode);

        void IShellUiHost.ApplyCursorToolModeVisuals() => ApplyCursorToolModeCore();

        void IShellUiHost.ApplyPenToolModeVisuals() => ApplyPenToolModeCore();

        void IShellUiHost.ApplyPointEraserToolModeVisuals(double eraserDiameter) => ApplyPointEraserToolModeCore(eraserDiameter);

        void IShellUiHost.ApplyStrokeEraserToolModeVisuals() => ApplyStrokeEraserToolModeCore();

        void IShellUiHost.ApplySelectionToolModeVisuals() => ApplySelectionToolModeCore();

        void IShellUiHost.CompleteBlackboardTransition() => _ = CompleteBlackboardTransitionAsync();

        void IShellUiHost.ApplyShellThemeRefresh() => ApplyShellThemeRefresh();

        void IShellUiHost.SaveScreenshotForCurrentContext() => SaveScreenshotForCurrentContext();

        void IShellUiHost.RequestDefaultDesktopFloatingBarPosition() => RequestDefaultDesktopFloatingBarPosition();

        private void ApplyCursorToolModeCore()
        {
            SetCursorInteractionMode();

            if (inkCanvas.Strokes.Count > 0
                && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshotForCurrentContext();
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
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await Dispatcher.InvokeAsync(ViewboxFloatingBarMarginAnimation);
                });
            }
        }

        private void ApplyPenToolModeCore()
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
            inkPaletteCoordinator?.HandlePaletteThemeRefresh();
            HideSubPanels("pen", true);
        }

        private void ApplyPointEraserToolModeCore(double eraserDiameter)
        {
            forceEraser = true;
            forcePointEraser = true;

            ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByPoint, eraserDiameter);
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
            HideSubPanels("eraser");
        }

        private void ApplyStrokeEraserToolModeCore()
        {
            forceEraser = true;
            forcePointEraser = false;

            ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByStroke, strokeEraserDiameter: 5);
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
            HideSubPanels("eraserByStrokes");
        }

        private void ApplySelectionToolModeCore()
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
                    StrokeCollection selectedStrokes = new();
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

        private void ApplySubPanelStateCore(SubPanelKind panel)
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

        private void ApplyShellThemeRefresh()
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            inkPaletteCoordinator?.HandlePaletteThemeRefresh(true);
        }

        private void SaveScreenshotForCurrentContext()
        {
            if (IsPresentationSlideShowRunning)
            {
                SavePPTScreenshot($"{CurrentPresentationName}/{CurrentPresentationSlideIndex}_{DateTime.Now:HH-mm-ss}");
            }
            else
            {
                SaveScreenshot(true);
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

        private async Task CompleteBlackboardTransitionAsync()
        {
            await Task.Delay(200);
            await Dispatcher.InvokeAsync(() =>
            {
                isDisplayingOrHidingBlackboard = false;
            });
        }
    }
}
