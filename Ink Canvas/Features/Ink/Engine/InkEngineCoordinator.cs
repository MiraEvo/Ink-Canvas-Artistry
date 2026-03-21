using Ink_Canvas.Features.Ink.Services;
using Ink_Canvas.Services.Logging;
using System;

namespace Ink_Canvas.Features.Ink.Engine
{
    internal readonly record struct InkRuntimeRoutingChange(
        bool BackendChanged,
        bool RecognizerChanged,
        bool ArchiveWriteFormatChanged,
        InkRuntimeRouting CurrentRouting);

    internal sealed class InkEngineCoordinator : IDisposable
    {
        private readonly IAppLogger logger;
        private IInkEngine? engine;
        private IInkSurfaceHost? host;
        private InkEngineOptions options = new(EnablePressure: true, EnableRealtimePreview: true);
        private bool disposed;

        public InkEngineCoordinator(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkEngineCoordinator));
            CurrentRouting = new InkRuntimeRouting(
                InkBackendKind.SkiaV1,
                InkRecognizerKind.V2,
                InkArchiveWriteFormatKind.V4,
                false);
        }

        public InkRuntimeRouting CurrentRouting { get; private set; }

        public IInkEngine? CurrentEngine => engine;

        public void AttachHost(IInkSurfaceHost host, InkEngineOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(host);
            ThrowIfDisposed();
            this.host = host;
            if (options.HasValue)
            {
                this.options = options.Value;
            }

            EnsureEngineAttached();
        }

        public InkRuntimeRoutingChange ApplySettings(global::Ink_Canvas.Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ThrowIfDisposed();

            InkRuntimeRouting previous = CurrentRouting;
            InkRuntimeRouting current = InkRuntimeSettingsResolver.Resolve(settings);
            CurrentRouting = current;

            bool backendChanged = previous.Backend != current.Backend;
            bool recognizerChanged = previous.Recognizer != current.Recognizer;
            bool archiveWriteFormatChanged = previous.ArchiveWriteFormat != current.ArchiveWriteFormat;

            if (backendChanged)
            {
                logger.Event($"Ink Engine | Switching backend from {previous.Backend} to {current.Backend}");
                SwitchEngine(current.Backend);
            }

            return new InkRuntimeRoutingChange(backendChanged, recognizerChanged, archiveWriteFormatChanged, current);
        }

        public void ProcessInput(InkInputSample sample)
        {
            ThrowIfDisposed();
            engine?.ProcessInput(sample);
        }

        public InkDocumentSnapshot ExportSnapshot()
        {
            ThrowIfDisposed();
            return engine?.ExportSnapshot() ?? new InkDocumentSnapshot(new InkDocumentModel());
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            engine?.Dispose();
            engine = null;
            host = null;
        }

        private void EnsureEngineAttached()
        {
            if (engine != null || host == null)
            {
                return;
            }

            engine = CreateEngine(CurrentRouting.Backend);
            engine.Attach(host, options);
        }

        private void SwitchEngine(InkBackendKind backendKind)
        {
            if (host == null)
            {
                engine = CreateEngine(backendKind);
                return;
            }

            IInkEngine? previous = engine;
            engine = CreateEngine(backendKind);
            engine.Attach(host, options);
            previous?.Detach();
            previous?.Dispose();
        }

        private static IInkEngine CreateEngine(InkBackendKind backendKind)
        {
            return backendKind == InkBackendKind.Legacy
                ? new LegacyInkAdapter()
                : new SkiaInkEngine();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }
    }
}
