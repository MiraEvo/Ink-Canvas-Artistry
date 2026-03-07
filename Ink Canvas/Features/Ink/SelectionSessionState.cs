using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink
{
    internal sealed class SelectionSessionState
    {
        public object? LastBorderMouseDownObject { get; set; }

        public bool IsStrokeSelectionCloneOn { get; set; }

        public bool IsGridInkCanvasSelectionCoverMouseDown { get; set; }

        public Point LastMousePoint { get; set; }

        public bool IsProgramChangeStrokeSelection { get; set; }

        public StrokeCollection StrokesSelectionClone { get; set; } = new();

        public List<UIElement> ElementsSelectionClone { get; set; } = [];

        public Point LastTouchPointOnGridInkCanvasCover { get; set; }

        public double BorderStrokeSelectionControlWidth { get; set; } = 695;

        public double BorderStrokeSelectionControlHeight { get; set; } = 104;
    }
}
