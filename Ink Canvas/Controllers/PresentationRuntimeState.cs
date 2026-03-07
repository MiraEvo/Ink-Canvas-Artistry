using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Controllers
{
    internal readonly record struct PresentationRuntimeState(
        bool IsConnected,
        PresentationProvider Provider,
        string PresentationIdentity,
        string PresentationName,
        int SlideCount,
        int CurrentSlideIndex,
        bool IsSlideShowRunning)
    {
        public static PresentationRuntimeState Disconnected =>
            new(false, PresentationProvider.None, string.Empty, string.Empty, 0, 0, false);
    }
}
