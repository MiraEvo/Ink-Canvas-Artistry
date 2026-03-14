using System;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Services
{
    internal static class InkStrokeDrawingAttributesHelper
    {
        public static DrawingAttributes CreateFreehandDrawingAttributes(DrawingAttributes source)
        {
            ArgumentNullException.ThrowIfNull(source);

            DrawingAttributes clone = source.Clone();
            clone.FitToCurve = true;
            return clone;
        }

        public static DrawingAttributes CreateShapeDrawingAttributes(DrawingAttributes source, bool fitToCurve = false)
        {
            ArgumentNullException.ThrowIfNull(source);

            DrawingAttributes clone = source.Clone();
            clone.FitToCurve = fitToCurve;
            return clone;
        }
    }
}
