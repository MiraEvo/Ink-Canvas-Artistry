using Ink_Canvas.Features.Ink.Services;
using Ink_Canvas.Services.Logging;
using System;
using System.Windows.Controls;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal interface IInkRecognitionEngine
    {
        string VersionId { get; }

        void HandleStrokeCollected(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            InkCanvasStrokeCollectedEventArgs e);
    }

    internal sealed class InkRecognitionV2Engine : IInkRecognitionEngine
    {
        private readonly InkRecognitionService service;

        public InkRecognitionV2Engine(IAppLogger logger)
        {
            service = new InkRecognitionService(logger);
        }

        public string VersionId => InkRuntimeDefaults.RecognizerV2;

        public void HandleStrokeCollected(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            InkCanvasStrokeCollectedEventArgs e)
        {
            service.HandleStrokeCollected(inkCanvasHost, inkHistoryHost, shapeDrawingState, e);
        }
    }

    internal sealed class InkRecognitionV1Engine : IInkRecognitionEngine
    {
        private readonly InkRecognitionService service;
        private readonly IAppLogger logger;
        private bool hasLoggedFallback;

        public InkRecognitionV1Engine(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkRecognitionV1Engine));
            service = new InkRecognitionService(this.logger);
        }

        public string VersionId => InkRuntimeDefaults.RecognizerV1;

        public void HandleStrokeCollected(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            InkCanvasStrokeCollectedEventArgs e)
        {
            if (!hasLoggedFallback)
            {
                hasLoggedFallback = true;
                logger.Event("Ink Recognition | V1 fallback is active.");
            }

            service.HandleStrokeCollected(inkCanvasHost, inkHistoryHost, shapeDrawingState, e);
        }
    }
}
