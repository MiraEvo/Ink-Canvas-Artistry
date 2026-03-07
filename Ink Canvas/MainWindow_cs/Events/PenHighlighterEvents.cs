using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void InkAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            inkPaletteCoordinator?.HandleInkAlphaChanged(
                ((Slider)sender).Value,
                ReferenceEquals(sender, BoardInkAlphaSlider));
        }

        private void BtnHighlighterColorRed_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(101);
        }

        private void BtnHighlighterColorOrange_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(102);
        }

        private void BtnHighlighterColorYellow_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(103);
        }

        private void BtnHighlighterColorTeal_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(104);
        }
        private void BtnHighlighterColorBlue_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(105);
        }

        private void BtnHighlighterColorPurple_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(106);
        }
    }
}
