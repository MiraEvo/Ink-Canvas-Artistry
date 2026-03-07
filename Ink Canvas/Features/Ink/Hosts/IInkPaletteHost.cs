namespace Ink_Canvas.Features.Ink.Hosts
{
    internal interface IInkPaletteHost
    {
        bool IsLoaded { get; }

        void ApplyInkWidthChange(double value, bool fromBoardSlider);

        void ApplyInkAlphaChange(double value, bool fromBoardSlider);

        void ApplyPaletteColorSelection(int colorIndex);

        void ApplyPaletteTheme(bool changeColorTheme = false);

        void ToggleBoardBackgroundColor();
    }
}

