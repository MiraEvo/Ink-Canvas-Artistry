using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private CancellationTokenSource? inkReplayCancellationTokenSource;
        private bool closeIsFromButton;

        private void SymbolIconScreenshot_Click(object sender, RoutedEventArgs e)
        {
            taskGuard.Forget(
                SaveToolbarScreenshotAsync(),
                new AppErrorContext(nameof(MainWindow), "SaveToolbarScreenshotAsync"));
        }

        private async Task SaveToolbarScreenshotAsync()
        {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            SaveScreenShotToDesktop();
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
            taskGuard.Forget(
                StartInkReplayAsync(),
                new AppErrorContext(nameof(MainWindow), "StartInkReplayAsync"));
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

            StrokeCollection selectedStrokes = inkCanvas.GetSelectedStrokes();
            StrokeCollection strokes = selectedStrokes.Count != 0
                ? selectedStrokes.Clone()
                : inkCanvas.Strokes.Clone();

            CancellationTokenSource replayCancellationTokenSource = BeginInkReplay();
            try
            {
                await ReplayStrokesAsync(strokes, replayCancellationTokenSource.Token);
                await Task.Delay(100, replayCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Toolbox | Ink replay was canceled.");
            }
            finally
            {
                CompleteInkReplay(replayCancellationTokenSource);
            }
        }

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
            StylusPointCollection stylusPoints = new();
            Stroke? replayStroke = null;
            int pointsSinceDelay = 0;

            foreach (StylusPoint stylusPoint in stroke.StylusPoints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (replayStroke != null)
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

        private void Element_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded)
            {
                return;
            }

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
    }
}
