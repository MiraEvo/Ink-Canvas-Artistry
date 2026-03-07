using Ink_Canvas.Features.Ink;
using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : IInkHistoryHost
    {
        private readonly TimeMachine fallbackTimeMachine = new();

        private TimeMachine timeMachine => inkHistoryCoordinator?.TimeMachine ?? fallbackTimeMachine;

        private CommitReason _currentCommitType
        {
            get => InkHistoryState.CurrentCommitReason;
            set => InkHistoryState.CurrentCommitReason = value;
        }

        private StrokeCollection ReplacedStroke
        {
            get => InkHistoryState.ReplacedStroke;
            set => InkHistoryState.ReplacedStroke = value;
        }

        private StrokeCollection AddedStroke
        {
            get => InkHistoryState.AddedStroke;
            set => InkHistoryState.AddedStroke = value;
        }

        private StrokeCollection CuboidStrokeCollection
        {
            get => InkHistoryState.CuboidStrokeCollection;
            set => InkHistoryState.CuboidStrokeCollection = value;
        }

        private Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> StrokeManipulationHistory
        {
            get => InkHistoryState.StrokeManipulationHistory;
            set => InkHistoryState.StrokeManipulationHistory = value;
        }

        private Dictionary<Stroke, StylusPointCollection> StrokeInitialHistory
        {
            get => InkHistoryState.StrokeInitialHistory;
            set => InkHistoryState.StrokeInitialHistory = value;
        }

        private Dictionary<string, Tuple<object, TransformGroup>> ElementsManipulationHistory
        {
            get => InkHistoryState.ElementsManipulationHistory;
            set => InkHistoryState.ElementsManipulationHistory = value;
        }

        private Dictionary<string, object> ElementsInitialHistory
        {
            get => InkHistoryState.ElementsInitialHistory;
            set => InkHistoryState.ElementsInitialHistory = value;
        }

        private Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributesHistory
        {
            get => InkHistoryState.DrawingAttributesHistory;
            set => InkHistoryState.DrawingAttributesHistory = value;
        }

        private Dictionary<Guid, List<Stroke>> DrawingAttributesHistoryFlag => InkHistoryState.DrawingAttributesHistoryFlag;

        private StrokeCollection[] strokeCollections => WhiteboardState.StrokeCollections;

        private StrokeCollection lastTouchDownStrokeCollection
        {
            get => WhiteboardState.LastTouchDownStrokeCollection;
            set => WhiteboardState.LastTouchDownStrokeCollection = value;
        }

        private int CurrentWhiteboardIndex
        {
            get => WhiteboardState.CurrentWhiteboardIndex;
            set => WhiteboardState.CurrentWhiteboardIndex = value;
        }

        private int WhiteboardTotalCount
        {
            get => WhiteboardState.WhiteboardTotalCount;
            set => WhiteboardState.WhiteboardTotalCount = value;
        }

        private TimeMachineHistory[][] TimeMachineHistories => WhiteboardState.TimeMachineHistories;

        InkCanvas IInkHistoryHost.InkCanvas => inkCanvas;

        bool IInkHistoryHost.IsDesktopAnnotationMode => ShellViewModel.IsDesktopAnnotationMode;

        bool IInkHistoryHost.IsSelectionCoverDragging => isGridInkCanvasSelectionCoverMouseDown;

        bool IInkHistoryHost.IsHidingSubPanelsWhenInking
        {
            get => isHidingSubPanelsWhenInking;
            set => isHidingSubPanelsWhenInking = value;
        }

        void IInkHistoryHost.HideSubPanels() => HideSubPanels();

        void IInkHistoryHost.BackupCurrentStrokes() => BackupCurrentStrokes();

        void IInkHistoryHost.SetCommitReason(CommitReason reason) => _currentCommitType = reason;

        void IInkHistoryHost.ApplyHistoryItem(TimeMachineHistory item) => ApplyHistoryToCanvasCore(item);

        void IInkHistoryHost.ClearCanvasVisuals() => inkCanvas.Strokes.Clear();

        void IInkHistoryHost.ClearCanvasElements() => inkCanvas.Children.Clear();

        void IInkHistoryHost.HideSelectionCover() => GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

        void IInkHistoryHost.ClearSelection()
        {
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            inkCanvas.Select(new StrokeCollection());
        }

        void IInkHistoryHost.UpdateWhiteboardIndexDisplay(int currentIndex, int totalCount) => UpdateWhiteboardIndexDisplayCore(currentIndex, totalCount);

        private void ApplyHistoryToCanvas(TimeMachineHistory item)
        {
            ApplyHistoryToCanvasCore(item);
        }

        private void ApplyHistoryToCanvasCore(TimeMachineHistory item)
        {
            if (item.CommitType == TimeMachineHistoryType.UserInput)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Add(strokes);
                        }
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Remove(strokes);
                        }
                    }
                }

                return;
            }

            if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
            {
                if (item.StrokeHasBeenCleared)
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Remove(strokes);
                        }
                    }

                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Add(strokes);
                        }
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Add(strokes);
                        }
                    }

                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                        {
                            inkCanvas.Strokes.Remove(strokes);
                        }
                    }
                }

                return;
            }

            if (item.CommitType == TimeMachineHistoryType.Manipulation)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    if (item.StylusPointDictionary != null)
                    {
                        foreach (var currentStroke in item.StylusPointDictionary)
                        {
                            if (inkCanvas.Strokes.Contains(currentStroke.Key))
                            {
                                currentStroke.Key.StylusPoints = currentStroke.Value.Item2;
                            }
                        }
                    }

                    if (item.ElementsManipulationHistory != null)
                    {
                        foreach (var currentElement in item.ElementsManipulationHistory)
                        {
                            UIElement element = GetElementByTimestamp(inkCanvas, currentElement.Key);
                            if (element != null && inkCanvas.Children.Contains(element))
                            {
                                element.RenderTransform = currentElement.Value.Item2;
                            }
                            else if (currentElement.Value.Item1 is InkCanvasElementsHelper.ElementData elementData)
                            {
                                InkCanvas.SetLeft(elementData.FrameworkElement, elementData.SetLeftData);
                                InkCanvas.SetTop(elementData.FrameworkElement, elementData.SetTopData);
                                inkCanvas.Children.Add(elementData.FrameworkElement);
                                elementData.FrameworkElement.RenderTransform = currentElement.Value.Item2;
                            }
                        }
                    }
                }
                else
                {
                    if (item.StylusPointDictionary != null)
                    {
                        foreach (var currentStroke in item.StylusPointDictionary)
                        {
                            if (inkCanvas.Strokes.Contains(currentStroke.Key))
                            {
                                currentStroke.Key.StylusPoints = currentStroke.Value.Item1;
                            }
                        }
                    }

                    if (item.ElementsManipulationHistory != null)
                    {
                        foreach (var currentElement in item.ElementsManipulationHistory)
                        {
                            UIElement element = GetElementByTimestamp(inkCanvas, currentElement.Key);
                            if (element != null && inkCanvas.Children.Contains(element))
                            {
                                if (currentElement.Value.Item1 is TransformGroup transformGroup)
                                {
                                    element.RenderTransform = transformGroup;
                                }
                                else if (currentElement.Value.Item1 is InkCanvasElementsHelper.ElementData)
                                {
                                    inkCanvas.Children.Remove(element);
                                }
                            }
                        }
                    }
                }

                return;
            }

            if (item.CommitType == TimeMachineHistoryType.DrawingAttributes)
            {
                foreach (var currentStroke in item.DrawingAttributes)
                {
                    if (inkCanvas.Strokes.Contains(currentStroke.Key))
                    {
                        currentStroke.Key.DrawingAttributes = item.StrokeHasBeenCleared
                            ? currentStroke.Value.Item1
                            : currentStroke.Value.Item2;
                    }
                }

                return;
            }

            if (item.CommitType == TimeMachineHistoryType.Clear)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(currentStroke))
                            {
                                inkCanvas.Strokes.Add(currentStroke);
                            }
                        }
                    }

                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (inkCanvas.Strokes.Contains(replacedStroke))
                            {
                                inkCanvas.Strokes.Remove(replacedStroke);
                            }
                        }
                    }
                }
                else
                {
                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(replacedStroke))
                            {
                                inkCanvas.Strokes.Add(replacedStroke);
                            }
                        }
                    }

                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (inkCanvas.Strokes.Contains(currentStroke))
                            {
                                inkCanvas.Strokes.Remove(currentStroke);
                            }
                        }
                    }
                }

                return;
            }

            if (item.CommitType == TimeMachineHistoryType.ElementInsert)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    inkCanvas.Children.Add(item.Element);
                }
                else
                {
                    inkCanvas.Children.Remove(item.Element);
                }
            }
        }

        public UIElement GetElementByTimestamp(InkCanvas inkCanvas, string timestamp)
        {
            foreach (UIElement child in inkCanvas.Children)
            {
                if (child is FrameworkElement frameworkElement && frameworkElement.Name == timestamp)
                {
                    return child;
                }
            }

            return null;
        }

        private void CommitDrawingAttributesHistoryIfNeeded()
        {
            inkHistoryCoordinator?.CommitDrawingAttributes();
        }

        private void CommitPendingManipulationHistory()
        {
            inkHistoryCoordinator?.CommitPendingManipulation();
        }

        private void UpdateWhiteboardIndexDisplayCore(int currentIndex, int totalCount)
        {
            TextBlockWhiteBoardIndexInfo.Text = string.Format("{0} / {1}", currentIndex, totalCount);

            if (currentIndex == totalCount)
            {
                BoardLeftPannelNextPage1.Width = 26;
                BoardLeftPannelNextPage2.Width = 0;
                BoardLeftPannelNextPageTextBlock.Text = "加页";
            }
            else
            {
                BoardLeftPannelNextPage1.Width = 0;
                BoardLeftPannelNextPage2.Width = 26;
                BoardLeftPannelNextPageTextBlock.Text = "下一页";
            }

            BtnWhiteBoardSwitchPrevious.IsEnabled = currentIndex != 1;
            BoardLeftPannelNextPage1.IsEnabled = currentIndex != 99;
            BtnBoardAddPage.IsEnabled = totalCount != 99;
        }
    }
}
