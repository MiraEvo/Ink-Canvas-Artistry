using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class PresentationSessionViewModel : ObservableObject
    {
        private PresentationProvider provider;
        private PresentationSessionState sessionState;
        private string presentationName = string.Empty;
        private int currentSlideIndex;
        private int slideCount;
        private bool isBottomNavigationVisible;
        private bool isSideNavigationVisible;

        public PresentationProvider Provider => provider;

        public PresentationSessionState SessionState => sessionState;

        public string PresentationName => presentationName;

        public int CurrentSlideIndex => currentSlideIndex;

        public int SlideCount => slideCount;

        public bool IsBottomNavigationVisible => isBottomNavigationVisible;

        public bool IsSideNavigationVisible => isSideNavigationVisible;

        public bool IsNavigationVisible => isBottomNavigationVisible || isSideNavigationVisible;

        public bool IsSlideShowRunning => sessionState == PresentationSessionState.SlideShowRunning;

        public bool IsPresentationConnected => sessionState != PresentationSessionState.Disconnected;

        public bool IsPowerPointSession => provider == PresentationProvider.PowerPoint;

        public bool CanNavigateSlides => IsSlideShowRunning && slideCount > 0;

        public bool ShouldShowSlideShowEndButton => IsSlideShowRunning;

        public void SetConnection(
            PresentationProvider sessionProvider,
            string name,
            int totalSlides,
            int currentSlide,
            bool isSlideShowRunning)
        {
            bool providerChanged = SetProperty(ref provider, sessionProvider, nameof(Provider));
            bool nameChanged = SetProperty(ref presentationName, name ?? string.Empty, nameof(PresentationName));
            bool slideCountChanged = SetProperty(ref slideCount, totalSlides, nameof(SlideCount));
            bool currentSlideChanged = SetProperty(ref currentSlideIndex, currentSlide, nameof(CurrentSlideIndex));
            bool stateChanged = SetProperty(
                ref sessionState,
                isSlideShowRunning ? PresentationSessionState.SlideShowRunning : PresentationSessionState.Connected,
                nameof(SessionState));

            if (providerChanged)
            {
                OnPropertyChanged(nameof(IsPowerPointSession));
            }

            if (stateChanged)
            {
                OnPropertyChanged(nameof(IsSlideShowRunning));
                OnPropertyChanged(nameof(IsPresentationConnected));
                OnPropertyChanged(nameof(CanNavigateSlides));
                OnPropertyChanged(nameof(ShouldShowSlideShowEndButton));
            }

            if (slideCountChanged)
            {
                OnPropertyChanged(nameof(CanNavigateSlides));
            }

            if (providerChanged || nameChanged || slideCountChanged || currentSlideChanged || stateChanged)
            {
                OnPropertyChanged(nameof(IsNavigationVisible));
            }
        }

        public void SetCurrentSlide(int currentSlide, int totalSlides)
        {
            bool currentSlideChanged = SetProperty(ref currentSlideIndex, currentSlide, nameof(CurrentSlideIndex));
            bool slideCountChanged = SetProperty(ref slideCount, totalSlides, nameof(SlideCount));
            if (slideCountChanged)
            {
                OnPropertyChanged(nameof(CanNavigateSlides));
            }

            if (currentSlideChanged || slideCountChanged)
            {
                OnPropertyChanged(nameof(IsNavigationVisible));
            }
        }

        public void SetSlideShowRunning(bool value)
        {
            PresentationSessionState nextState = value
                ? PresentationSessionState.SlideShowRunning
                : provider == PresentationProvider.None ? PresentationSessionState.Disconnected : PresentationSessionState.Connected;

            if (SetProperty(ref sessionState, nextState, nameof(SessionState)))
            {
                OnPropertyChanged(nameof(IsSlideShowRunning));
                OnPropertyChanged(nameof(IsPresentationConnected));
                OnPropertyChanged(nameof(CanNavigateSlides));
                OnPropertyChanged(nameof(ShouldShowSlideShowEndButton));
            }
        }

        public void SetNavigationVisibility(bool isBottomVisible, bool isSideVisible)
        {
            bool bottomChanged = SetProperty(ref isBottomNavigationVisible, isBottomVisible, nameof(IsBottomNavigationVisible));
            bool sideChanged = SetProperty(ref isSideNavigationVisible, isSideVisible, nameof(IsSideNavigationVisible));

            if (bottomChanged || sideChanged)
            {
                OnPropertyChanged(nameof(IsNavigationVisible));
            }
        }

        public void Disconnect()
        {
            bool providerChanged = SetProperty(ref provider, PresentationProvider.None, nameof(Provider));
            bool stateChanged = SetProperty(ref sessionState, PresentationSessionState.Disconnected, nameof(SessionState));
            bool nameChanged = SetProperty(ref presentationName, string.Empty, nameof(PresentationName));
            bool slideCountChanged = SetProperty(ref slideCount, 0, nameof(SlideCount));
            bool currentSlideChanged = SetProperty(ref currentSlideIndex, 0, nameof(CurrentSlideIndex));
            bool bottomChanged = SetProperty(ref isBottomNavigationVisible, false, nameof(IsBottomNavigationVisible));
            bool sideChanged = SetProperty(ref isSideNavigationVisible, false, nameof(IsSideNavigationVisible));

            if (providerChanged)
            {
                OnPropertyChanged(nameof(IsPowerPointSession));
            }

            if (stateChanged)
            {
                OnPropertyChanged(nameof(IsSlideShowRunning));
                OnPropertyChanged(nameof(IsPresentationConnected));
                OnPropertyChanged(nameof(CanNavigateSlides));
                OnPropertyChanged(nameof(ShouldShowSlideShowEndButton));
            }

            if (providerChanged || stateChanged || nameChanged || slideCountChanged || currentSlideChanged || bottomChanged || sideChanged)
            {
                OnPropertyChanged(nameof(IsNavigationVisible));
            }
        }
    }
}
