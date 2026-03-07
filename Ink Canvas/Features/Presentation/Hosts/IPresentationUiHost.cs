using System;
using System.Threading.Tasks;

namespace Ink_Canvas.Features.Presentation.Hosts
{
    internal interface IPresentationUiHost
    {
        bool IsBlackboardMode { get; }

        bool IsCanvasHidden { get; }

        bool IsFloatingBarFolded { get; }

        int CurrentInkStrokeCount { get; }

        void ResetDesktopInkColorBaseline();

        void FoldFloatingBar();

        void UnfoldFloatingBar();

        void PrepareWorkspaceForSlideShow(bool shouldShowCanvas);

        void ShowPresentationNavigation(bool showBottom, bool showSide);

        void HidePresentationNavigation();

        void SetSlideShowEndButtonVisibility(bool visible);

        void SetPresentationCounterText(string text);

        void SetFloatingBarOpacity(double opacity);

        Task AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay);

        void AnimateFloatingBarMargin();

        byte[] CaptureInkStrokes();

        void RestoreInkStrokes(byte[] inkData);

        void ClearPresentationInkAndHistory();

        void SavePresentationScreenshot(string fileName);

        void ApplySlideShowEndWorkspaceTransition(bool isColorfulFloatingBar);

        void PrepareForPresentationNavigationRequest();

        Task PrepareForSlideShowExitRequestAsync();

        void PromptRestorePreviousPage(int page, Action onConfirm);

        void PromptUnhideHiddenSlides(Action onConfirm);

        void PromptDisableAutomaticAdvance(Action onConfirm);
    }
}

