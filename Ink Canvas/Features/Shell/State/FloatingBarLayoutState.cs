using System.Windows;

namespace Ink_Canvas.Features.Shell.State
{
    internal sealed class FloatingBarLayoutState
    {
        public bool IsDragDropInEffect { get; set; }

        public bool IsMarginAnimationRunning { get; set; }

        public Point CurrentPointerPosition { get; set; }

        public Point MouseDownPosition { get; set; }

        public Point DesktopPosition { get; set; } = new(-1, -1);

        public Point PresentationPosition { get; set; } = new(-1, -1);

        public bool ShouldRestoreDefaultDesktopPosition { get; set; }
    }
}

