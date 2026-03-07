using System;

namespace Ink_Canvas.Controllers
{
    public sealed class HotkeyController : IHotkeyController
    {
        private readonly Action exitPresentation;
        private readonly Action clearCanvas;
        private readonly Action captureScreen;
        private readonly Action toggleCanvasVisibility;
        private readonly Action activatePen;
        private readonly Action exitDrawMode;
        private readonly Action toggleBlackboard;

        public HotkeyController(
            Action exitPresentation,
            Action clearCanvas,
            Action captureScreen,
            Action toggleCanvasVisibility,
            Action activatePen,
            Action exitDrawMode,
            Action toggleBlackboard)
        {
            this.exitPresentation = exitPresentation;
            this.clearCanvas = clearCanvas;
            this.captureScreen = captureScreen;
            this.toggleCanvasVisibility = toggleCanvasVisibility;
            this.activatePen = activatePen;
            this.exitDrawMode = exitDrawMode;
            this.toggleBlackboard = toggleBlackboard;
        }

        public void ExitPresentation()
        {
            exitPresentation?.Invoke();
        }

        public void ClearCanvas()
        {
            clearCanvas?.Invoke();
        }

        public void CaptureScreen()
        {
            captureScreen?.Invoke();
        }

        public void ToggleCanvasVisibility()
        {
            toggleCanvasVisibility?.Invoke();
        }

        public void ActivatePen()
        {
            activatePen?.Invoke();
        }

        public void ExitDrawMode()
        {
            exitDrawMode?.Invoke();
        }

        public void ToggleBlackboard()
        {
            toggleBlackboard?.Invoke();
        }
    }
}
