using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ink_Canvas.Services.ErrorHandling
{
    internal static class ExceptionBoundary
    {
        public static bool IsCritical(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (exception is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions.Any(IsCritical);
            }

            return exception is OutOfMemoryException
                or AccessViolationException
                or AppDomainUnloadedException
                or SEHException;
        }
    }
}
