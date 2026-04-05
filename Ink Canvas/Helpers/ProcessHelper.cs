using System.Diagnostics;
using System;
using System.IO;

namespace Ink_Canvas.Helpers
{
    internal static class ProcessHelper
    {
        public static void StartWithShell(string fileName, string? arguments = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Process target cannot be empty.", nameof(fileName));
            }

            bool hasArguments = !string.IsNullOrWhiteSpace(arguments);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ResolveShellTarget(fileName),
                UseShellExecute = !hasArguments
            };

            if (hasArguments)
            {
                startInfo.Arguments = arguments;
            }

            Process.Start(startInfo);
        }

        private static string ResolveShellTarget(string target)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out Uri uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    return uri.AbsoluteUri;
                }

                if (uri.IsFile)
                {
                    return EnsureExistingLocalPath(uri.LocalPath);
                }

                throw new InvalidOperationException($"Unsupported URI scheme '{uri.Scheme}'.");
            }

            if (!Path.IsPathRooted(target))
            {
                throw new InvalidOperationException("Only absolute local paths or http/https URLs are allowed.");
            }

            return EnsureExistingLocalPath(target);
        }

        private static string EnsureExistingLocalPath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                return fullPath;
            }

            throw new FileNotFoundException("The requested process target does not exist.", fullPath);
        }
    }
}
