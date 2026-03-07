using System.Windows.Input;

namespace Ink_Canvas.Features.Ink.Hosts
{
    internal interface IInkGestureHost
    {
        bool IsLoaded { get; }

        bool IsInMultiTouchMode { get; }

        void ToggleMultiTouchMode();

        void HandleMainWindowTouchDown(TouchEventArgs e, InkGestureSessionState gestureState);

        void HandleGridTouchDown(TouchEventArgs e, InkGestureSessionState gestureState);

        void HandlePreviewTouchDown(TouchEventArgs e, InkGestureSessionState gestureState);

        void HandlePreviewTouchUp(TouchEventArgs e, InkGestureSessionState gestureState);

        void HandleManipulationCompleted(ManipulationCompletedEventArgs e, InkGestureSessionState gestureState);

        void HandleManipulationDelta(ManipulationDeltaEventArgs e, InkGestureSessionState gestureState);
    }
}

