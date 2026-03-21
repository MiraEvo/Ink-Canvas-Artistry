using Ink_Canvas.Features.Ink.Engine;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow : IInkSurfaceHost
    {
        StrokeCollection IInkSurfaceHost.CurrentStrokes => inkCanvas.Strokes;

        DrawingAttributes IInkSurfaceHost.CurrentDrawingAttributes => drawingAttributes;

        IReadOnlyList<UIElement> IInkSurfaceHost.CurrentElements => inkCanvas.Children.Cast<UIElement>().ToList();
    }
}
