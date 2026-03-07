using Ink_Canvas.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using iNKORE.UI.WPF.Modern;
using System.Threading;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
using System.Diagnostics;
using iNKORE.UI.WPF.Modern.Controls;
using System.Collections.Generic;
using Ink_Canvas.ViewModels;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region TwoFingZoomBtn

        private void TwoFingerGestureBorder_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.ToggleTwoFingerPanelCommand.Execute(null);
        }

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
                if (Settings.Gesture.IsEnableTwoFingerGesture)
                {
                    EnableTwoFingerGestureBtn.Opacity = 1;
                }
                else
                {
                    EnableTwoFingerGestureBtn.Opacity = 0.5;
                }
            }
        }

        private void CheckEnableTwoFingerGestureBtnVisibility(bool isVisible)
        {
            if (StackPanelCanvasControls.Visibility != Visibility.Visible
                || BorderFloatingBarMainControls.Visibility != Visibility.Visible)
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
            else if (isVisible == true)
            {
                if (IsPresentationSlideShowRunning) EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                else EnableTwoFingerGestureBorder.Visibility = Visibility.Visible;
            }
            else EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
        }

        #endregion TwoFingZoomBtn

        #region Drag

        bool isDragDropInEffect = false;
        Point pos = new Point();
        Point downPos = new Point();
        Point pointDesktop = new Point(-1, -1); //用于记录上次在桌面时的坐标
        Point pointPPT = new Point(-1, -1); //用于记录上次在PPT中的坐标
        bool shouldRestoreDefaultDesktopFloatingBarPosition;

        private void RequestDefaultDesktopFloatingBarPosition()
        {
            shouldRestoreDefaultDesktopFloatingBarPosition = true;
            pointPPT = new Point(-1, -1);
        }

        void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                double xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                double yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);

                pos = e.GetPosition(null);
                if (IsPresentationSlideShowRunning)
                {
                    pointPPT = new Point(xPos, yPos);
                }
                else
                {
                    pointDesktop = new Point(xPos, yPos);
                }
            }
        }

        void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isViewboxFloatingBarMarginAnimationRunning)
            {
                ViewboxFloatingBar.BeginAnimation(FrameworkElement.MarginProperty, null);
                isViewboxFloatingBarMarginAnimationRunning = false;
            }
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;
            SymbolIconEmoji1.Width = 0;
            SymbolIconEmoji2.Width = 28;
        }

        void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || Math.Abs(downPos.X - e.GetPosition(null).X) <= 10 && Math.Abs(downPos.Y - e.GetPosition(null).Y) <= 10)
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

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
            SymbolIconEmoji1.Width = 28;
            SymbolIconEmoji2.Width = 0;
        }

        #endregion

        private void HideSubPanelsImmediately()
        {
            if (!isApplyingShellSubPanelState)
            {
                ShellViewModel.SetActiveSubPanel(SubPanelKind.None, false);
            }

            BorderTools.Visibility = Visibility.Collapsed;
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

                if (autoAlignCenter) // 控制居中
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

        private void SymbolIconUndo_Click(object sender, RoutedEventArgs e)
        {
            if (!Icon_Undo.IsEnabled) return;
            BtnUndo_Click(null, null);
            HideSubPanels();
        }

        private void SymbolIconRedo_Click(object sender, RoutedEventArgs e)
        {
            if (!Icon_Redo.IsEnabled) return;
            BtnRedo_Click(null, null);
            HideSubPanels();
        }

        private void SymbolIconCursor_Click(object sender, RoutedEventArgs e)
        {
            _ = SwitchToCursorModeAsync();
        }

        private async Task SwitchToCursorModeAsync()
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ImageBlackboard_Click(null, null);
                return;
            }

            ShellViewModel.SetToolMode(ToolMode.Cursor, true, true);

            if (IsPresentationSlideShowRunning)
            {
                await Task.Delay(100);
                ViewboxFloatingBarMarginAnimation();
            }
        }

        private void SymbolIconDelete_MouseUp(object sender, RoutedEventArgs e)
        {
            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            var selectedElements = new List<UIElement>(inkCanvas.GetSelectedElements());
            if (selectedStrokes.Count > 0 || selectedElements.Count > 0)
            {
                inkCanvas.Strokes.Remove(inkCanvas.GetSelectedStrokes());
                foreach(UIElement element in selectedElements)
                {
                    inkCanvas.Children.Remove(element);
                    timeMachine.CommitElementInsertHistory(element, true);
                }
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else if (inkCanvas.Strokes.Count > 0 || inkCanvas.Children.Count > 0)
            {
                if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    if (IsPresentationSlideShowRunning)
                        SavePPTScreenshot($"{CurrentPresentationName}/{CurrentPresentationSlideIndex}_{DateTime.Now:HH-mm-ss}");
                    else
                        SaveScreenshot(true);
                }
                BtnClear_Click(null, null);
            }
        }

        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.OpenSettingsPanelCommand.Execute(null);
        }

        private void SymbolIconSelect_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.SetToolMode(ToolMode.Select, true, true);
        }

        private void SymbolIconScreenshot_Click(object sender, RoutedEventArgs e)
        {
            _ = SaveToolbarScreenshotAsync();
        }

        private async Task SaveToolbarScreenshotAsync()
        {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            SaveScreenShotToDesktop();
        }

        private void ImageBlackboard_Click(object sender, RoutedEventArgs e)
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

        private void ImageCountdownTimer_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            new CountdownTimerWindow().Show();
        }

        private void OperatingGuideWindowIcon_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            new OperatingGuideWindow().Show();
        }

        private void SymbolIconRand_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            new RandWindow().Show();
        }

        private void SymbolIconRandOne_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new RandWindow(true).ShowDialog();
        }

        private void GridInkReplayButton_Click(object sender, RoutedEventArgs e)
        {
            _ = StartInkReplayAsync();
        }

        private async Task StartInkReplayAsync()
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            CollapseBorderDrawShape();

            CancelInkReplay(restoreCanvas: false);
            InkCanvasForInkReplay.Visibility = Visibility.Visible;
            inkCanvas.Visibility = Visibility.Collapsed;
            InkCanvasForInkReplay.Strokes.Clear();
            StrokeCollection strokes = inkCanvas.Strokes.Clone();
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                strokes = inkCanvas.GetSelectedStrokes().Clone();
            }

            CancellationTokenSource replayCancellationTokenSource = BeginInkReplay();
            try
            {
                await ReplayStrokesAsync(strokes, replayCancellationTokenSource.Token);
                await Task.Delay(100, replayCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                CompleteInkReplay(replayCancellationTokenSource);
            }
        }

        private CancellationTokenSource inkReplayCancellationTokenSource;

        private CancellationTokenSource BeginInkReplay()
        {
            inkReplayCancellationTokenSource?.Cancel();
            inkReplayCancellationTokenSource?.Dispose();
            inkReplayCancellationTokenSource = new CancellationTokenSource();
            return inkReplayCancellationTokenSource;
        }

        private async Task ReplayStrokesAsync(StrokeCollection strokes, CancellationToken cancellationToken)
        {
            foreach (Stroke stroke in strokes)
            {
                int batchSize = stroke.StylusPoints.Count == 629 ? 50 : 1;
                await ReplayStrokeAsync(stroke, batchSize, cancellationToken);
            }
        }

        private async Task ReplayStrokeAsync(Stroke stroke, int batchSize, CancellationToken cancellationToken)
        {
            StylusPointCollection stylusPoints = new StylusPointCollection();
            Stroke replayStroke = null;
            int pointsSinceDelay = 0;

            foreach (StylusPoint stylusPoint in stroke.StylusPoints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (replayStroke != null && InkCanvasForInkReplay.Strokes.Contains(replayStroke))
                {
                    InkCanvasForInkReplay.Strokes.Remove(replayStroke);
                }

                stylusPoints.Add(stylusPoint);
                replayStroke = new Stroke(stylusPoints.Clone())
                {
                    DrawingAttributes = stroke.DrawingAttributes.Clone()
                };
                InkCanvasForInkReplay.Strokes.Add(replayStroke);

                if (++pointsSinceDelay >= batchSize)
                {
                    pointsSinceDelay = 0;
                    await Task.Delay(10, cancellationToken);
                }
            }
        }

        private void CancelInkReplay(bool restoreCanvas)
        {
            inkReplayCancellationTokenSource?.Cancel();
            if (restoreCanvas)
            {
                ResetInkReplayCanvasState();
            }
        }

        private void CompleteInkReplay(CancellationTokenSource replayCancellationTokenSource)
        {
            if (!ReferenceEquals(inkReplayCancellationTokenSource, replayCancellationTokenSource))
            {
                replayCancellationTokenSource.Dispose();
                return;
            }

            inkReplayCancellationTokenSource.Dispose();
            inkReplayCancellationTokenSource = null;
            ResetInkReplayCanvasState();
        }

        private void ResetInkReplayCanvasState()
        {
            InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
            inkCanvas.Visibility = Visibility.Visible;
        }

        private void InkCanvasForInkReplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                CancelInkReplay(restoreCanvas: true);
            }
        }

        private void SymbolIconTools_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.ToggleToolsPanelCommand.Execute(null);
        }

        bool isViewboxFloatingBarMarginAnimationRunning = false;

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
            double MarginFromEdge = Settings.Appearance.FloatingBarBottomMargin;
            if (isFloatingBarFolded)
            {
                MarginFromEdge = -100;
            }
            else if (IsPresentationSlideShowRunning)
            {
                MarginFromEdge = 60;
            }
            MarginFromEdge = MarginFromEdge * (Settings.Appearance.FloatingBarScale / 100);
            await Dispatcher.InvokeAsync(() =>
            {
                if (Topmost == false)
                {
                    MarginFromEdge = -60;
                }
                else
                {
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                }
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * ViewboxFloatingBarScaleTransform.ScaleX) / 2;
                pos.Y = screenHeight - MarginFromEdge * ((ViewboxFloatingBarScaleTransform.ScaleY == 1) ? 1 : 0.9);

                if (MarginFromEdge != -60)
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

                ThicknessAnimation marginAnimation = new ThicknessAnimation
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
                if (Topmost == false) ViewboxFloatingBar.Visibility = Visibility.Hidden;
            });
        }

        private void CursorIcon_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.SetToolMode(ToolMode.Cursor, true, true);
        }

        private void PenIcon_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsPenMode && StackPanelCanvasControls.Visibility != Visibility.Collapsed)
            {
                ShellViewModel.TogglePenPaletteCommand.Execute(null);
            }
            else
            {
                ShellViewModel.SetToolMode(ToolMode.Pen, true, true);
            }
        }

        private void ColorThemeSwitch_MouseUp(object sender, RoutedEventArgs e)
        {
            isUselightThemeColor = !isUselightThemeColor;
            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                isDesktopUselightThemeColor = isUselightThemeColor;
            }
            CheckColorTheme();
        }

        private void EraserIcon_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.SetToolMode(ToolMode.Eraser, true, true);
        }

        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.SetToolMode(ToolMode.EraserByStrokes, true, true);
        }

        private void CursorWithDelIcon_Click(object sender, RoutedEventArgs e)
        {
            SymbolIconDelete_MouseUp(sender, null);
            CursorIcon_Click(null, null);
        }

        private void SelectIcon_MouseUp(object sender, RoutedEvent e)
        {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                StrokeCollection selectedStrokes = new StrokeCollection();
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                    {
                        selectedStrokes.Add(stroke);
                    }
                }
                inkCanvas.Select(selectedStrokes);
            }
            else
            {
                ApplyCanvasInteractionMode(CanvasInteractionMode.Select);
            }
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
            if (isLongPressSelected == true)
            {
                HideSubPanels("pen");
            }
            else
            {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                {
                    HideSubPanels("pen");
                }
                else
                {
                    HideSubPanels("cursor");
                }
            }
        }

        private void CloseBordertools_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideSubPanels();
        }

        #region Left Side Panel

        private void BtnFingerDragMode_Click(object sender, RoutedEventArgs e)
        {
            if (isSingleFingerDragMode)
            {
                isSingleFingerDragMode = false;
                //BtnFingerDragMode.Content = "单指\n拖动";
            }
            else
            {
                isSingleFingerDragMode = true;
                //BtnFingerDragMode.Content = "多指\n拖动";
            }
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }
            var item = timeMachine.Undo();
            ApplyHistoryToCanvas(item);
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }
            var item = timeMachine.Redo();
            ApplyHistoryToCanvas(item);
        }

        private void Element_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded) return;

            if (sender is Button { Content: UIElement content } button)
            {
                content.Opacity = button.IsEnabled ? 1 : 0.5;
                return;
            }

            if (sender is FontIcon fontIcon)
            {
                fontIcon.Opacity = fontIcon.IsEnabled ? 1 : 0.5;
            }
        }

        #endregion Left Side Panel

        #region Right Side Panel

        private bool closeIsFromButton;
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            closeIsFromButton = true;
            Close();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelper.StartWithShell(System.Windows.Forms.Application.ExecutablePath, "-m");

            closeIsFromButton = true;
            Application.Current.Shutdown();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.ToggleSettingsPanelCommand.Execute(null);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            //BorderClearInDelete.Visibility = Visibility.Collapsed;

            if (ShellViewModel.IsDesktopAnnotationMode)
            { // 先回到画笔再清屏，避免 TimeMachine 的相关 bug 影响
                if (Pen_Icon.Background == null && StackPanelCanvasControls.Visibility == Visibility.Visible)
                {
                    PenIcon_Click(null, null);
                }
            }
            else
            {
                if (Pen_Icon.Background == null)
                {
                    PenIcon_Click(null, null);
                }
            }

            if (inkCanvas.Strokes.Count != 0)
            {
                int whiteboardIndex = CurrentWhiteboardIndex;
                if (ShellViewModel.IsDesktopAnnotationMode)
                {
                    whiteboardIndex = 0;
                }
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();

            }

            ClearStrokes(false);
            inkCanvas.Children.Clear();

            CancelSingleFingerDragMode();
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
                BtnFingerDragMode_Click(null, null);
            }
            isLongPressSelected = false;
        }

        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            ApplyWorkspaceVisualState(ShellViewModel.WorkspaceMode);
        }

        int BoundsWidth = 5;

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;

                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Auto-clear Strokes 要等待截图完成再清理笔记
                if (!IsPresentationSlideShowRunning)
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode)
                    {
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                            {
                                SaveScreenshot(true);
                            }

                            BtnClear_Click(null, null);
                        }
                    }
                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode && !Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                    {
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                            {
                                SaveScreenshot(true);
                            }

                            BtnClear_Click(null, null);
                        }
                    }


                    if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                    {
                        inkCanvas.Visibility = Visibility.Visible;
                        inkCanvas.IsHitTestVisible = true;
                    }
                    else
                    {
                        inkCanvas.IsHitTestVisible = true;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }

                Main_Grid.Background = Brushes.Transparent;

                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

                if (ShellViewModel.IsBlackboardMode)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }
            }

            if (Main_Grid.Background == Brushes.Transparent)
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
        #endregion
    }
}
