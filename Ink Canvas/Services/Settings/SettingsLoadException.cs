using System;

namespace Ink_Canvas.Services.Settings
{
    public sealed class SettingsLoadException : Exception
    {
        public SettingsLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
