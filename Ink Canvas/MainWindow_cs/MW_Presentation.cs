using Ink_Canvas.Controllers;
using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System.ComponentModel;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private IPresentationSessionController? presentationSessionController;

        private PresentationSessionViewModel PresentationViewModel => mainWindowViewModel.Presentation;

        private AutomationStateViewModel AutomationViewModel => mainWindowViewModel.Automation;

        private bool IsPresentationSlideShowRunning => PresentationViewModel.IsSlideShowRunning;

        private bool IsPresentationConnected => PresentationViewModel.IsPresentationConnected;

        private string CurrentPresentationName => string.IsNullOrWhiteSpace(PresentationViewModel.PresentationName)
            ? pptName
            : PresentationViewModel.PresentationName;

        private int CurrentPresentationSlideIndex => PresentationViewModel.CurrentSlideIndex != 0
            ? PresentationViewModel.CurrentSlideIndex
            : previousSlideID;

        public static bool IsShowingRestoreHiddenSlidesWindow = false;

        public Microsoft.Office.Interop.PowerPoint.Application? pptApplication => presentationSessionController?.PowerPointApplication;

        public Presentation? presentation => presentationSessionController?.Presentation;

        public Slides? slides => presentationSessionController?.Slides;

        public Slide? slide => presentationSessionController?.Slide;

        private bool foldFloatingBarByUser
        {
            get => AutomationViewModel.IsFloatingBarFoldedByUser;
            set => AutomationViewModel.SetFloatingBarFoldedByUser(value);
        }

        private bool unfoldFloatingBarByUser
        {
            get => AutomationViewModel.IsFloatingBarUnfoldedByUser;
            set => AutomationViewModel.SetFloatingBarUnfoldedByUser(value);
        }

        private bool isHidingSubPanelsWhenInking
        {
            get => AutomationViewModel.IsHidingSubPanelsWhenInking;
            set => AutomationViewModel.SetHidingSubPanelsWhenInking(value);
        }

        private void InitializePresentationController()
        {
            presentationSessionController = new PresentationSessionController(
                mainWindowViewModel.Presentation,
                () => Settings.PowerPointSettings.IsSupportWPS,
                () => IsShowingRestoreHiddenSlidesWindow);
            presentationSessionController.PresentationConnected += PresentationSessionController_PresentationConnected;
            presentationSessionController.PresentationClosed += PptApplication_PresentationClose;
            presentationSessionController.SlideShowBegin += PptApplication_SlideShowBegin;
            presentationSessionController.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
            presentationSessionController.SlideShowEnd += PptApplication_SlideShowEnd;
        }

        private void PresentationViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!TryEnqueueOnUiThread(() => PresentationViewModel_PropertyChanged(sender, e)))
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(PresentationSessionViewModel.ShouldShowSlideShowEndButton):
                    BtnPPTSlideShowEnd.Visibility = PresentationViewModel.ShouldShowSlideShowEndButton
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    if (!PresentationViewModel.ShouldShowSlideShowEndButton)
                    {
                        PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                        PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                        PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                        PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }

        private void StartPresentationMonitoring()
        {
            presentationSessionController?.StartMonitoring();
        }

        private void StopPresentationMonitoring()
        {
            presentationSessionController?.StopMonitoring();
        }

        private bool TryEnqueueOnUiThread(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                return true;
            }

            _ = Dispatcher.InvokeAsync(action);
            return false;
        }
    }
}
