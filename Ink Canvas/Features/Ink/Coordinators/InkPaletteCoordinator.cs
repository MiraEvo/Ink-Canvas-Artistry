using System;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal sealed class InkPaletteCoordinator
    {
        private readonly IInkPaletteHost host;

        public InkPaletteCoordinator(IInkPaletteHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public InkPaletteSessionState PaletteState { get; } = new();

        public void HandleInkWidthChanged(double value, bool fromBoardSlider)
        {
            if (!host.IsLoaded)
            {
                return;
            }

            host.ApplyInkWidthChange(value, fromBoardSlider);
        }

        public void HandleInkAlphaChanged(double value, bool fromBoardSlider)
        {
            if (!host.IsLoaded)
            {
                return;
            }

            host.ApplyInkAlphaChange(value, fromBoardSlider);
        }

        public void HandleColorSelected(int colorIndex)
        {
            PaletteState.InkColor = colorIndex;
            host.ApplyPaletteColorSelection(colorIndex);
        }

        public void HandlePaletteThemeRefresh(bool changeColorTheme = false)
        {
            host.ApplyPaletteTheme(changeColorTheme);
        }

        public void HandleBoardBackgroundToggle()
        {
            if (!host.IsLoaded)
            {
                return;
            }

            host.ToggleBoardBackgroundColor();
        }
    }
}

