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

        public bool IsSlideShowRunning => sessionState is PresentationSessionState.SlideShowRunning;

        public bool IsPresentationConnected => sessionState is not PresentationSessionState.Disconnected;

        public bool IsPowerPointSession => provider is PresentationProvider.PowerPoint;

        public bool CanNavigateSlides => IsSlideShowRunning && slideCount > 0;

        public bool ShouldShowSlideShowEndButton => IsSlideShowRunning;

        public void SetConnection(
            PresentationProvider sessionProvider,
            string? name,
            int totalSlides,
            int currentSlide,
            bool isSlideShowRunning)
        {
            bool providerChanged = SetProvider(sessionProvider);
            bool nameChanged = SetPresentationName(name);
            bool slideCountChanged = SetSlideCount(totalSlides);
            bool currentSlideChanged = SetCurrentSlideIndex(currentSlide);
            bool stateChanged = SetSessionState(ResolveSessionState(sessionProvider, isSlideShowRunning));

            NotifyStateChangeEffects(
                providerChanged,
                stateChanged,
                slideCountChanged,
                providerChanged || nameChanged || slideCountChanged || currentSlideChanged || stateChanged);
        }

        public void SetCurrentSlide(int currentSlide, int totalSlides)
        {
            bool currentSlideChanged = SetCurrentSlideIndex(currentSlide);
            bool slideCountChanged = SetSlideCount(totalSlides);

            if (slideCountChanged)
            {
                NotifySlideAvailabilityChanged();
            }

            if (currentSlideChanged || slideCountChanged)
            {
                NotifyNavigationVisibilityChanged();
            }
        }

        public void SetSlideShowRunning(bool value)
        {
            if (SetSessionState(ResolveSessionState(provider, value)))
            {
                NotifySessionStateChanged();
            }
        }

        public void SetNavigationVisibility(bool isBottomVisible, bool isSideVisible)
        {
            bool bottomChanged = SetBottomNavigationVisible(isBottomVisible);
            bool sideChanged = SetSideNavigationVisible(isSideVisible);

            if (bottomChanged || sideChanged)
            {
                NotifyNavigationVisibilityChanged();
            }
        }

        public void Disconnect()
        {
            bool providerChanged = SetProvider(PresentationProvider.None);
            bool stateChanged = SetSessionState(PresentationSessionState.Disconnected);
            bool nameChanged = SetPresentationName(string.Empty);
            bool slideCountChanged = SetSlideCount(0);
            bool currentSlideChanged = SetCurrentSlideIndex(0);
            bool bottomChanged = SetBottomNavigationVisible(false);
            bool sideChanged = SetSideNavigationVisible(false);

            NotifyStateChangeEffects(
                providerChanged,
                stateChanged,
                slideCountChanged,
                providerChanged || stateChanged || nameChanged || slideCountChanged || currentSlideChanged || bottomChanged || sideChanged);
        }

        private bool SetProvider(PresentationProvider value)
        {
            return SetProperty(ref provider, value, nameof(Provider));
        }

        private bool SetPresentationName(string? value)
        {
            return SetProperty(ref presentationName, value ?? string.Empty, nameof(PresentationName));
        }

        private bool SetSlideCount(int value)
        {
            return SetProperty(ref slideCount, value, nameof(SlideCount));
        }

        private bool SetCurrentSlideIndex(int value)
        {
            return SetProperty(ref currentSlideIndex, value, nameof(CurrentSlideIndex));
        }

        private bool SetSessionState(PresentationSessionState value)
        {
            return SetProperty(ref sessionState, value, nameof(SessionState));
        }

        private bool SetBottomNavigationVisible(bool value)
        {
            return SetProperty(ref isBottomNavigationVisible, value, nameof(IsBottomNavigationVisible));
        }

        private bool SetSideNavigationVisible(bool value)
        {
            return SetProperty(ref isSideNavigationVisible, value, nameof(IsSideNavigationVisible));
        }

        private void NotifyStateChangeEffects(
            bool providerChanged,
            bool stateChanged,
            bool slideCountChanged,
            bool navigationContextChanged)
        {
            if (providerChanged)
            {
                OnPropertyChanged(nameof(IsPowerPointSession));
            }

            if (stateChanged)
            {
                NotifySessionStateChanged();
            }
            else if (slideCountChanged)
            {
                NotifySlideAvailabilityChanged();
            }

            if (navigationContextChanged)
            {
                NotifyNavigationVisibilityChanged();
            }
        }

        private void NotifySessionStateChanged()
        {
            OnPropertyChanged(nameof(IsSlideShowRunning));
            OnPropertyChanged(nameof(IsPresentationConnected));
            NotifySlideAvailabilityChanged();
            OnPropertyChanged(nameof(ShouldShowSlideShowEndButton));
        }

        private void NotifySlideAvailabilityChanged()
        {
            OnPropertyChanged(nameof(CanNavigateSlides));
        }

        private void NotifyNavigationVisibilityChanged()
        {
            OnPropertyChanged(nameof(IsNavigationVisible));
        }

        private static PresentationSessionState ResolveSessionState(
            PresentationProvider currentProvider,
            bool isSlideShowRunning)
        {
            if (currentProvider is PresentationProvider.None)
            {
                return PresentationSessionState.Disconnected;
            }

            return isSlideShowRunning
                ? PresentationSessionState.SlideShowRunning
                : PresentationSessionState.Connected;
        }
    }
}
