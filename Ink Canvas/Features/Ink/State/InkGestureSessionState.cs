using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.State
{
    internal sealed class InkGestureSessionState
    {
        public List<int> ActiveTouchDeviceIds { get; } = [];

        public Point CenterPoint { get; set; }

        public InkCanvasEditingMode LastInkCanvasEditingMode { get; set; } = InkCanvasEditingMode.Ink;

        public bool IsLastTouchEraser { get; set; }

        public bool IsWaitUntilNextTouchDown { get; set; }

        public StrokeCollection LastTouchDownStrokeCollection { get; set; } = new();

        public bool IsSingleFingerDragMode { get; set; }
    }
}

