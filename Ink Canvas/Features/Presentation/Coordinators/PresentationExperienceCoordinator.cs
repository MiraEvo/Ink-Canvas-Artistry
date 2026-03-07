using Ink_Canvas.Controllers;
using Ink_Canvas.Services.Logging;
using Ink_Canvas.ViewModels;
using System;
using System.Threading.Tasks;

namespace Ink_Canvas.Features.Presentation.Coordinators
{
    internal sealed class PresentationExperienceCoordinator
    {
        private readonly IPresentationSessionController presentationSessionController;
        private readonly SettingsViewModel settingsViewModel;
        private readonly PresentationSessionViewModel presentationViewModel;
        private readonly IPresentationUiHost uiHost;
        private readonly PresentationInkArchiveService archiveService;
        private readonly IAppLogger logger;

        public PresentationExperienceCoordinator(
            IPresentationSessionController presentationSessionController,
            SettingsViewModel settingsViewModel,
            PresentationSessionViewModel presentationViewModel,
            IPresentationUiHost uiHost,
            PresentationInkArchiveService archiveService,
            IAppLogger logger)
        {
            ArgumentNullException.ThrowIfNull(presentationSessionController);
            ArgumentNullException.ThrowIfNull(settingsViewModel);
            ArgumentNullException.ThrowIfNull(presentationViewModel);
            ArgumentNullException.ThrowIfNull(uiHost);
            ArgumentNullException.ThrowIfNull(archiveService);
            ArgumentNullException.ThrowIfNull(logger);

            this.presentationSessionController = presentationSessionController;
            this.settingsViewModel = settingsViewModel;
            this.presentationViewModel = presentationViewModel;
            this.uiHost = uiHost;
            this.archiveService = archiveService;
            this.logger = logger.ForCategory(nameof(PresentationExperienceCoordinator));
        }

        public PresentationInkSessionState State { get; } = new();

        public void HandlePresentationConnected()
        {
            string presentationName = ResolvePresentationName();
            int slideCount = presentationViewModel.SlideCount;
            if (string.IsNullOrWhiteSpace(presentationName) || slideCount <= 0)
            {
                return;
            }

            string folderPath = archiveService.GetPresentationStoragePath(
                settingsViewModel.AutoSavedStrokesLocation,
                presentationName,
                slideCount);

            if (settingsViewModel.IsNotifyPreviousPage
                && archiveService.TryReadPosition(folderPath, out int page)
                && page > 0
                && page <= slideCount)
            {
                uiHost.PromptRestorePreviousPage(page, () => presentationSessionController.TryGoToSlide(page));
            }

            if (settingsViewModel.IsNotifyHiddenPage && presentationSessionController.HasHiddenSlides())
            {
                uiHost.PromptUnhideHiddenSlides(() => presentationSessionController.TryUnhideHiddenSlides());
            }

            if (settingsViewModel.IsNotifyAutoPlayPresentation
                && !presentationViewModel.IsSlideShowRunning
                && presentationSessionController.HasAutomaticAdvance())
            {
                uiHost.PromptDisableAutomaticAdvance(() => presentationSessionController.TryDisableAutomaticAdvance());
            }
        }

        public void HandlePresentationClosed()
        {
            State.End();
            presentationViewModel.SetNavigationVisibility(false, false);
            uiHost.HidePresentationNavigation();
            uiHost.SetSlideShowEndButtonVisibility(false);
        }

        public void HandleSlideShowBegin()
        {
            if (settingsViewModel.IsAutoFoldInPPTSlideShow && !uiHost.IsFloatingBarFolded)
            {
                uiHost.FoldFloatingBar();
            }
            else if (uiHost.IsFloatingBarFolded)
            {
                uiHost.UnfoldFloatingBar();
            }

            string presentationName = ResolvePresentationName();
            int slideCount = presentationViewModel.SlideCount;
            int currentSlideIndex = ResolveCurrentSlideIndex();

            uiHost.ResetDesktopInkColorBaseline();
            State.Start(presentationName, slideCount, currentSlideIndex);

            if (settingsViewModel.IsAutoSaveStrokesInPowerPoint)
            {
                string folderPath = archiveService.GetPresentationStoragePath(
                    settingsViewModel.AutoSavedStrokesLocation,
                    presentationName,
                    slideCount);

                foreach ((int slideIndex, byte[] inkData) in archiveService.LoadInkBuffers(folderPath))
                {
                    State.SetSlideInk(slideIndex, inkData);
                }
            }

            bool showBottomNavigation = settingsViewModel.IsShowBottomPptNavigationPanel;
            bool showSideNavigation = settingsViewModel.IsShowSidePptNavigationPanel;

            uiHost.ShowPresentationNavigation(showBottomNavigation, showSideNavigation);
            uiHost.SetSlideShowEndButtonVisibility(true);
            uiHost.SetFloatingBarOpacity(settingsViewModel.IsColorfulViewboxFloatingBar ? 0.8 : 0.75);
            uiHost.PrepareWorkspaceForSlideShow(settingsViewModel.IsShowCanvasAtNewSlideShow);
            uiHost.SetPresentationCounterText($"{ResolveCurrentSlideIndex()}/{slideCount}");
            presentationViewModel.SetNavigationVisibility(showBottomNavigation, showSideNavigation);

            logger.Info("PowerPoint Slide Show Loading process complete");
            _ = uiHost.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan.FromMilliseconds(100));
        }

        public async Task HandleSlideShowEndAsync()
        {
            if (uiHost.IsFloatingBarFolded)
            {
                uiHost.UnfoldFloatingBar();
            }

            logger.Event("PowerPoint Slide Show End");
            if (!State.TryBeginSlideShowEnd())
            {
                logger.Info("Detected previous entrance, returning");
                return;
            }

            if (settingsViewModel.IsAutoSaveStrokesInPowerPoint)
            {
                string presentationName = ResolvePresentationName();
                int slideCount = presentationViewModel.SlideCount;
                string folderPath = archiveService.GetPresentationStoragePath(
                    settingsViewModel.AutoSavedStrokesLocation,
                    presentationName,
                    slideCount);

                CaptureCurrentSlideInk(ResolveCurrentSlideIndex());
                archiveService.SavePosition(folderPath, ResolveCurrentSlideIndex());
                archiveService.SaveSession(folderPath, State);
            }

            presentationViewModel.SetNavigationVisibility(false, false);
            uiHost.ApplySlideShowEndWorkspaceTransition(settingsViewModel.IsColorfulViewboxFloatingBar);

            await Task.Delay(150);
            uiHost.AnimateFloatingBarMargin();
            State.End();
        }

        public void HandleSlideShowNextSlide()
        {
            int currentSlideIndex = ResolveCurrentSlideIndex();
            int slideCount = presentationViewModel.SlideCount;
            logger.Event($"PowerPoint Next Slide (Slide {currentSlideIndex})");

            if (currentSlideIndex == State.PreviousSlideIndex)
            {
                return;
            }

            CaptureCurrentSlideInk(State.PreviousSlideIndex);

            if (uiHost.CurrentInkStrokeCount > settingsViewModel.MinimumAutomationStrokeNumber
                && settingsViewModel.IsAutoSaveScreenShotInPowerPoint
                && !State.IsNavigationButtonTurnPending)
            {
                uiHost.SavePresentationScreenshot($"{ResolvePresentationName()}/{currentSlideIndex}");
            }

            State.ClearNavigationButtonTurnRequested();
            uiHost.ClearPresentationInkAndHistory();

            if (State.TryGetSlideInk(currentSlideIndex, out byte[]? inkData) && inkData != null)
            {
                uiHost.RestoreInkStrokes(inkData);
            }

            State.SetCurrentSlideIndex(currentSlideIndex);
            State.SetPreviousSlideIndex(currentSlideIndex);
            uiHost.SetPresentationCounterText($"{currentSlideIndex}/{slideCount}");
        }

        public void HandlePreviousSlideRequested()
        {
            State.MarkNavigationButtonTurnRequested();
            SaveNavigationScreenshotIfNeeded();
            presentationSessionController.TryGoToPreviousSlide();
        }

        public void HandleNextSlideRequested()
        {
            State.MarkNavigationButtonTurnRequested();
            SaveNavigationScreenshotIfNeeded();
            presentationSessionController.TryGoToNextSlide();
        }

        public async Task HandleNavigationButtonAsync()
        {
            uiHost.PrepareForPresentationNavigationRequest();
            presentationSessionController.TryShowSlideNavigation();

            if (!uiHost.IsFloatingBarFolded)
            {
                await Task.Delay(100);
                uiHost.AnimateFloatingBarMargin();
            }
        }

        public async Task HandleSlideShowEndRequestedAsync()
        {
            presentationSessionController.TryExitSlideShow();
            await uiHost.PrepareForSlideShowExitRequestAsync();
        }

        private void CaptureCurrentSlideInk(int slideIndex)
        {
            if (slideIndex <= 0)
            {
                return;
            }

            State.SetSlideInk(slideIndex, uiHost.CaptureInkStrokes());
        }

        private void SaveNavigationScreenshotIfNeeded()
        {
            if (uiHost.CurrentInkStrokeCount > settingsViewModel.MinimumAutomationStrokeNumber
                && settingsViewModel.IsAutoSaveScreenShotInPowerPoint)
            {
                uiHost.SavePresentationScreenshot($"{ResolvePresentationName()}/{ResolveCurrentSlideIndex()}");
            }
        }

        private string ResolvePresentationName()
        {
            return !string.IsNullOrWhiteSpace(presentationViewModel.PresentationName)
                ? presentationViewModel.PresentationName
                : State.PresentationName;
        }

        private int ResolveCurrentSlideIndex()
        {
            if (presentationViewModel.CurrentSlideIndex > 0)
            {
                return presentationViewModel.CurrentSlideIndex;
            }

            if (State.CurrentSlideIndex > 0)
            {
                return State.CurrentSlideIndex;
            }

            return State.PreviousSlideIndex;
        }
    }
}

