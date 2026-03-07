using System.Diagnostics;

namespace Ink_Canvas.Helpers
{
    internal static class ProcessHelper
    {
        public static void StartWithShell(string fileName, string arguments = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startInfo.Arguments = arguments;
            }

            Process.Start(startInfo);
        }
    }
}
