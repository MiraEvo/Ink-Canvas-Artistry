using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Controllers
{
    public interface IWorkspaceSessionController
    {
        void Initialize(bool isCanvasVisible);

        void ApplyWorkspaceMode(WorkspaceMode mode, bool isCanvasVisible);

        void EnterBlackboard();

        void ExitBlackboard(bool restoreDefaultTool, bool restoreFloatingBarPosition, bool clearStrokes);

        void RestoreDesktopDefaultsAfterPresentation();

        void SyncCanvasVisibility(bool isCanvasVisible);
    }
}
