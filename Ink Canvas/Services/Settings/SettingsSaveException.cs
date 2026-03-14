using System;

namespace Ink_Canvas.Services.Settings
{
    public sealed class SettingsSaveException : Exception
    {
        public SettingsSaveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
