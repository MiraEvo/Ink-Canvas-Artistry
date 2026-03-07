using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class WorkspaceSessionViewModel : ObservableObject
    {
        private WorkspaceVisualState workspaceVisualState = WorkspaceVisualState.Desktop;
        private bool isCanvasVisible = true;
        private bool shouldRestoreDefaultToolOnDesktopResume;
        private bool shouldRestoreDefaultFloatingBarPosition;

        public WorkspaceVisualState WorkspaceVisualState => workspaceVisualState;

        public bool IsCanvasVisible => isCanvasVisible;

        public bool ShouldRestoreDefaultToolOnDesktopResume => shouldRestoreDefaultToolOnDesktopResume;

        public bool ShouldRestoreDefaultFloatingBarPosition => shouldRestoreDefaultFloatingBarPosition;

        public bool IsDesktopSession => workspaceVisualState == WorkspaceVisualState.Desktop;

        public bool IsBlackboardSession => workspaceVisualState == WorkspaceVisualState.Blackboard;

        public bool IsBlackboardVisible => IsBlackboardSession;

        public bool IsTransparentDesktopCanvas => IsDesktopSession && !isCanvasVisible;

        public bool SetWorkspaceVisualState(WorkspaceVisualState value)
        {
            bool changed = SetProperty(ref workspaceVisualState, value);
            if (changed)
            {
                OnPropertyChanged(nameof(IsDesktopSession));
                OnPropertyChanged(nameof(IsBlackboardSession));
                OnPropertyChanged(nameof(IsBlackboardVisible));
                OnPropertyChanged(nameof(IsTransparentDesktopCanvas));
            }

            return changed;
        }

        public bool SetCanvasVisible(bool value)
        {
            bool changed = SetProperty(ref isCanvasVisible, value);
            if (changed)
            {
                OnPropertyChanged(nameof(IsTransparentDesktopCanvas));
            }

            return changed;
        }

        public bool SetRestoreDefaultToolOnDesktopResume(bool value)
        {
            return SetProperty(ref shouldRestoreDefaultToolOnDesktopResume, value);
        }

        public bool SetRestoreDefaultFloatingBarPosition(bool value)
        {
            return SetProperty(ref shouldRestoreDefaultFloatingBarPosition, value);
        }

        public bool ConsumeRestoreDefaultToolOnDesktopResume()
        {
            if (!shouldRestoreDefaultToolOnDesktopResume)
            {
                return false;
            }

            shouldRestoreDefaultToolOnDesktopResume = false;
            OnPropertyChanged(nameof(ShouldRestoreDefaultToolOnDesktopResume));
            return true;
        }

        public bool ConsumeRestoreDefaultFloatingBarPosition()
        {
            if (!shouldRestoreDefaultFloatingBarPosition)
            {
                return false;
            }

            shouldRestoreDefaultFloatingBarPosition = false;
            OnPropertyChanged(nameof(ShouldRestoreDefaultFloatingBarPosition));
            return true;
        }
    }
}
