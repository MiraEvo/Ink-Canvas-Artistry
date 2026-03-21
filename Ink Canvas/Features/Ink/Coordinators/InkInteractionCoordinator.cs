using Ink_Canvas.ViewModels;
using Ink_Canvas.Features.Ink.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal sealed class InkInteractionCoordinator
    {
        private readonly IInkCanvasHost inkCanvasHost;
        private readonly IInkHistoryHost inkHistoryHost;
        private readonly SettingsViewModel settingsViewModel;
        private readonly ShellViewModel shellViewModel;
        private readonly InputStateViewModel inputStateViewModel;
        private readonly IInkRecognitionEngine recognitionEngineV1;
        private readonly IInkRecognitionEngine recognitionEngineV2;
        private readonly Func<InkRecognizerKind> recognizerResolver;

        public InkInteractionCoordinator(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            SettingsViewModel settingsViewModel,
            ShellViewModel shellViewModel,
            InputStateViewModel inputStateViewModel,
            IInkRecognitionEngine recognitionEngineV1,
            IInkRecognitionEngine recognitionEngineV2,
            Func<InkRecognizerKind> recognizerResolver)
        {
            this.inkCanvasHost = inkCanvasHost ?? throw new ArgumentNullException(nameof(inkCanvasHost));
            this.inkHistoryHost = inkHistoryHost ?? throw new ArgumentNullException(nameof(inkHistoryHost));
            this.settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            this.shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            this.inputStateViewModel = inputStateViewModel ?? throw new ArgumentNullException(nameof(inputStateViewModel));
            this.recognitionEngineV1 = recognitionEngineV1 ?? throw new ArgumentNullException(nameof(recognitionEngineV1));
            this.recognitionEngineV2 = recognitionEngineV2 ?? throw new ArgumentNullException(nameof(recognitionEngineV2));
            this.recognizerResolver = recognizerResolver ?? throw new ArgumentNullException(nameof(recognizerResolver));
        }

        public ShapeDrawingSessionState ShapeDrawingState { get; } = new();

        public SelectionSessionState SelectionState { get; } = new();

        public async Task HandleShapeButtonMouseDownAsync(object sender)
        {
            ShapeDrawingState.LastMouseDownSender = sender;
            ShapeDrawingState.LastMouseDownTime = DateTime.Now;

            await Task.Delay(500);

            if (ShapeDrawingState.LastMouseDownSender != sender
                || sender is not UIElement shapeButton
                || !inkCanvasHost.TryGetLongPressShapeTool(sender, out ShapeToolKind tool))
            {
                return;
            }

            ShapeDrawingState.LastMouseDownSender = null;
            inkCanvasHost.AnimateLongPressPreview(shapeButton);
            PrepareForShapeDrawing();
            inkCanvasHost.BeginShapeDrawing(tool);
            inkCanvasHost.SetCanvasManipulationEnabled(true);
            inkCanvasHost.CancelSingleFingerDragMode();
            ShapeDrawingState.IsLongPressSelected = true;

            if (ShapeDrawingState.IsSingleFingerDragMode)
            {
                inkCanvasHost.ToggleSingleFingerDragMode();
            }
        }

        public void HandleShapeToolSelection(
            object? sender,
            ShapeToolKind tool,
            UIElement previewElement,
            bool resetMultiStep = false,
            bool initializeCuboid = false)
        {
            PrepareForShapeDrawing();

            if (ShapeDrawingState.LastMouseDownSender == sender)
            {
                inkCanvasHost.BeginShapeDrawing(tool);
            }

            ShapeDrawingState.LastMouseDownSender = null;
            if (ShapeDrawingState.IsLongPressSelected)
            {
                if (inkCanvasHost.IsShapePanelAutoHideEnabled)
                {
                    inkCanvasHost.CollapseShapePanel(true);
                }

                inkCanvasHost.ResetLongPressPreview(previewElement);
            }

            if (resetMultiStep)
            {
                ShapeDrawingState.DrawMultiStepShapeCurrentStep = 0;
            }

            if (initializeCuboid)
            {
                inkCanvasHost.InitializeCuboidDrawing();
            }

            inkCanvasHost.DrawShapePromptToPen();
        }

        public void HandlePenButtonClicked()
        {
            inkCanvasHost.SetToolModeToPen();
            inkCanvasHost.EndShapeDrawing(true);
            inkCanvasHost.SetCanvasManipulationEnabled(true);
            inkCanvasHost.CancelSingleFingerDragMode();
            ShapeDrawingState.IsLongPressSelected = false;
        }

        public void HandleSelectionBorderMouseDown(object sender)
        {
            SelectionState.LastBorderMouseDownObject = sender;
        }

        public void ToggleSelectionClone()
        {
            SelectionState.IsStrokeSelectionCloneOn = !SelectionState.IsStrokeSelectionCloneOn;
            inkCanvasHost.UpdateSelectionCloneToggleVisual(SelectionState.IsStrokeSelectionCloneOn);
        }

        public void HandleSelectionCloneToBoardOrNewPage()
        {
            inkCanvasHost.CommitSelectionCloneToBoardOrNewPage();
        }

        public void HandleSelectionDelete(object sender, RoutedEventArgs e)
        {
            inkCanvasHost.DeleteSelection(sender, e);
        }

        public void HandleSelectionThicknessChanged(double multiplier)
        {
            inkCanvasHost.ChangeSelectedStrokeThickness(multiplier);
        }

        public void HandleSelectionThicknessRestore()
        {
            inkCanvasHost.RestoreSelectedStrokeThickness();
        }

        public void HandleSelectionSaveToImage()
        {
            inkCanvasHost.SaveSelectionToImage();
        }

        public void HandleSelectionMatrixTransform(int type)
        {
            inkCanvasHost.ApplySelectionMatrixTransform(type);
        }

        public void HandleStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            IInkRecognitionEngine engine = recognizerResolver() == InkRecognizerKind.V1
                ? recognitionEngineV1
                : recognitionEngineV2;
            engine.HandleStrokeCollected(inkCanvasHost, inkHistoryHost, ShapeDrawingState, e);
        }

        private void PrepareForShapeDrawing()
        {
            if (inkCanvasHost.IsInMultiTouchMode)
            {
                inkCanvasHost.SetMultiTouchModeEnabled(false);
                inputStateViewModel.SetTwoFingerGestureTemporarilySuspended(true);
            }
        }
    }
}

