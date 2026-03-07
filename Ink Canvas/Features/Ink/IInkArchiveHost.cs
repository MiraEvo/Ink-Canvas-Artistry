using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink
{
    internal interface IInkArchiveHost
    {
        Ink_Canvas.Settings Settings { get; }

        InkCanvas InkCanvas { get; }

        bool IsDesktopAnnotationMode { get; }

        bool IsBlackboardMode { get; }

        int CurrentWhiteboardIndex { get; }

        string? ShowOpenArchiveDialog();

        void ShowArchiveNotification(string message);

        void ReplaceCanvasContent(StrokeCollection strokes, IReadOnlyList<UIElement> elements);

        void ClearCanvasForArchiveImport();

        void EnsureCanvasVisibleAfterArchiveImport();
    }
}
