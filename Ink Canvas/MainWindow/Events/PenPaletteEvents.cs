using Ink_Canvas.Helpers;
using System;
using Ink_Canvas.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            inkPaletteCoordinator?.HandleInkWidthChanged(
                ((Slider)sender).Value,
                ReferenceEquals(sender, BoardInkWidthSlider));
        }

        private void ColorSwitchCheck()
        {
            forceEraser = false;
            ShellViewModel.SetToolMode(ToolMode.Pen, false);
            HideSubPanels("color");
            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (ShellViewModel.IsBlackboardMode)
                {
                    ExitBlackboardSession();
                }
                else
                {
                    BtnHideInkCanvas_Click(null, null);
                }
            }

            StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count != 0)
            {
                foreach (Stroke stroke in strokes)
                {
                    stroke.DrawingAttributes.Color = inkCanvas.DefaultDrawingAttributes.Color;
                }
            }
            if (DrawingAttributesHistory.Count > 0)
            {
                CommitDrawingAttributesHistoryIfNeeded();
            }
            else
            {
                inkCanvas.IsManipulationEnabled = true;
                drawingShapeMode = 0;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                CancelSingleFingerDragMode();
                forceEraser = false;
                CheckColorTheme();
            }

            isLongPressSelected = false;
        }

        private void CheckColorTheme(bool changeColorTheme = false)
        {
            if (changeColorTheme && ShellViewModel.IsBlackboardMode)
            {
                if (Settings.Canvas.UsingWhiteboard)
                {
                    GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
                    isUselightThemeColor = false;
                }
                else
                {
                    GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FF1F1F1F"));
                    isUselightThemeColor = true;
                }
            }

            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                isUselightThemeColor = isDesktopUselightThemeColor;
                inkColor = lastDesktopInkColor;
            }
            else
            {
                inkColor = lastBoardInkColor;
            }

            if (inkColor == 101)
            { // Highlighter Red
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(220, 38, 38);
            }
            else if (inkColor == 102)
            { // Highlighter Orange
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(234, 88, 12);
            }
            else if (inkColor == 103)
            { // Highlighter Yellow
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(234, 179, 8);
            }
            else if (inkColor == 104)
            { // Highlighter Teal
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(13, 148, 136);
            }
            else if (inkColor == 105)
            { // Highlighter Blue
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(37, 99, 235);
            }
            else if (inkColor == 106)
            { // Highlighter Purple
                inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(147, 51, 234);
            }
            else if (isUselightThemeColor)
            {
                inkCanvas.DefaultDrawingAttributes.Color = inkColorLightThemeMapping[inkColor];
            }
            else
            {
                inkCanvas.DefaultDrawingAttributes.Color = inkColorDarkThemeMapping[inkColor];
            }
            if (isUselightThemeColor)
            { // 亮系
                // Red
                BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                // Green
                BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                // Blue
                BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                // Yellow
                BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                // Pink ( Purple )
                BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                // Teal
                BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                // Orange
                BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));

                ColorThemeSwitchIcon.Glyph = "\uE708";
                BoardColorThemeSwitchIcon.Glyph = "\uE708";
                ColorThemeSwitchTextBlock.Text = "暗系";
            }
            else
            { // 暗系
                // Red
                BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                // Green
                BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                // Blue
                BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                // Yellow
                BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                // Pink ( Purple )
                BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                // Teal
                BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                // Orange
                BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));
                BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));


                ColorThemeSwitchIcon.Glyph = "\uE706";
                BoardColorThemeSwitchIcon.Glyph = "\uE706";
                ColorThemeSwitchTextBlock.Text = "亮系";
            }
            if (drawingAttributes != null && isLoaded)
            {
                if (inkColor > 100)
                {
                    drawingAttributes.Height = 30 + Settings.Canvas.InkWidth;
                    drawingAttributes.Width = 30 + Settings.Canvas.InkWidth;
                    byte NowR = drawingAttributes.Color.R;
                    byte NowG = drawingAttributes.Color.G;
                    byte NowB = drawingAttributes.Color.B;
                    drawingAttributes.Color = Color.FromArgb((byte)Settings.Canvas.InkAlpha, NowR, NowG, NowB);
                    drawingAttributes.IsHighlighter = true;
                }
                else
                {
                    drawingAttributes.Height = Settings.Canvas.InkWidth;
                    drawingAttributes.Width = Settings.Canvas.InkWidth;
                    drawingAttributes.IsHighlighter = false;
                }
            }

        }

        private void CheckLastColor(int colorIndex)
        {
            inkColor = colorIndex;
            StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count > 0)
            {
                Color targetedColor = inkColorLightThemeMapping[colorIndex];
                if (!isUselightThemeColor)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = inkColorDarkThemeMapping[colorIndex];
                }
                foreach (Stroke stroke in strokes)
                {
                    stroke.DrawingAttributes.Color = targetedColor;
                }
            }
            else
            {
                if (ShellViewModel.IsDesktopAnnotationMode)
                {
                    lastDesktopInkColor = colorIndex;
                }
                else
                {
                    lastBoardInkColor = colorIndex;
                }
                ColorSwitchCheck();
            }
        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(0);
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(1);
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(2);
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(3);
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(4);
        }

        private void BtnColorWhite_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(5);
        }

        private void BtnColorPink_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(6);
        }

        private void BtnColorTeal_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(7);
        }

        private void BtnColorOrange_Click(object sender, RoutedEventArgs e)
        {
            inkPaletteCoordinator?.HandleColorSelected(8);
        }

        private Color StringToColor(string colorStr)
        {
            Byte[] argb = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                char[] charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                Byte b1 = toByte(charArray[0]);
                Byte b2 = toByte(charArray[1]);
                argb[i] = (Byte)(b2 | (b1 << 4));
            }
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);//#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }
    }
}
