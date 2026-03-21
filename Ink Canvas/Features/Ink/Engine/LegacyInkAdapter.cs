using Ink_Canvas.Features.Ink.Services;
using System;
using System.Collections.Generic;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Engine
{
    internal sealed class LegacyInkAdapter : IInkEngine
    {
        private readonly Dictionary<int, List<InkInputPoint>> activePointerPoints = [];
        private IInkSurfaceHost? host;
        private InkEngineOptions options;
        private bool disposed;

        public string EngineId => InkRuntimeDefaults.InkBackendLegacy;

        public event EventHandler<InkStrokeCommittedEventArgs>? StrokeCommitted;

        public event EventHandler<InkPreviewUpdatedEventArgs>? PreviewUpdated;

        public void Attach(IInkSurfaceHost host, InkEngineOptions options)
        {
            ArgumentNullException.ThrowIfNull(host);
            ThrowIfDisposed();
            this.host = host;
            this.options = options;
            activePointerPoints.Clear();
        }

        public void Detach()
        {
            if (disposed)
            {
                return;
            }

            activePointerPoints.Clear();
            host = null;
        }

        public void ProcessInput(InkInputSample sample)
        {
            ThrowIfDisposed();
            if (host == null)
            {
                return;
            }

            switch (sample.Phase)
            {
                case InkInputPhase.Begin:
                    activePointerPoints[sample.PointerId] = ToMutableList(sample.Points);
                    break;
                case InkInputPhase.Move:
                    if (activePointerPoints.TryGetValue(sample.PointerId, out List<InkInputPoint>? points))
                    {
                        points.AddRange(sample.Points);
                        RaisePreviewUpdatedIfNeeded();
                    }
                    break;
                case InkInputPhase.End:
                case InkInputPhase.Cancel:
                    if (activePointerPoints.TryGetValue(sample.PointerId, out List<InkInputPoint>? finishedPoints)
                        && finishedPoints.Count > 0
                        && sample.Phase == InkInputPhase.End)
                    {
                        InkStrokeModel model = BuildCommittedStroke(finishedPoints);
                        StrokeCommitted?.Invoke(this, new InkStrokeCommittedEventArgs(model));
                    }

                    activePointerPoints.Remove(sample.PointerId);
                    break;
            }
        }

        public void FlushFrame(TimeSpan frameTime)
        {
            ThrowIfDisposed();
            if (!options.EnableRealtimePreview)
            {
                return;
            }

            RaisePreviewUpdatedIfNeeded();
        }

        public InkHitTestResult HitTestErase(InkEraserPath path)
        {
            ArgumentNullException.ThrowIfNull(path);
            ThrowIfDisposed();
            return new InkHitTestResult(path.Points.Count > 0);
        }

        public InkSelectionResult HitTestSelection(InkSelectionPath path)
        {
            ArgumentNullException.ThrowIfNull(path);
            ThrowIfDisposed();
            return new InkSelectionResult([]);
        }

        public void ApplyTransform(InkTransformCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            ThrowIfDisposed();
        }

        public InkDocumentSnapshot ExportSnapshot()
        {
            ThrowIfDisposed();
            if (host == null)
            {
                return new InkDocumentSnapshot(new InkDocumentModel());
            }

            InkDocumentModel document = InkDocumentModelAdapter.FromStrokeAndElementCollections(host.CurrentStrokes, host.CurrentElements);
            return new InkDocumentSnapshot(document);
        }

        public void ImportSnapshot(InkDocumentSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ThrowIfDisposed();
            // Current migration stage keeps runtime source in InkCanvas host;
            // snapshot import is a no-op for legacy path to avoid changing behavior.
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Detach();
        }

        private void RaisePreviewUpdatedIfNeeded()
        {
            if (!options.EnableRealtimePreview || host == null)
            {
                return;
            }

            InkDocumentSnapshot snapshot = ExportSnapshot();
            PreviewUpdated?.Invoke(this, new InkPreviewUpdatedEventArgs(snapshot));
        }

        private InkStrokeModel BuildCommittedStroke(IReadOnlyList<InkInputPoint> points)
        {
            DrawingAttributes drawingAttributes = host?.CurrentDrawingAttributes?.Clone() ?? new DrawingAttributes();
            InkStrokeModel model = new()
            {
                Argb = ((uint)drawingAttributes.Color.A << 24)
                       | ((uint)drawingAttributes.Color.R << 16)
                       | ((uint)drawingAttributes.Color.G << 8)
                       | drawingAttributes.Color.B,
                Width = (float)drawingAttributes.Width,
                Height = (float)drawingAttributes.Height,
                StylusTip = (byte)drawingAttributes.StylusTip
            };

            foreach (InkInputPoint point in points)
            {
                model.Points.Add(new InkStrokePointModel(
                    point.X,
                    point.Y,
                    (ushort)Math.Round(Math.Clamp(point.PressureFactor, 0f, 1f) * 65535f, MidpointRounding.AwayFromZero),
                    0));
            }

            return model;
        }

        private static List<InkInputPoint> ToMutableList(IReadOnlyList<InkInputPoint> points)
        {
            List<InkInputPoint> copy = new(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                copy.Add(points[i]);
            }

            return copy;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }
    }
}
