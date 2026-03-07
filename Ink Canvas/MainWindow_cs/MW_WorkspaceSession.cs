using Ink_Canvas.Helpers;
using Ink_Canvas.Controllers;
using Ink_Canvas.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private IWorkspaceSessionController workspaceSessionController;

        private WorkspaceSessionViewModel WorkspaceSessionViewModel => mainWindowViewModel.WorkspaceSession;

        private bool IsCanvasWritingVisible => Main_Grid.Background != Brushes.Transparent;

        private void InitializeWorkspaceSessionController()
        {
            workspaceSessionController = new WorkspaceSessionController(
                mainWindowViewModel.WorkspaceSession,
                mainWindowViewModel.Shell);
            workspaceSessionController.Initialize(IsCanvasWritingVisible);
        }

        private void EnterBlackboardSession()
        {
            workspaceSessionController?.EnterBlackboard();
        }

        private void ExitBlackboardSession(
            bool restoreDefaultTool = false,
            bool restoreFloatingBarPosition = false,
            bool clearStrokes = false)
        {
            workspaceSessionController?.ExitBlackboard(
                restoreDefaultTool,
                restoreFloatingBarPosition,
                clearStrokes);

            if (ShellViewModel?.IsBlackboardMode != true)
            {
                ApplyPendingWorkspaceDesktopDefaults();
            }
        }

        private void RestoreDesktopWorkspaceDefaultsAfterPresentation()
        {
            workspaceSessionController?.RestoreDesktopDefaultsAfterPresentation();
            ApplyPendingWorkspaceDesktopDefaults();
        }

        private void SyncWorkspaceCanvasVisibility()
        {
            workspaceSessionController?.SyncCanvasVisibility(IsCanvasWritingVisible);
        }

        private void ApplyWorkspaceVisualState(WorkspaceMode workspaceMode)
        {
            workspaceSessionController?.ApplyWorkspaceMode(workspaceMode, IsCanvasWritingVisible);

            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (workspaceMode == WorkspaceMode.Blackboard)
                {
                    SetBlackboardVisualState(false);
                    SaveStrokes(true);
                    ClearStrokes(true);
                    RestoreStrokes();
                }

                Topmost = true;
                BtnHideInkCanvas_Click(null, null);
                ApplyPendingWorkspaceDesktopDefaults();
                return;
            }

            if (workspaceMode == WorkspaceMode.Blackboard)
            {
                SetBlackboardVisualState(true);
                SaveStrokes(true);
                ClearStrokes(true);
                RestoreStrokes();
                Topmost = false;
            }
            else
            {
                SetBlackboardVisualState(false);
                SaveStrokes();
                ClearStrokes(true);
                RestoreStrokes(true);
                Topmost = true;
            }

            SyncWorkspaceCanvasVisibility();
            ApplyPendingWorkspaceDesktopDefaults();
        }

        private void SetBlackboardVisualState(bool isVisible)
        {
            GridBackgroundCover.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            if (isVisible)
            {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardLeftSide);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardCenterSide);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardRightSide);
            }
            else
            {
                AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);
            }
        }

        private void ApplyPendingWorkspaceDesktopDefaults()
        {
            if (WorkspaceSessionViewModel == null || !WorkspaceSessionViewModel.IsDesktopSession)
            {
                return;
            }

            if (WorkspaceSessionViewModel.ConsumeRestoreDefaultFloatingBarPosition())
            {
                RequestDefaultDesktopFloatingBarPosition();
            }

            if (WorkspaceSessionViewModel.ConsumeRestoreDefaultToolOnDesktopResume())
            {
                ShellViewModel.SetToolMode(ToolMode.Cursor, true, true);
            }
        }

        private void UpdateSelectionClonePrompt()
        {
            TextSelectionCloneToNewBoard.Text = ShellViewModel.IsDesktopAnnotationMode ? "衍至画板" : "衍至新页";
        }
    }
}
