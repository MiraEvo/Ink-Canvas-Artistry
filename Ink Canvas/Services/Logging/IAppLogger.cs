using System;

namespace Ink_Canvas.Services.Logging
{
    public interface IAppLogger
    {
        IAppLogger ForCategory(string category);

        void Trace(string message);

        void Info(string message);

        void Event(string message);

        void Error(string message, bool force = false);

        void Error(Exception exception, string? context = null, bool force = false);
    }
}
