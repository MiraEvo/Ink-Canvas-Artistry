using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Ink_Canvas.Helpers
{
    internal static class PathSafetyHelper
    {
        private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        public static string NormalizeLeafName(string? value, string fallbackName)
        {
            string normalizedFallback = NormalizeLeafNameCore(fallbackName);
            if (string.IsNullOrWhiteSpace(normalizedFallback))
            {
                throw new ArgumentException("Fallback name must contain at least one valid file name character.", nameof(fallbackName));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return normalizedFallback;
            }

            string leafName = Path.GetFileName(value.Trim());
            string normalizedLeafName = NormalizeLeafNameCore(leafName);
            return string.IsNullOrWhiteSpace(normalizedLeafName) ? normalizedFallback : normalizedLeafName;
        }

        public static string ResolveRelativePath(string rootPath, params string[] relativeSegments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
            ArgumentNullException.ThrowIfNull(relativeSegments);

            string normalizedRoot = AppendDirectorySeparator(Path.GetFullPath(rootPath));
            string[] safeSegments = relativeSegments.Select(EnsureRelativeSegment).ToArray();
            string resolvedPath = Path.GetFullPath(Path.Join(normalizedRoot, Path.Join(safeSegments)));
            if (!resolvedPath.StartsWith(normalizedRoot, PathComparison))
            {
                throw new InvalidOperationException($"Resolved path '{resolvedPath}' escaped root '{normalizedRoot}'.");
            }

            return resolvedPath;
        }

        public static string GetRequiredDirectoryPath(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            string? directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new InvalidOperationException($"Path '{filePath}' does not contain a valid directory.");
            }

            return directoryPath;
        }

        public static string AppendDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string EnsureRelativeSegment(string segment)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(segment);

            if (Path.IsPathRooted(segment))
            {
                throw new InvalidOperationException($"Path segment '{segment}' must be relative.");
            }

            string trimmedSegment = segment.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (trimmedSegment.Length != segment.Length)
            {
                throw new InvalidOperationException($"Path segment '{segment}' cannot start with a directory separator.");
            }

            return segment;
        }

        private static string NormalizeLeafNameCore(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            StringBuilder builder = new(value.Length);
            foreach (char character in value)
            {
                builder.Append(IsUnsafeFileNameCharacter(character) ? '_' : character);
            }

            string normalized = builder.ToString().Trim().TrimEnd('.');
            return normalized is "." or ".." ? string.Empty : normalized;
        }

        private static bool IsUnsafeFileNameCharacter(char character)
        {
            return character == Path.DirectorySeparatorChar
                || character == Path.AltDirectorySeparatorChar
                || Path.GetInvalidFileNameChars().Contains(character);
        }
    }
}
