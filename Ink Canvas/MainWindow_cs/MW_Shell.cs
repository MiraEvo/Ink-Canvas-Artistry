using Ink_Canvas.Features.Shell;
using Ink_Canvas.ViewModels;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private readonly FloatingBarLayoutState floatingBarLayoutState = new();
        private readonly ShellInteractionState shellInteractionState = new();

        private bool isApplyingShellSubPanelState
        {
            get => shellInteractionState.IsApplyingSubPanelState;
            set => shellInteractionState.IsApplyingSubPanelState = value;
        }

        private bool isFloatingBarFolded
        {
            get => ShellViewModel?.IsFloatingBarFolded == true;
            set => ShellViewModel?.SetFloatingBarFolded(value, false);
        }

        private bool isFloatingBarChangingHideMode
        {
            get => ShellViewModel?.IsFloatingBarTransitioning == true;
            set => ShellViewModel?.SetFloatingBarTransitioning(value);
        }

        private bool isDisplayingOrHidingBlackboard
        {
            get => ShellViewModel?.IsBlackboardTransitioning == true;
            set => ShellViewModel?.SetBlackboardTransitioning(value);
        }

        private void ShellViewModel_WorkspaceModeChanged(WorkspaceMode mode)
        {
            shellExperienceCoordinator.HandleWorkspaceModeChanged(mode);
        }

        private void ShellViewModel_ToolModeChanged(ToolMode mode)
        {
            shellExperienceCoordinator.HandleToolModeChanged(mode);
        }

        private void ShellViewModel_ActiveSubPanelChanged(SubPanelKind panel)
        {
            shellExperienceCoordinator.HandleSubPanelChanged(panel);
        }
    }
}
