using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.Services.ErrorHandling
{
    public sealed class UiDispatchGuard
    {
        private readonly AppErrorHandler errorHandler;

        public UiDispatchGuard(AppErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        [SuppressMessage("Reliability", "cs/catch-of-all-exceptions", Justification = "CodeQL-AUDITED-ERROR-BOUNDARY: dispatcher invocation must report UI-boundary failures without crashing the caller flow.")]
        public bool TryInvoke(Action action, AppErrorContext context)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);

            if (Application.Current?.Dispatcher is not { } dispatcher)
            {
                action();
                return true;
            }

            if (dispatcher.CheckAccess())
            {
                action();
                return true;
            }

            try
            {
                dispatcher.Invoke(action);
                return true;
            }
            catch (TaskCanceledException ex)
            {
                errorHandler.Handle(ex, context);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                errorHandler.Handle(ex, context);
                return false;
            }
            catch (Exception ex) when (!ExceptionBoundary.IsCritical(ex))
            {
                errorHandler.Handle(ex, context);
                return false;
            }
        }

        [SuppressMessage("Reliability", "cs/catch-of-all-exceptions", Justification = "CodeQL-AUDITED-ERROR-BOUNDARY: dispatcher invocation must report UI-boundary failures without crashing the caller flow.")]
        public T Invoke<T>(Func<T> action, T fallback, AppErrorContext context)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);

            if (Application.Current?.Dispatcher is not { } dispatcher)
            {
                return action();
            }

            if (dispatcher.CheckAccess())
            {
                return action();
            }

            try
            {
                return dispatcher.Invoke(action);
            }
            catch (TaskCanceledException ex)
            {
                errorHandler.Handle(ex, context);
                return fallback;
            }
            catch (InvalidOperationException ex)
            {
                errorHandler.Handle(ex, context);
                return fallback;
            }
            catch (Exception ex) when (!ExceptionBoundary.IsCritical(ex))
            {
                errorHandler.Handle(ex, context);
                return fallback;
            }
        }
    }
}
