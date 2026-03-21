using Ink_Canvas.Features.Ink.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ink_Canvas.Features.Ink.Engine
{
    internal sealed class SkiaInkEngine : IInkEngine
    {
        private readonly LegacyInkAdapter fallback = new();
        private readonly Dictionary<int, SKPath> activePaths = [];
        private readonly SKPaint previewPaint = new()
        {
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Style = SKPaintStyle.Stroke
        };

        private bool disposed;

        public string EngineId => InkRuntimeDefaults.InkBackendSkiaV1;

        public event EventHandler<InkStrokeCommittedEventArgs>? StrokeCommitted
        {
            add => fallback.StrokeCommitted += value;
            remove => fallback.StrokeCommitted -= value;
        }

        public event EventHandler<InkPreviewUpdatedEventArgs>? PreviewUpdated
        {
            add => fallback.PreviewUpdated += value;
            remove => fallback.PreviewUpdated -= value;
        }

        public void Attach(IInkSurfaceHost host, InkEngineOptions options)
        {
            ThrowIfDisposed();
            fallback.Attach(host, options);
            activePaths.Clear();
        }

        public void Detach()
        {
            if (disposed)
            {
                return;
            }

            foreach (SKPath path in activePaths.Values)
            {
                path.Dispose();
            }

            activePaths.Clear();
            fallback.Detach();
        }

        public void ProcessInput(InkInputSample sample)
        {
            ThrowIfDisposed();
            TrackSkiaPath(sample);
            fallback.ProcessInput(sample);
        }

        public void FlushFrame(TimeSpan frameTime)
        {
            ThrowIfDisposed();
            fallback.FlushFrame(frameTime);
        }

        public InkHitTestResult HitTestErase(InkEraserPath path)
        {
            ThrowIfDisposed();
            return fallback.HitTestErase(path);
        }

        public InkSelectionResult HitTestSelection(InkSelectionPath path)
        {
            ThrowIfDisposed();
            return fallback.HitTestSelection(path);
        }

        public void ApplyTransform(InkTransformCommand command)
        {
            ThrowIfDisposed();
            fallback.ApplyTransform(command);
        }

        public InkDocumentSnapshot ExportSnapshot()
        {
            ThrowIfDisposed();
            return fallback.ExportSnapshot();
        }

        public void ImportSnapshot(InkDocumentSnapshot snapshot)
        {
            ThrowIfDisposed();
            fallback.ImportSnapshot(snapshot);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            previewPaint.Dispose();
            fallback.Dispose();
            foreach (SKPath path in activePaths.Values)
            {
                path.Dispose();
            }

            activePaths.Clear();
        }

        private void TrackSkiaPath(InkInputSample sample)
        {
            switch (sample.Phase)
            {
                case InkInputPhase.Begin:
                    ResetPath(sample.PointerId);
                    if (sample.Points.Count > 0)
                    {
                        SKPath path = activePaths[sample.PointerId];
                        InkInputPoint start = sample.Points[0];
                        path.MoveTo(start.X, start.Y);
                    }
                    break;
                case InkInputPhase.Move:
                    if (activePaths.TryGetValue(sample.PointerId, out SKPath? movePath))
                    {
                        foreach (InkInputPoint point in sample.Points)
                        {
                            movePath.LineTo(point.X, point.Y);
                        }
                    }
                    break;
                case InkInputPhase.End:
                case InkInputPhase.Cancel:
                    if (activePaths.TryGetValue(sample.PointerId, out SKPath? finishedPath))
                    {
                        finishedPath.Dispose();
                        activePaths.Remove(sample.PointerId);
                    }
                    break;
            }
        }

        private void ResetPath(int pointerId)
        {
            if (activePaths.TryGetValue(pointerId, out SKPath? previous))
            {
                previous.Dispose();
            }

            activePaths[pointerId] = new SKPath();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }
    }
}
