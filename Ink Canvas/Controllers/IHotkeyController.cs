namespace Ink_Canvas.Controllers
{
    public interface IHotkeyController
    {
        void ExitPresentation();

        void ClearCanvas();

        void CaptureScreen();

        void ToggleCanvasVisibility();

        void ActivatePen();

        void ExitDrawMode();

        void ToggleBlackboard();
    }
}
