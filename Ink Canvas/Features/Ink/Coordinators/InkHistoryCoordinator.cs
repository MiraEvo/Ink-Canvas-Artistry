using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Ink_Canvas.Services.Logging;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal sealed class InkHistoryCoordinator
    {
        private readonly IInkHistoryHost host;
        private readonly IAppLogger logger;

        public InkHistoryCoordinator(IInkHistoryHost host, IAppLogger logger)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkHistoryCoordinator));
        }

        public TimeMachine TimeMachine { get; } = new();

        public InkHistorySessionState HistoryState { get; } = new();

        public WhiteboardSessionState WhiteboardState { get; } = new();

        public void BackupCurrentStrokes()
        {
            WhiteboardState.LastTouchDownStrokeCollection = host.InkCanvas.Strokes.Clone();
            WhiteboardState.StrokeCollections[GetCurrentHistoryIndex()] = WhiteboardState.LastTouchDownStrokeCollection;
        }

        public void SetCommitReason(CommitReason reason)
        {
            HistoryState.CurrentCommitReason = reason;
        }

        public void RestoreUserInputCommitReason()
        {
            HistoryState.CurrentCommitReason = CommitReason.UserInput;
        }

        public void HandleStrokesChanged(StrokeCollectionChangedEventArgs e, bool isEraseByPoint)
        {
            if (!host.IsHidingSubPanelsWhenInking)
            {
                host.IsHidingSubPanelsWhenInking = true;
                host.HideSubPanels();
            }

            foreach (var stroke in e?.Removed)
            {
                HistoryState.StrokeInitialHistory.Remove(stroke);
            }

            foreach (var stroke in e?.Added)
            {
                HistoryState.StrokeInitialHistory[stroke] = stroke.StylusPoints.Clone();
            }

            if (HistoryState.CurrentCommitReason == CommitReason.CodeInput
                || HistoryState.CurrentCommitReason == CommitReason.ShapeDrawing)
            {
                return;
            }

            if ((e.Added.Count != 0 || e.Removed.Count != 0) && isEraseByPoint)
            {
                HistoryState.AddedStroke ??= new StrokeCollection();
                HistoryState.ReplacedStroke ??= new StrokeCollection();
                HistoryState.AddedStroke.Add(e.Added);
                HistoryState.ReplacedStroke.Add(e.Removed);
                return;
            }

            if (e.Added.Count != 0)
            {
                if (HistoryState.CurrentCommitReason == CommitReason.ShapeRecognition)
                {
                    CommitShapeRecognition(HistoryState.ReplacedStroke, e.Added);
                    HistoryState.ReplacedStroke = null;
                    return;
                }

                CommitUserInput(e.Added);
                return;
            }

            if (e.Removed.Count != 0)
            {
                if (HistoryState.CurrentCommitReason == CommitReason.ShapeRecognition)
                {
                    HistoryState.ReplacedStroke = e.Removed;
                    return;
                }

                if (!isEraseByPoint || HistoryState.CurrentCommitReason == CommitReason.ClearingCanvas)
                {
                    CommitErase(e.Removed);
                }
            }
        }

        public void TrackStrokeDrawingAttributesChanged(Stroke stroke, PropertyDataChangedEventArgs e)
        {
            if (stroke == null)
            {
                return;
            }

            var currentValue = stroke.DrawingAttributes.Clone();
            HistoryState.DrawingAttributesHistory.TryGetValue(stroke, out var previousTuple);
            var previousValue = previousTuple?.Item1 ?? currentValue.Clone();
            var needUpdateValue = !HistoryState.DrawingAttributesHistoryFlag[e.PropertyGuid].Contains(stroke);

            if (needUpdateValue)
            {
                HistoryState.DrawingAttributesHistoryFlag[e.PropertyGuid].Add(stroke);
                Debug.Write(e.PreviousValue.ToString());
            }

            if (e.PropertyGuid == DrawingAttributeIds.Color && needUpdateValue)
            {
                previousValue.Color = (Color)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.IsHighlighter && needUpdateValue)
            {
                previousValue.IsHighlighter = (bool)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.StylusHeight && needUpdateValue)
            {
                previousValue.Height = (double)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.StylusWidth && needUpdateValue)
            {
                previousValue.Width = (double)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.StylusTip && needUpdateValue)
            {
                previousValue.StylusTip = (StylusTip)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.StylusTipTransform && needUpdateValue)
            {
                previousValue.StylusTipTransform = (Matrix)e.PreviousValue;
            }
            if (e.PropertyGuid == DrawingAttributeIds.DrawingFlags && needUpdateValue)
            {
                previousValue.IgnorePressure = (bool)e.PreviousValue;
            }

            HistoryState.DrawingAttributesHistory[stroke] = new Tuple<DrawingAttributes, DrawingAttributes>(previousValue, currentValue);
        }

        public void TrackStrokeStylusPointsReplaced(Stroke stroke, StylusPointCollection newStylusPoints)
        {
            if (stroke == null)
            {
                return;
            }

            HistoryState.StrokeInitialHistory[stroke] = newStylusPoints.Clone();
        }

        public void TrackStrokeStylusPointsChanged(Stroke stroke, int expectedCount)
        {
            if (stroke == null)
            {
                return;
            }

            HistoryState.StrokeManipulationHistory ??= new Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>();
            HistoryState.StrokeManipulationHistory[stroke] =
                new Tuple<StylusPointCollection, StylusPointCollection>(
                    HistoryState.StrokeInitialHistory[stroke],
                    stroke.StylusPoints.Clone());

            if (HistoryState.StrokeManipulationHistory.Count == expectedCount
                && !host.IsSelectionCoverDragging)
            {
                CommitPendingManipulation();
            }
        }

        public void CommitPendingManipulation()
        {
            if (HistoryState.StrokeManipulationHistory == null && HistoryState.ElementsManipulationHistory == null)
            {
                return;
            }

            if (HistoryState.StrokeManipulationHistory?.Count > 0 || HistoryState.ElementsManipulationHistory?.Count > 0)
            {
                TimeMachine.CommitStrokeManipulationHistory(
                    HistoryState.StrokeManipulationHistory,
                    HistoryState.ElementsManipulationHistory);

                if (HistoryState.StrokeManipulationHistory != null)
                {
                    foreach (var item in HistoryState.StrokeManipulationHistory)
                    {
                        HistoryState.StrokeInitialHistory[item.Key] = item.Value.Item2;
                    }

                    HistoryState.StrokeManipulationHistory = null;
                }

                if (HistoryState.ElementsManipulationHistory != null)
                {
                    foreach (var item in HistoryState.ElementsManipulationHistory)
                    {
                        HistoryState.ElementsInitialHistory[item.Key] = item.Value.Item2;
                    }

                    HistoryState.ElementsManipulationHistory = null;
                }
            }
        }

        public void CommitDrawingAttributes()
        {
            if (HistoryState.DrawingAttributesHistory.Count == 0)
            {
                return;
            }

            TimeMachine.CommitStrokeDrawingAttributesHistory(HistoryState.DrawingAttributesHistory);
            HistoryState.ClearDrawingAttributesTracking();
        }

        public void CommitUserInput(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                return;
            }

            TimeMachine.CommitStrokeUserInputHistory(strokes);
        }

        public void CommitShapeRecognition(StrokeCollection replacedStroke, StrokeCollection addedStrokes)
        {
            if (addedStrokes == null || addedStrokes.Count == 0)
            {
                return;
            }

            TimeMachine.CommitStrokeShapeHistory(replacedStroke, addedStrokes);
        }

        public void CommitErase(StrokeCollection strokes, StrokeCollection sourceStroke = null)
        {
            if ((strokes == null || strokes.Count == 0)
                && (sourceStroke == null || sourceStroke.Count == 0))
            {
                return;
            }

            TimeMachine.CommitStrokeEraseHistory(strokes, sourceStroke);
        }

        public void CommitElementInsert(UIElement element, bool strokeHasBeenCleared = false)
        {
            if (element == null)
            {
                return;
            }

            TimeMachine.CommitElementInsertHistory(element, strokeHasBeenCleared);
        }

        public void Undo()
        {
            var item = TimeMachine.Undo();
            ApplyHistory(item);
        }

        public void Redo()
        {
            var item = TimeMachine.Redo();
            ApplyHistory(item);
        }

        public void ClearCanvas(bool isErasedByCode)
        {
            HistoryState.CurrentCommitReason = isErasedByCode ? CommitReason.CodeInput : CommitReason.ClearingCanvas;
            host.ClearCanvasVisuals();
            host.ClearCanvasElements();
            HistoryState.CurrentCommitReason = CommitReason.UserInput;
        }

        public void ClearHistory()
        {
            TimeMachine.ClearStrokeHistory();
        }

        public void SaveCurrentPageHistory(bool isBackupMain = false)
        {
            var history = TimeMachine.ExportTimeMachineHistory();
            WhiteboardState.TimeMachineHistories[isBackupMain ? 0 : WhiteboardState.CurrentWhiteboardIndex] = history;
            TimeMachine.ClearStrokeHistory();
        }

        public void RestoreCurrentPageHistory(bool isBackupMain = false)
        {
            TimeMachineHistory[] history = isBackupMain
                ? WhiteboardState.TimeMachineHistories[0]
                : WhiteboardState.TimeMachineHistories[WhiteboardState.CurrentWhiteboardIndex];

            if (history == null)
            {
                return;
            }

            try
            {
                TimeMachine.ImportTimeMachineHistory(history);
                foreach (var item in history)
                {
                    ApplyHistory(item);
                }
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "Board | Failed to restore whiteboard history");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "Board | Failed to apply restored whiteboard history");
            }
        }

        public bool MoveToPreviousWhiteboardPage()
        {
            if (WhiteboardState.CurrentWhiteboardIndex <= 1)
            {
                return false;
            }

            SaveCurrentPageHistory();
            ClearCanvas(true);
            WhiteboardState.CurrentWhiteboardIndex--;
            RestoreCurrentPageHistory();
            host.UpdateWhiteboardIndexDisplay(WhiteboardState.CurrentWhiteboardIndex, WhiteboardState.WhiteboardTotalCount);
            return true;
        }

        public bool MoveToNextWhiteboardPage()
        {
            if (WhiteboardState.CurrentWhiteboardIndex >= WhiteboardState.WhiteboardTotalCount)
            {
                return false;
            }

            SaveCurrentPageHistory();
            ClearCanvas(true);
            WhiteboardState.CurrentWhiteboardIndex++;
            RestoreCurrentPageHistory();
            host.UpdateWhiteboardIndexDisplay(WhiteboardState.CurrentWhiteboardIndex, WhiteboardState.WhiteboardTotalCount);
            return true;
        }

        public bool AddWhiteboardPage()
        {
            if (WhiteboardState.WhiteboardTotalCount >= 99)
            {
                return false;
            }

            SaveCurrentPageHistory();
            ClearCanvas(true);
            WhiteboardState.WhiteboardTotalCount++;
            WhiteboardState.CurrentWhiteboardIndex++;
            if (WhiteboardState.CurrentWhiteboardIndex != WhiteboardState.WhiteboardTotalCount)
            {
                for (int i = WhiteboardState.WhiteboardTotalCount; i > WhiteboardState.CurrentWhiteboardIndex; i--)
                {
                    WhiteboardState.TimeMachineHistories[i] = WhiteboardState.TimeMachineHistories[i - 1];
                }
            }

            host.UpdateWhiteboardIndexDisplay(WhiteboardState.CurrentWhiteboardIndex, WhiteboardState.WhiteboardTotalCount);
            return true;
        }

        public bool DeleteWhiteboardPage()
        {
            if (WhiteboardState.WhiteboardTotalCount <= 1)
            {
                return false;
            }

            ClearCanvas(true);
            if (WhiteboardState.CurrentWhiteboardIndex != WhiteboardState.WhiteboardTotalCount)
            {
                for (int i = WhiteboardState.CurrentWhiteboardIndex; i <= WhiteboardState.WhiteboardTotalCount; i++)
                {
                    WhiteboardState.TimeMachineHistories[i] = WhiteboardState.TimeMachineHistories[i + 1];
                }
            }
            else
            {
                WhiteboardState.CurrentWhiteboardIndex--;
            }

            WhiteboardState.WhiteboardTotalCount--;
            RestoreCurrentPageHistory();
            host.UpdateWhiteboardIndexDisplay(WhiteboardState.CurrentWhiteboardIndex, WhiteboardState.WhiteboardTotalCount);
            return true;
        }

        private void ApplyHistory(TimeMachineHistory item)
        {
            HistoryState.CurrentCommitReason = CommitReason.CodeInput;
            host.ApplyHistoryItem(item);
            HistoryState.CurrentCommitReason = CommitReason.UserInput;
        }

        private int GetCurrentHistoryIndex()
        {
            return host.IsDesktopAnnotationMode ? 0 : WhiteboardState.CurrentWhiteboardIndex;
        }
    }
}

