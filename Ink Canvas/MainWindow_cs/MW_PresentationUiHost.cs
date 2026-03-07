using Ink_Canvas.Features.Presentation;
using Ink_Canvas.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : IPresentationUiHost
    {
        bool IPresentationUiHost.IsBlackboardMode => ShellViewModel.IsBlackboardMode;

        bool IPresentationUiHost.IsCanvasHidden => Main_Grid.Background == Brushes.Transparent;

        bool IPresentationUiHost.IsFloatingBarFolded => isFloatingBarFolded;

        int IPresentationUiHost.CurrentInkStrokeCount => inkCanvas.Strokes.Count;

        void IPresentationUiHost.ResetDesktopInkColorBaseline() => ResetDesktopInkColorBaseline();

        void IPresentationUiHost.FoldFloatingBar() => FoldFloatingBar_Click(null, null);

        void IPresentationUiHost.UnfoldFloatingBar() => UnFoldFloatingBar_MouseUp(null, null);

        void IPresentationUiHost.PrepareWorkspaceForSlideShow(bool shouldShowCanvas) => PrepareWorkspaceForSlideShow(shouldShowCanvas);

        void IPresentationUiHost.ShowPresentationNavigation(bool showBottom, bool showSide) => ShowPresentationNavigation(showBottom, showSide);

        void IPresentationUiHost.HidePresentationNavigation() => HidePresentationNavigation();

        void IPresentationUiHost.SetSlideShowEndButtonVisibility(bool visible) => BtnPPTSlideShowEnd.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        void IPresentationUiHost.SetPresentationCounterText(string text) => PptNavigationTextBlockBottom.Text = text;

        void IPresentationUiHost.SetFloatingBarOpacity(double opacity) => ViewboxFloatingBar.Opacity = opacity;

        Task IPresentationUiHost.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay) => ViewboxFloatingBarMarginAnimationAfterDelayAsync(delay);

        void IPresentationUiHost.AnimateFloatingBarMargin() => ViewboxFloatingBarMarginAnimation();

        byte[] IPresentationUiHost.CaptureInkStrokes() => CaptureInkStrokes();

        void IPresentationUiHost.RestoreInkStrokes(byte[] inkData) => RestoreInkStrokes(inkData);

        void IPresentationUiHost.ClearPresentationInkAndHistory() => ClearPresentationInkAndHistory();

        void IPresentationUiHost.SavePresentationScreenshot(string fileName) => SavePPTScreenshot(fileName);

        void IPresentationUiHost.ApplySlideShowEndWorkspaceTransition(bool isColorfulFloatingBar) => ApplySlideShowEndWorkspaceTransition(isColorfulFloatingBar);

        void IPresentationUiHost.PrepareForPresentationNavigationRequest() => PrepareForPresentationNavigationRequest();

        Task IPresentationUiHost.PrepareForSlideShowExitRequestAsync() => PrepareForSlideShowExitRequestAsync();

        void IPresentationUiHost.PromptRestorePreviousPage(int page, Action onConfirm) => PromptRestorePreviousPage(page, onConfirm);

        void IPresentationUiHost.PromptUnhideHiddenSlides(Action onConfirm) => PromptUnhideHiddenSlides(onConfirm);

        void IPresentationUiHost.PromptDisableAutomaticAdvance(Action onConfirm) => PromptDisableAutomaticAdvance(onConfirm);

        private void ResetDesktopInkColorBaseline()
        {
            lastDesktopInkColor = 1;
        }

        private void PrepareWorkspaceForSlideShow(bool shouldShowCanvas)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }
            else if (shouldShowCanvas && Main_Grid.Background == Brushes.Transparent)
            {
                BtnHideInkCanvas_Click(null, null);
            }

            ClearStrokes(true);
            BorderFloatingBarMainControls.Visibility = Visibility.Visible;

            if (shouldShowCanvas)
            {
                BtnColorRed_Click(null, null);
            }
        }

        private void ShowPresentationNavigation(bool showBottom, bool showSide)
        {
            if (showBottom)
            {
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
            }
            else
            {
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
            }

            if (showSide)
            {
                AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
            }
            else
            {
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
            }
        }

        private byte[] CaptureInkStrokes()
        {
            try
            {
                using MemoryStream memoryStream = new();
                inkCanvas.Strokes.Save(memoryStream);
                return memoryStream.ToArray();
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to capture current strokes");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to capture current strokes");
            }

            return [];
        }

        private void RestoreInkStrokes(byte[] inkData)
        {
            if (inkData == null || inkData.Length == 0)
            {
                return;
            }

            try
            {
                using MemoryStream memoryStream = new(inkData);
                inkCanvas.Strokes.Add(new StrokeCollection(memoryStream));
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to restore presentation strokes");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to restore presentation strokes");
            }
        }

        private void ClearPresentationInkAndHistory()
        {
            ClearStrokes(true);
            timeMachine.ClearStrokeHistory();
        }

        private void ApplySlideShowEndWorkspaceTransition(bool isColorfulFloatingBar)
        {
            HidePresentationNavigation();
            BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;

            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            ClearStrokes(true);

            if (Main_Grid.Background != Brushes.Transparent)
            {
                BtnHideInkCanvas_Click(null, null);
            }

            RestoreDesktopWorkspaceDefaultsAfterPresentation();
            ViewboxFloatingBar.Opacity = isColorfulFloatingBar ? 0.95 : 1;
        }

        private void PrepareForPresentationNavigationRequest()
        {
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            CursorIcon_Click(null, null);
        }

        private async Task PrepareForSlideShowExitRequestAsync()
        {
            HideSubPanels("cursor");
            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
        }

        private void PromptRestorePreviousPage(int page, Action onConfirm)
        {
            new YesOrNoNotificationWindow(
                $"上次播放到了第 {page} 页, 是否立即跳转",
                onConfirm)
                .ShowDialog();
        }

        private void PromptUnhideHiddenSlides(Action onConfirm)
        {
            if (IsShowingRestoreHiddenSlidesWindow)
            {
                return;
            }

            IsShowingRestoreHiddenSlidesWindow = true;
            new YesOrNoNotificationWindow(
                "检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                onConfirm)
                .ShowDialog();
        }

        private void PromptDisableAutomaticAdvance(Action onConfirm)
        {
            new YesOrNoNotificationWindow(
                "检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                onConfirm)
                .ShowDialog();
        }
    }
}
