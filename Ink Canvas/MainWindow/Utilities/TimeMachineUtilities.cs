using Ink_Canvas.Features.Ink;
using Ink_Canvas.Helpers;
using System;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private bool IsEraseByPoint => inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;

        private void TimeMachine_OnUndoStateChanged(bool status)
        {
            Icon_Undo.IsEnabled = status;
        }

        private void TimeMachine_OnRedoStateChanged(bool status)
        {
            Icon_Redo.IsEnabled = status;
        }

        private void StrokesOnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            StrokeCollection removed = e.Removed;
            foreach (var stroke in removed)
            {
                stroke.StylusPointsChanged -= Stroke_StylusPointsChanged;
                stroke.StylusPointsReplaced -= Stroke_StylusPointsReplaced;
                stroke.DrawingAttributesChanged -= Stroke_DrawingAttributesChanged;
                StrokeInitialHistory.Remove(stroke);
            }

            StrokeCollection added = e.Added;
            foreach (var stroke in added)
            {
                stroke.StylusPointsChanged += Stroke_StylusPointsChanged;
                stroke.StylusPointsReplaced += Stroke_StylusPointsReplaced;
                stroke.DrawingAttributesChanged += Stroke_DrawingAttributesChanged;
                StrokeInitialHistory[stroke] = stroke.StylusPoints.Clone();
            }

            inkHistoryCoordinator?.HandleStrokesChanged(e, IsEraseByPoint);
        }

        private void Stroke_DrawingAttributesChanged(object sender, PropertyDataChangedEventArgs e)
        {
            if (sender is Stroke stroke)
            {
                inkHistoryCoordinator?.TrackStrokeDrawingAttributesChanged(stroke, e);
            }
        }

        private void Stroke_StylusPointsReplaced(object sender, StylusPointsReplacedEventArgs e)
        {
            if (sender is Stroke stroke)
            {
                inkHistoryCoordinator?.TrackStrokeStylusPointsReplaced(stroke, e.NewStylusPoints);
            }
        }

        private void Stroke_StylusPointsChanged(object sender, EventArgs e)
        {
            if (sender is not Stroke stroke)
            {
                return;
            }

            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            int count = selectedStrokes.Count > 0 ? selectedStrokes.Count : inkCanvas.Strokes.Count;
            if (dec.Count != 0 || isGridInkCanvasSelectionCoverMouseDown)
            {
                if (StrokeManipulationHistory == null)
                {
                    StrokeManipulationHistory = new System.Collections.Generic.Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>();
                }

                StrokeManipulationHistory[stroke] =
                    new Tuple<StylusPointCollection, StylusPointCollection>(StrokeInitialHistory[stroke], stroke.StylusPoints.Clone());
                return;
            }

            inkHistoryCoordinator?.TrackStrokeStylusPointsChanged(stroke, count);
        }

        private void ToCommitStrokeManipulationHistoryAfterMouseUp()
        {
            inkHistoryCoordinator?.CommitPendingManipulation();
        }
    }
}
