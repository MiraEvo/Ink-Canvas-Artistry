using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels.Workspace
{
    public sealed class WorkspaceSessionViewModel : ObservableObject
    {
        private static readonly string[] WorkspaceVisualStateDependentProperties =
        [
            nameof(IsDesktopSession),
            nameof(IsBlackboardSession),
            nameof(IsBlackboardVisible),
            nameof(IsTransparentDesktopCanvas)
        ];

        private static readonly string[] CanvasVisibilityDependentProperties =
        [
            nameof(IsTransparentDesktopCanvas)
        ];

        private WorkspaceVisualState workspaceVisualState = WorkspaceVisualState.Desktop;
        private bool isCanvasVisible = true;
        private bool shouldRestoreDefaultToolOnDesktopResume;
        private bool shouldRestoreDefaultFloatingBarPosition;

        public WorkspaceVisualState WorkspaceVisualState => workspaceVisualState;

        public bool IsCanvasVisible => isCanvasVisible;

        public bool ShouldRestoreDefaultToolOnDesktopResume => shouldRestoreDefaultToolOnDesktopResume;

        public bool ShouldRestoreDefaultFloatingBarPosition => shouldRestoreDefaultFloatingBarPosition;

        public bool IsDesktopSession => workspaceVisualState is WorkspaceVisualState.Desktop;

        public bool IsBlackboardSession => workspaceVisualState is WorkspaceVisualState.Blackboard;

        public bool IsBlackboardVisible => IsBlackboardSession;

        public bool IsTransparentDesktopCanvas => IsDesktopSession && !isCanvasVisible;

        public bool SetWorkspaceVisualState(WorkspaceVisualState value)
        {
            return SetState(ref workspaceVisualState, value, WorkspaceVisualStateDependentProperties);
        }

        public bool SetCanvasVisible(bool value)
        {
            return SetState(ref isCanvasVisible, value, CanvasVisibilityDependentProperties);
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
            return ConsumeFlag(ref shouldRestoreDefaultToolOnDesktopResume, nameof(ShouldRestoreDefaultToolOnDesktopResume));
        }

        public bool ConsumeRestoreDefaultFloatingBarPosition()
        {
            return ConsumeFlag(ref shouldRestoreDefaultFloatingBarPosition, nameof(ShouldRestoreDefaultFloatingBarPosition));
        }

        private bool SetState<T>(ref T field, T value, string[] dependentProperties)
        {
            bool changed = SetProperty(ref field, value);
            if (changed)
            {
                NotifyPropertiesChanged(dependentProperties);
            }

            return changed;
        }

        private bool ConsumeFlag(ref bool field, string propertyName)
        {
            if (!field)
            {
                return false;
            }

            field = false;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertiesChanged(string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }
    }
}

