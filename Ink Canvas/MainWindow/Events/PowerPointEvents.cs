using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void PresentationSessionController_PresentationConnected()
        {
            presentationExperienceCoordinator?.HandlePresentationConnected();
        }

        private void PptApplication_PresentationClose()
        {
            presentationExperienceCoordinator?.HandlePresentationClosed();
        }

        private void PptApplication_SlideShowBegin()
        {
            presentationExperienceCoordinator?.HandleSlideShowBegin();
        }

        private async void PptApplication_SlideShowEnd()
        {
            if (presentationExperienceCoordinator == null)
            {
                return;
            }

            await presentationExperienceCoordinator.HandleSlideShowEndAsync();
        }

        private void PptApplication_SlideShowNextSlide()
        {
            presentationExperienceCoordinator?.HandleSlideShowNextSlide();
        }

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            presentationExperienceCoordinator?.HandlePreviousSlideRequested();
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            presentationExperienceCoordinator?.HandleNextSlideRequested();
        }

        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender || presentationExperienceCoordinator == null)
            {
                return;
            }

            await presentationExperienceCoordinator.HandleNavigationButtonAsync();
        }

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            if (presentationExperienceCoordinator == null)
            {
                return;
            }

            await presentationExperienceCoordinator.HandleSlideShowEndRequestedAsync();
        }

        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }

            BtnPPTSlidesUp_Click(null, null);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }

            BtnPPTSlidesDown_Click(null, null);
        }
    }
}
