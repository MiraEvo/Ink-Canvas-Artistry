using Ink_Canvas.Features.Shell;
using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : IToolbarUiHost
    {
        int BoundsWidth = 5;

        private bool isDragDropInEffect
        {
            get => floatingBarLayoutState.IsDragDropInEffect;
            set => floatingBarLayoutState.IsDragDropInEffect = value;
        }

        private Point pos
        {
            get => floatingBarLayoutState.CurrentPointerPosition;
            set => floatingBarLayoutState.CurrentPointerPosition = value;
        }

        private Point downPos
        {
            get => floatingBarLayoutState.MouseDownPosition;
            set => floatingBarLayoutState.MouseDownPosition = value;
        }

        private Point pointDesktop
        {
            get => floatingBarLayoutState.DesktopPosition;
            set => floatingBarLayoutState.DesktopPosition = value;
        }

        private Point pointPPT
        {
            get => floatingBarLayoutState.PresentationPosition;
            set => floatingBarLayoutState.PresentationPosition = value;
        }

        private bool shouldRestoreDefaultDesktopFloatingBarPosition
        {
            get => floatingBarLayoutState.ShouldRestoreDefaultDesktopPosition;
            set => floatingBarLayoutState.ShouldRestoreDefaultDesktopPosition = value;
        }

        private bool isViewboxFloatingBarMarginAnimationRunning
        {
            get => floatingBarLayoutState.IsMarginAnimationRunning;
            set => floatingBarLayoutState.IsMarginAnimationRunning = value;
        }

        bool IToolbarUiHost.IsPresentationSlideShowRunning => IsPresentationSlideShowRunning;

        bool IToolbarUiHost.IsCanvasControlsVisible => StackPanelCanvasControls.Visibility != Visibility.Collapsed;

        Task IToolbarUiHost.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay) => ViewboxFloatingBarMarginAnimationAfterDelayAsync(delay);

        void IToolbarUiHost.HideSubPanels(string? mode, bool autoAlignCenter) => HideSubPanels(mode, autoAlignCenter);

        void IToolbarUiHost.HideSubPanelsImmediately() => HideSubPanelsImmediately();

        void IToolbarUiHost.BeginFloatingBarDrag(Point pointerPosition) => BeginFloatingBarDrag(pointerPosition);

        void IToolbarUiHost.UpdateFloatingBarDrag(Point pointerPosition) => UpdateFloatingBarDrag(pointerPosition);

        void IToolbarUiHost.EndFloatingBarDrag(Point? pointerPosition) => EndFloatingBarDrag(pointerPosition);

        Task IToolbarUiHost.FoldFloatingBarAsync(bool userInitiated) => FoldFloatingBarAsync(userInitiated);

        Task IToolbarUiHost.UnfoldFloatingBarAsync(bool userInitiated) => UnfoldFloatingBarAsync(userInitiated);

        void IToolbarUiHost.DeleteSelectionOrClear() => DeleteSelectionOrClear();

        void IToolbarUiHost.UndoHistory() => UndoHistory();

        void IToolbarUiHost.RedoHistory() => RedoHistory();

        void IToolbarUiHost.ClearCanvas() => PerformClearCanvas();

        void IToolbarUiHost.ToggleBlackboardSession() => ToggleBlackboardSession();

        void IToolbarUiHost.ToggleInkCanvasVisibility() => ToggleInkCanvasVisibility();

        void IToolbarUiHost.ToggleColorTheme() => ToggleColorTheme();

        void IToolbarUiHost.ToggleSingleFingerDragMode() => ToggleSingleFingerDragMode();

        void IToolbarUiHost.ApplyCurrentWorkspaceVisualState() => ApplyWorkspaceVisualStateCore(ShellViewModel.WorkspaceMode);

        void IToolbarUiHost.ExitBlackboardSession() => ExitBlackboardSession();

        private void CheckEnableTwoFingerGestureBtnColorPrompt()
        {
            if (Settings.Gesture.IsEnableMultiTouchMode)
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 0.5;
                EnableTwoFingerGestureBtn.Opacity = 0.5;
            }
            else
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 1;
                EnableTwoFingerGestureBtn.Opacity = Settings.Gesture.IsEnableTwoFingerGesture ? 1 : 0.5;
            }
        }

        private void CheckEnableTwoFingerGestureBtnVisibility(bool isVisible)
        {
            if (StackPanelCanvasControls.Visibility != Visibility.Visible
                || BorderFloatingBarMainControls.Visibility != Visibility.Visible)
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
            else if (isVisible)
            {
                EnableTwoFingerGestureBorder.Visibility = IsPresentationSlideShowRunning
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            else
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void RequestDefaultDesktopFloatingBarPosition()
        {
            shouldRestoreDefaultDesktopFloatingBarPosition = true;
            pointPPT = new Point(-1, -1);
        }

        private void BeginFloatingBarDrag(Point pointerPosition)
        {
            if (isViewboxFloatingBarMarginAnimationRunning)
            {
                ViewboxFloatingBar.BeginAnimation(FrameworkElement.MarginProperty, null);
                isViewboxFloatingBarMarginAnimationRunning = false;
            }

            isDragDropInEffect = true;
            pos = pointerPosition;
            downPos = pointerPosition;
            GridForFloatingBarDraging.Visibility = Visibility.Visible;
            SymbolIconEmoji1.Width = 0;
            SymbolIconEmoji2.Width = 28;
        }

        private void UpdateFloatingBarDrag(Point pointerPosition)
        {
            if (!isDragDropInEffect)
            {
                return;
            }

            double xPos = pointerPosition.X - pos.X + ViewboxFloatingBar.Margin.Left;
            double yPos = pointerPosition.Y - pos.Y + ViewboxFloatingBar.Margin.Top;
            ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);

            pos = pointerPosition;
            if (IsPresentationSlideShowRunning)
            {
                pointPPT = new Point(xPos, yPos);
            }
            else
            {
                pointDesktop = new Point(xPos, yPos);
            }
        }

        private void EndFloatingBarDrag(Point? pointerPosition)
        {
            isDragDropInEffect = false;
            if (!pointerPosition.HasValue
                || Math.Abs(downPos.X - pointerPosition.Value.X) <= 10 && Math.Abs(downPos.Y - pointerPosition.Value.Y) <= 10)
            {
                ToggleFloatingBarMainControls();
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
            SymbolIconEmoji1.Width = 28;
            SymbolIconEmoji2.Width = 0;
        }

        private void ToggleFloatingBarMainControls()
        {
            if (BorderFloatingBarMainControls.Visibility == Visibility.Visible)
            {
                BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                CheckEnableTwoFingerGestureBtnVisibility(false);
            }
            else
            {
                BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                CheckEnableTwoFingerGestureBtnVisibility(true);
            }
        }

        private void HideSubPanelsImmediately()
        {
            if (!isApplyingShellSubPanelState)
            {
                ShellViewModel.SetActiveSubPanel(SubPanelKind.None, false);
            }

            BorderTools.Visibility = Visibility.Collapsed;
            BoardBorderTools.Visibility = Visibility.Collapsed;
            PenPalette.Visibility = Visibility.Collapsed;
            BoardPenPalette.Visibility = Visibility.Collapsed;
            BoardDeleteIcon.Visibility = Visibility.Collapsed;
            BorderSettings.Visibility = Visibility.Collapsed;
        }

        private void HideSubPanels(string? mode = null, bool autoAlignCenter = false)
        {
            _ = HideSubPanelsAsync(mode, autoAlignCenter);
        }

        private async Task HideSubPanelsAsync(string? mode = null, bool autoAlignCenter = false)
        {
            if (!isApplyingShellSubPanelState)
            {
                ShellViewModel.SetActiveSubPanel(SubPanelKind.None, false);
            }

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(PenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardDeleteIcon);
            AnimationsHelper.HideWithSlideAndFade(BorderSettings, 0.5);
            AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
            AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
            }

            if (mode != null)
            {
                UpdateToolSelectionHighlights(mode);
                if (autoAlignCenter)
                {
                    await Task.Delay(50);
                    ViewboxFloatingBarMarginAnimation();
                }
            }

            await Task.Delay(150);
            isHidingSubPanelsWhenInking = false;
        }

        private void UpdateToolSelectionHighlights(string mode)
        {
            if (mode != "clear")
            {
                Pen_Icon.Background = null;
                BoardPen.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
                BoardPen.Opacity = 1;
                Eraser_Icon.Background = null;
                BoardEraser.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
                BoardEraser.Opacity = 1;
                SymbolIconSelect.Background = null;
                BoardSelect.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
                BoardSelect.Opacity = 1;
                EraserByStrokes_Icon.Background = null;
                BoardEraserByStrokes.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
                BoardEraserByStrokes.Opacity = 1;
            }

            ImageBrush highlightBrush = CreateToolSelectionHighlightBrush();
            if (mode == "pen" || mode == "color")
            {
                Pen_Icon.Background = highlightBrush;
                BoardPen.Background = CreateToolSelectionHighlightBrush();
                BoardPen.Opacity = 0.99;
                return;
            }

            if (mode == "eraser")
            {
                Eraser_Icon.Background = highlightBrush;
                BoardEraser.Background = CreateToolSelectionHighlightBrush();
                BoardEraser.Opacity = 0.99;
                return;
            }

            if (mode == "eraserByStrokes")
            {
                EraserByStrokes_Icon.Background = highlightBrush;
                BoardEraserByStrokes.Background = CreateToolSelectionHighlightBrush();
                BoardEraserByStrokes.Opacity = 0.99;
                return;
            }

            if (mode == "select")
            {
                BoardSelect.Background = highlightBrush;
                SymbolIconSelect.Background = CreateToolSelectionHighlightBrush();
                SymbolIconSelect.Opacity = 0.99;
            }
        }

        private static ImageBrush CreateToolSelectionHighlightBrush()
        {
            return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/Icons-png/check-box-background.png")))
            {
                Opacity = 0.5
            };
        }

        private void ViewboxFloatingBarMarginAnimation()
        {
            _ = ViewboxFloatingBarMarginAnimationAsync();
        }

        private async Task ViewboxFloatingBarMarginAnimationAfterDelayAsync(TimeSpan delay)
        {
            await Task.Delay(delay);
            await ViewboxFloatingBarMarginAnimationAsync();
        }

        private async Task ViewboxFloatingBarMarginAnimationAsync()
        {
            double marginFromEdge = Settings.Appearance.FloatingBarBottomMargin;
            if (isFloatingBarFolded)
            {
                marginFromEdge = -100;
            }
            else if (IsPresentationSlideShowRunning)
            {
                marginFromEdge = 60;
            }

            marginFromEdge *= Settings.Appearance.FloatingBarScale / 100;
            await Dispatcher.InvokeAsync(() =>
            {
                if (!Topmost)
                {
                    marginFromEdge = -60;
                }
                else
                {
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                }

                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1;
                double dpiScaleY = 1;
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX;
                double screenHeight = screen.Bounds.Height / dpiScaleY;
                pos = new Point(
                    (screenWidth - ViewboxFloatingBar.ActualWidth * ViewboxFloatingBarScaleTransform.ScaleX) / 2,
                    screenHeight - marginFromEdge * (ViewboxFloatingBarScaleTransform.ScaleY == 1 ? 1 : 0.9));

                if (marginFromEdge != -60)
                {
                    if (IsPresentationSlideShowRunning)
                    {
                        if (pointPPT.X != -1 || pointPPT.Y != -1)
                        {
                            if (Math.Abs(pointPPT.Y - pos.Y) > 50)
                            {
                                pos = pointPPT;
                            }
                            else
                            {
                                pointPPT = pos;
                            }
                        }
                    }
                    else
                    {
                        if (shouldRestoreDefaultDesktopFloatingBarPosition)
                        {
                            pointDesktop = pos;
                            shouldRestoreDefaultDesktopFloatingBarPosition = false;
                        }
                        else if (pointDesktop.X != -1 || pointDesktop.Y != -1)
                        {
                            if (Math.Abs(pointDesktop.Y - pos.Y) > 50)
                            {
                                pos = pointDesktop;
                            }
                            else
                            {
                                pointDesktop = pos;
                            }
                        }
                    }
                }

                ThicknessAnimation marginAnimation = new()
                {
                    Duration = TimeSpan.FromSeconds(0.5),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, -2000, -200),
                    EasingFunction = new CircleEase()
                };
                ViewboxFloatingBar.BeginAnimation(FrameworkElement.MarginProperty, marginAnimation);
            });

            await Task.Delay(200);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
                if (!Topmost)
                {
                    ViewboxFloatingBar.Visibility = Visibility.Hidden;
                }
            });
        }

        private void CollapseBorderDrawShape(bool isLongPressSelected = false)
        {
            if (!isApplyingShellSubPanelState && ShellViewModel.IsShapePanelOpen)
            {
                ShellViewModel.SetActiveSubPanel(SubPanelKind.None, false);
            }

            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
        }

        private void DrawShapePromptToPen()
        {
            if (isLongPressSelected)
            {
                HideSubPanels("pen");
                return;
            }

            HideSubPanels(StackPanelCanvasControls.Visibility == Visibility.Visible ? "pen" : "cursor");
        }

        private void DeleteSelectionOrClear()
        {
            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            var selectedElements = new List<UIElement>(inkCanvas.GetSelectedElements());
            if (selectedStrokes.Count > 0 || selectedElements.Count > 0)
            {
                inkCanvas.Strokes.Remove(selectedStrokes);
                foreach (UIElement element in selectedElements)
                {
                    inkCanvas.Children.Remove(element);
                    inkHistoryCoordinator?.CommitElementInsert(element, true);
                }

                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                return;
            }

            if (inkCanvas.Strokes.Count > 0 || inkCanvas.Children.Count > 0)
            {
                if (Settings.Automation.IsAutoSaveStrokesAtClear
                    && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    SaveScreenshotForCurrentContext();
                }

                PerformClearCanvas();
            }
        }

        private void UndoHistory()
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            inkHistoryCoordinator?.Undo();
        }

        private void RedoHistory()
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            inkHistoryCoordinator?.Redo();
        }

        private void PerformClearCanvas()
        {
            forceEraser = false;

            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                if (Pen_Icon.Background == null && StackPanelCanvasControls.Visibility == Visibility.Visible)
                {
                    toolbarExperienceCoordinator.HandlePenRequested();
                }
            }
            else if (Pen_Icon.Background == null)
            {
                toolbarExperienceCoordinator.HandlePenRequested();
            }

            if (inkCanvas.Strokes.Count != 0)
            {
                int whiteboardIndex = ShellViewModel.IsDesktopAnnotationMode ? 0 : CurrentWhiteboardIndex;
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();
            }

            ClearStrokes(false);
            CancelSingleFingerDragMode();
        }

        private void ToggleBlackboardSession()
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }
            else
            {
                EnterBlackboardSession();
            }
        }

        private void ToggleInkCanvasVisibility()
        {
            if (IsCanvasWritingVisible)
            {
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;
                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (!IsPresentationSlideShowRunning)
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode && inkCanvas.Strokes.Count > 0)
                    {
                        if (Settings.Automation.IsAutoSaveStrokesAtClear
                            && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                        {
                            SaveScreenshot(true);
                        }

                        PerformClearCanvas();
                    }

                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    if (isLoaded
                        && Settings.Automation.IsAutoClearWhenExitingWritingMode
                        && !Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint
                        && inkCanvas.Strokes.Count > 0)
                    {
                        if (Settings.Automation.IsAutoSaveStrokesAtClear
                            && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                        {
                            SaveScreenshot(true);
                        }

                        PerformClearCanvas();
                    }

                    inkCanvas.Visibility = Visibility.Visible;
                    inkCanvas.IsHitTestVisible = true;
                }

                Main_Grid.Background = Brushes.Transparent;
                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

                if (ShellViewModel.IsBlackboardMode)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }
            }

            if (!IsCanvasWritingVisible)
            {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                CheckEnableTwoFingerGestureBtnVisibility(false);
                HideSubPanels("cursor");
            }
            else
            {
                AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);
            }

            SyncWorkspaceCanvasVisibility();
        }

        private void ToggleColorTheme()
        {
            isUselightThemeColor = !isUselightThemeColor;
            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                isDesktopUselightThemeColor = isUselightThemeColor;
            }

            inkPaletteCoordinator?.HandlePaletteThemeRefresh();
        }

        private void ToggleSingleFingerDragMode()
        {
            isSingleFingerDragMode = !isSingleFingerDragMode;
        }

        private void CancelSingleFingerDragMode()
        {
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                CollapseBorderDrawShape();
            }

            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            if (isSingleFingerDragMode)
            {
                ToggleSingleFingerDragMode();
            }

            isLongPressSelected = false;
        }

        private async Task FoldFloatingBarAsync(bool userInitiated)
        {
            ConfigureFoldRequest(userInitiated);
            if (isFloatingBarChangingHideMode)
            {
                return;
            }

            isFloatingBarChangingHideMode = true;
            try
            {
                await ExecuteFloatingBarTransitionAsync(
                    () =>
                    {
                        HideSubPanelsImmediately();
                        isFloatingBarFolded = true;

                        if (ShellViewModel.IsBlackboardMode)
                        {
                            ToggleBlackboardSession();
                        }

                        if (StackPanelCanvasControls.Visibility == Visibility.Visible
                            && foldFloatingBarByUser
                            && inkCanvas.Strokes.Count > 2)
                        {
                            ShowNotificationAsync("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                        }

                        DeleteSelectionOrClear();
                        ShellViewModel.SetToolMode(ToolMode.Cursor, true, true);
                        _ = AnimateSidePanelMarginsAsync(-16);
                    },
                    () =>
                    {
                        HidePresentationNavigation();
                        ViewboxFloatingBarMarginAnimation();
                        HideSubPanels("cursor");
                        _ = AnimateSidePanelMarginsAsync(-16);
                    });
            }
            finally
            {
                isFloatingBarChangingHideMode = false;
            }
        }

        private async Task UnfoldFloatingBarAsync(bool userInitiated)
        {
            ConfigureUnfoldRequest(userInitiated);
            if (isFloatingBarChangingHideMode)
            {
                return;
            }

            isFloatingBarChangingHideMode = true;
            try
            {
                await ExecuteFloatingBarTransitionAsync(
                    () => isFloatingBarFolded = false,
                    () =>
                    {
                        ShowPresentationNavigationIfNeeded();
                        ViewboxFloatingBarMarginAnimation();
                        _ = AnimateSidePanelMarginsAsync(-40);
                    });
            }
            finally
            {
                isFloatingBarChangingHideMode = false;
            }
        }

        private void ConfigureFoldRequest(bool userInitiated)
        {
            if (userInitiated)
            {
                foldFloatingBarByUser = true;
                AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(false);
            }
            else
            {
                foldFloatingBarByUser = false;
                AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(true);
            }

            unfoldFloatingBarByUser = false;
        }

        private void ConfigureUnfoldRequest(bool userInitiated)
        {
            unfoldFloatingBarByUser = userInitiated && !IsPresentationSlideShowRunning;
            foldFloatingBarByUser = false;
            AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(false);
        }

        private async Task ExecuteFloatingBarTransitionAsync(Action startAction, Action finishAction)
        {
            await Dispatcher.InvokeAsync(startAction);
            await Task.Delay(500);
            await Dispatcher.InvokeAsync(finishAction);
        }

        private void HidePresentationNavigation()
        {
            PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
            PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
            PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
            PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
        }

        private void ShowPresentationNavigationIfNeeded()
        {
            if (!IsPresentationSlideShowRunning)
            {
                return;
            }

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

        private async Task AnimateSidePanelMarginsAsync(int marginFromEdge)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (marginFromEdge == -16)
                {
                    LeftSidePanel.Visibility = Visibility.Visible;
                }

                ThicknessAnimation leftSidePanelMarginAnimation = new()
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = LeftSidePanel.Margin,
                    To = new Thickness(marginFromEdge, 0, 0, -150)
                };
                ThicknessAnimation rightSidePanelMarginAnimation = new()
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = RightSidePanel.Margin,
                    To = new Thickness(0, 0, marginFromEdge, -150)
                };

                LeftSidePanel.BeginAnimation(FrameworkElement.MarginProperty, leftSidePanelMarginAnimation);
                RightSidePanel.BeginAnimation(FrameworkElement.MarginProperty, rightSidePanelMarginAnimation);
            });

            await Task.Delay(600);

            await Dispatcher.InvokeAsync(() =>
            {
                LeftSidePanel.Margin = new Thickness(marginFromEdge, 0, 0, -150);
                RightSidePanel.Margin = new Thickness(0, 0, marginFromEdge, -150);
                if (marginFromEdge == -40)
                {
                    LeftSidePanel.Visibility = Visibility.Collapsed;
                }
            });
        }

    }
}
