using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink
{
    internal sealed class ShapeDrawingSessionState
    {
        public bool IsLongPressSelected { get; set; }

        public object? LastMouseDownSender { get; set; }

        public DateTime LastMouseDownTime { get; set; } = DateTime.MinValue;

        public int DrawMultiStepShapeCurrentStep { get; set; }

        public StrokeCollection DrawMultiStepShapeSpecialStrokeCollection { get; set; } = new();

        public double DrawMultiStepShapeSpecialParameter3 { get; set; }

        public bool IsFirstTouchCuboid { get; set; } = true;

        public Point CuboidFrontRectInitialPoint { get; set; }

        public Point CuboidFrontRectEndPoint { get; set; }

        public Stroke? LastTempStroke { get; set; }

        public StrokeCollection LastTempStrokeCollection { get; set; } = new();

        public bool IsWaitUntilNextTouchDown { get; set; }

        public bool IsMouseDown { get; set; }

        public int LastTouchDownTime { get; set; }

        public int LastTouchUpTime { get; set; }

        public Point InitialPoint { get; set; }

        public bool IsLastTouchEraser { get; set; }

        public List<int> ActiveTouchDeviceIds { get; } = [];

        public Point CenterPoint { get; set; }

        public InkCanvasEditingMode LastInkCanvasEditingMode { get; set; } = InkCanvasEditingMode.Ink;

        public bool IsSingleFingerDragMode { get; set; }

        public StrokeCollection NewStrokes { get; set; } = new();

        public List<Circle> Circles { get; } = [];
    }
}
