using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Controllers
{
    public sealed class WorkspaceSessionController(
        WorkspaceSessionViewModel workspaceSessionViewModel,
        ShellViewModel shellViewModel) : IWorkspaceSessionController
    {
        public void Initialize(bool isCanvasVisible)
        {
            ApplyWorkspaceMode(shellViewModel.WorkspaceMode, isCanvasVisible);
        }

        public void ApplyWorkspaceMode(WorkspaceMode mode, bool isCanvasVisible)
        {
            workspaceSessionViewModel.SetWorkspaceVisualState(
                mode == WorkspaceMode.Blackboard ? WorkspaceVisualState.Blackboard : WorkspaceVisualState.Desktop);
            workspaceSessionViewModel.SetCanvasVisible(isCanvasVisible);
        }

        public void EnterBlackboard()
        {
            if (shellViewModel.IsBlackboardTransitioning)
            {
                return;
            }

            shellViewModel.SetBlackboardTransitioning(true);
            shellViewModel.SetWorkspaceMode(WorkspaceMode.Blackboard);
        }

        public void ExitBlackboard(bool restoreDefaultTool, bool restoreFloatingBarPosition, bool clearStrokes)
        {
            if (restoreDefaultTool)
            {
                workspaceSessionViewModel.SetRestoreDefaultToolOnDesktopResume(true);
            }

            if (restoreFloatingBarPosition)
            {
                workspaceSessionViewModel.SetRestoreDefaultFloatingBarPosition(true);
            }

            if (shellViewModel.IsBlackboardMode)
            {
                if (shellViewModel.IsBlackboardTransitioning)
                {
                    return;
                }

                shellViewModel.SetBlackboardTransitioning(true);
                shellViewModel.SetWorkspaceMode(WorkspaceMode.DesktopAnnotation);
            }
        }

        public void RestoreDesktopDefaultsAfterPresentation()
        {
            ExitBlackboard(true, true, false);
        }

        public void SyncCanvasVisibility(bool isCanvasVisible)
        {
            workspaceSessionViewModel.SetCanvasVisible(isCanvasVisible);
        }
    }
}
