using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Ink_Canvas.Services.ErrorHandling
{
    public sealed class TaskGuard
    {
        private readonly AppErrorHandler errorHandler;

        public TaskGuard(AppErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public void Forget(Task task, AppErrorContext context)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(context);

            if (task.IsCompleted)
            {
                ObserveCompletedTask(task, context);
                return;
            }

            _ = ObserveAsync(task, context);
        }

        private void ObserveCompletedTask(Task task, AppErrorContext context)
        {
            if (task.IsFaulted && task.Exception != null)
            {
                errorHandler.Handle(task.Exception.GetBaseException(), context);
            }
        }

        [SuppressMessage("Reliability", "cs/catch-of-all-exceptions", Justification = "CodeQL-AUDITED-ERROR-BOUNDARY: background task observation must forward task failures into AppErrorHandler.")]
        private async Task ObserveAsync(Task task, AppErrorContext context)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionBoundary.IsCritical(ex))
            {
                errorHandler.Handle(ex, context);
            }
        }
    }
}
