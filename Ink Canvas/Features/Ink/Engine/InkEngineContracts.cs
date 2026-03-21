using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas.Features.Ink.Engine
{
    internal enum InkInputDeviceKind
    {
        Stylus = 0,
        Touch = 1,
        Mouse = 2
    }

    internal enum InkInputPhase
    {
        Begin = 0,
        Move = 1,
        End = 2,
        Cancel = 3
    }

    internal readonly record struct InkInputPoint(float X, float Y, float PressureFactor);

    internal readonly record struct InkInputSample(
        int PointerId,
        InkInputDeviceKind DeviceKind,
        InkInputPhase Phase,
        DateTimeOffset TimestampUtc,
        IReadOnlyList<InkInputPoint> Points);

    internal readonly record struct InkEngineOptions(bool EnablePressure, bool EnableRealtimePreview);

    internal sealed class InkEraserPath
    {
        public List<InkInputPoint> Points { get; } = [];
    }

    internal sealed class InkSelectionPath
    {
        public List<InkInputPoint> Points { get; } = [];
    }

    internal sealed class InkTransformCommand
    {
        public Matrix Matrix { get; set; } = Matrix.Identity;
    }

    internal readonly record struct InkHitTestResult(bool IsHit);

    internal readonly record struct InkSelectionResult(IReadOnlyList<Guid> SelectedStrokeIds);

    internal sealed class InkStrokeCommittedEventArgs : EventArgs
    {
        public InkStrokeCommittedEventArgs(InkStrokeModel stroke)
        {
            Stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
        }

        public InkStrokeModel Stroke { get; }
    }

    internal sealed class InkPreviewUpdatedEventArgs : EventArgs
    {
        public InkPreviewUpdatedEventArgs(InkDocumentSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public InkDocumentSnapshot Snapshot { get; }
    }

    internal interface IInkSurfaceHost
    {
        StrokeCollection CurrentStrokes { get; }

        DrawingAttributes CurrentDrawingAttributes { get; }

        IReadOnlyList<System.Windows.UIElement> CurrentElements { get; }
    }

    internal interface IInkEngine : IDisposable
    {
        string EngineId { get; }

        void Attach(IInkSurfaceHost host, InkEngineOptions options);

        void Detach();

        void ProcessInput(InkInputSample sample);

        void FlushFrame(TimeSpan frameTime);

        InkHitTestResult HitTestErase(InkEraserPath path);

        InkSelectionResult HitTestSelection(InkSelectionPath path);

        void ApplyTransform(InkTransformCommand command);

        InkDocumentSnapshot ExportSnapshot();

        void ImportSnapshot(InkDocumentSnapshot snapshot);

        event EventHandler<InkStrokeCommittedEventArgs>? StrokeCommitted;

        event EventHandler<InkPreviewUpdatedEventArgs>? PreviewUpdated;
    }
}
