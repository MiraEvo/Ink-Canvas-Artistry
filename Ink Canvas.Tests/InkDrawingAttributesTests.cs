using Ink_Canvas.Features.Ink.Services;
using System.Windows.Ink;
using System.Windows.Media;
using Xunit;

namespace Ink_Canvas.Tests
{
    public sealed class InkDrawingAttributesTests
    {
        [Fact]
        public void CreateFreehandDrawingAttributes_EnablesFitToCurve()
        {
            DrawingAttributes source = new()
            {
                Color = Colors.DodgerBlue,
                Width = 4.5,
                Height = 2.5,
                FitToCurve = false
            };

            DrawingAttributes result = InkStrokeDrawingAttributesHelper.CreateFreehandDrawingAttributes(source);

            Assert.True(result.FitToCurve);
            Assert.Equal(source.Color, result.Color);
            Assert.Equal(source.Width, result.Width);
            Assert.Equal(source.Height, result.Height);
        }

        [Fact]
        public void CreateShapeDrawingAttributes_KeepsSharpCornersByDefault()
        {
            DrawingAttributes source = new()
            {
                Color = Colors.DarkRed,
                Width = 3.0,
                Height = 3.0,
                FitToCurve = true
            };

            DrawingAttributes result = InkStrokeDrawingAttributesHelper.CreateShapeDrawingAttributes(source);

            Assert.False(result.FitToCurve);
            Assert.Equal(source.Color, result.Color);
            Assert.Equal(source.Width, result.Width);
            Assert.Equal(source.Height, result.Height);
        }
    }
}
