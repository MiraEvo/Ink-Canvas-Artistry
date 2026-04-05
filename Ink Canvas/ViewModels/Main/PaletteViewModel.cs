using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels.Main
{
    public sealed class PaletteViewModel : ObservableObject
    {
        private int inkColor = 1;

        public int InkColor
        {
            get => inkColor;
            set => SetProperty(ref inkColor, value);
        }
    }
}
