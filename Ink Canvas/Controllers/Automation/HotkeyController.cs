using System;

namespace Ink_Canvas.Controllers.Automation
{
    public sealed class HotkeyController(
        Action exitPresentation,
        Action clearCanvas,
        Action captureScreen,
        Action toggleCanvasVisibility,
        Action activatePen,
        Action exitDrawMode,
        Action toggleBlackboard) : IHotkeyController
    {
        public void ExitPresentation() => exitPresentation();

        public void ClearCanvas() => clearCanvas();

        public void CaptureScreen() => captureScreen();

        public void ToggleCanvasVisibility() => toggleCanvasVisibility();

        public void ActivatePen() => activatePen();

        public void ExitDrawMode() => exitDrawMode();

        public void ToggleBlackboard() => toggleBlackboard();
    }
}

