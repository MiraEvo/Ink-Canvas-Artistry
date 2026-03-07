using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Features.Ink
{
    internal sealed class InkHistorySessionState
    {
        public CommitReason CurrentCommitReason { get; set; } = CommitReason.UserInput;

        public StrokeCollection? ReplacedStroke { get; set; }

        public StrokeCollection? AddedStroke { get; set; }

        public StrokeCollection? CuboidStrokeCollection { get; set; }

        public Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>? StrokeManipulationHistory { get; set; }

        public Dictionary<Stroke, StylusPointCollection> StrokeInitialHistory { get; set; } = new();

        public Dictionary<string, Tuple<object, TransformGroup>>? ElementsManipulationHistory { get; set; }

        public Dictionary<string, object> ElementsInitialHistory { get; set; } = new();

        public Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributesHistory { get; set; } = new();

        public Dictionary<Guid, List<Stroke>> DrawingAttributesHistoryFlag { get; } = new()
        {
            { DrawingAttributeIds.Color, [] },
            { DrawingAttributeIds.DrawingFlags, [] },
            { DrawingAttributeIds.IsHighlighter, [] },
            { DrawingAttributeIds.StylusHeight, [] },
            { DrawingAttributeIds.StylusTip, [] },
            { DrawingAttributeIds.StylusTipTransform, [] },
            { DrawingAttributeIds.StylusWidth, [] }
        };

        public void ClearDrawingAttributesTracking()
        {
            DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
            foreach (var item in DrawingAttributesHistoryFlag)
            {
                item.Value.Clear();
            }
        }
    }
}
