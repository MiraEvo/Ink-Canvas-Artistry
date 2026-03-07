using System;
using System.Windows.Input;

namespace Ink_Canvas.Features.Ink
{
    internal sealed class InkGestureCoordinator
    {
        private readonly IInkGestureHost host;

        public InkGestureCoordinator(IInkGestureHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public InkGestureSessionState GestureState { get; } = new();

        public void HandleMultiTouchToggle()
        {
            host.ToggleMultiTouchMode();
        }

        public void HandleMainWindowTouchDown(TouchEventArgs e)
        {
            host.HandleMainWindowTouchDown(e, GestureState);
        }

        public void HandleGridTouchDown(TouchEventArgs e)
        {
            host.HandleGridTouchDown(e, GestureState);
        }

        public void HandlePreviewTouchDown(TouchEventArgs e)
        {
            if (!host.IsLoaded)
            {
                return;
            }

            host.HandlePreviewTouchDown(e, GestureState);
        }

        public void HandlePreviewTouchUp(TouchEventArgs e)
        {
            if (!host.IsLoaded)
            {
                return;
            }

            host.HandlePreviewTouchUp(e, GestureState);
        }

        public void HandleManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            host.HandleManipulationCompleted(e, GestureState);
        }

        public void HandleManipulationDelta(ManipulationDeltaEventArgs e)
        {
            host.HandleManipulationDelta(e, GestureState);
        }
    }
}
