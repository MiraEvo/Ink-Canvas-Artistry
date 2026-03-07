using System.Collections.Generic;
using System.Windows.Media;

namespace Ink_Canvas.Features.Ink.State
{
    internal sealed class InkPaletteSessionState
    {
        public int InkColor { get; set; } = 1;

        public bool IsUsingLightThemeColor { get; set; }

        public bool IsDesktopUsingLightThemeColor { get; set; }

        public int LastDesktopInkColor { get; set; } = 1;

        public int LastBoardInkColor { get; set; } = 5;

        public Dictionary<int, Color> LightThemeMapping { get; } = new()
        {
            { 0, Colors.Black },
            { 1, Color.FromRgb(239, 68, 68) },
            { 2, Color.FromRgb(34, 197, 94) },
            { 3, Color.FromRgb(59, 130, 246) },
            { 4, Color.FromRgb(250, 204, 21) },
            { 5, Colors.White },
            { 6, Color.FromRgb(236, 72, 153) },
            { 7, Color.FromRgb(20, 184, 166) },
            { 8, Color.FromRgb(249, 115, 22) },
        };

        public Dictionary<int, Color> DarkThemeMapping { get; } = new()
        {
            { 0, Colors.Black },
            { 1, Color.FromRgb(220, 38, 38) },
            { 2, Color.FromRgb(22, 163, 74) },
            { 3, Color.FromRgb(37, 99, 235) },
            { 4, Color.FromRgb(234, 179, 8) },
            { 5, Colors.White },
            { 6, Color.FromRgb(147, 51, 234) },
            { 7, Color.FromRgb(13, 148, 136) },
            { 8, Color.FromRgb(234, 88, 12) },
        };
    }
}

