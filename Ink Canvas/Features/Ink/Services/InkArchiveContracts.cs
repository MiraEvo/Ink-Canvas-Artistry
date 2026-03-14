using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Services
{
    internal enum InkArchiveSnapshotMode
    {
        StrokesOnly,
        FullCanvas
    }

    internal sealed record InkArchiveManifestEntries(
        string Strokes,
        string? Elements,
        string? DependenciesPrefix);

    internal sealed record InkArchiveManifest(
        string Format,
        int Version,
        string Mode,
        string CreatedAtUtc,
        InkArchiveManifestEntries Entries);

    internal sealed record InkArchiveSaveResult(
        InkArchiveSnapshotMode Mode,
        int SerializedElementCount,
        IReadOnlyList<string> DependencyFilePaths);

    internal sealed record InkArchiveSnapshot(
        InkArchiveSnapshotMode Mode,
        StrokeCollection Strokes,
        IReadOnlyList<UIElement> Elements,
        IReadOnlyList<string> DependencyFilePaths);

    internal sealed record InkArchiveLoadResult(
        StrokeCollection Strokes,
        IReadOnlyList<UIElement> Elements,
        int SkippedElementCount,
        string? WarningMessage)
    {
        public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningMessage) || SkippedElementCount > 0;
    }

    internal sealed record InkCanvasElementsSaveResult(
        int SerializedElementCount,
        IReadOnlyList<string> DependencyFilePaths);

    internal sealed record InkCanvasElementsLoadResult(
        IReadOnlyList<UIElement> Elements,
        int SkippedElementCount,
        string? WarningMessage);
}
