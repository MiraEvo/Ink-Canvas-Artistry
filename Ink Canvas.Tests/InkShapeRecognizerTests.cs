using Ink_Canvas.Features.Ink.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Xunit;

namespace Ink_Canvas.Tests
{
    public sealed class InkShapeRecognizerTests
    {
        [Fact]
        public void Recognize_SingleStrokeLine()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateStrokeBetween(new Point(80, 120), new Point(320, 120))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Line, result!.Kind);
        }

        [Fact]
        public void Recognize_TwoStrokeLine()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateStrokeBetween(new Point(80, 120), new Point(200, 120)),
                CreateStrokeBetween(new Point(200, 120), new Point(340, 120))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Line, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokePolyline()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateOpenPolylineStroke(
                    new Point(80, 120),
                    new Point(200, 120),
                    new Point(200, 240))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Polyline, result!.Kind);
        }

        [Fact]
        public void Recognize_TwoStrokePolyline()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateStrokeBetween(new Point(80, 120), new Point(200, 120)),
                CreateStrokeBetween(new Point(200, 120), new Point(200, 260))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Polyline, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokeArc()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateArcStroke(new Point(240, 220), 120, 120, 0, 0.2, Math.PI - 0.2)
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Arc, result!.Kind);
        }

        [Fact]
        public void Recognize_NearClosedArc_AsArc()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateArcStroke(new Point(240, 220), 120, 120, 0, 0, Math.PI * 1.8)
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Arc, result!.Kind);
        }

        [Fact]
        public void Reject_TwoStrokeArc_AsArc()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateArcStroke(new Point(240, 220), 120, 120, 0, 0, Math.PI / 2),
                CreateArcStroke(new Point(240, 220), 120, 120, 0, Math.PI / 2, Math.PI)
            });

            Assert.True(result == null || result.Kind != RecognizedShapeKind.Arc);
        }

        [Fact]
        public void Reject_ShortShallowArc()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateArcStroke(new Point(160, 160), 40, 40, 0, 0, Math.PI / 8)
            });

            Assert.Null(result);
        }

        [Fact]
        public void Recognize_SingleStrokeCircle()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateEllipseStroke(new Point(220, 220), 100, 100, 0)
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Circle, result!.Kind);
        }

        [Fact]
        public void Recognize_RotatedSingleStrokeEllipse()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateEllipseStroke(new Point(260, 180), 120, 60, 32)
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Ellipse, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokeTriangle()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateClosedPolylineStroke(
                    new Point(120, 280),
                    new Point(220, 80),
                    new Point(320, 280))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Triangle, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokeRectangle()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateClosedPolylineStroke(
                    new Point(120, 120),
                    new Point(340, 120),
                    new Point(340, 280),
                    new Point(120, 280))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Rectangle, result!.Kind);
        }

        [Fact]
        public void Recognize_JitteredSingleStrokeRectangle()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateClosedPolylineStroke(
                    new Point(120, 122),
                    new Point(220, 116),
                    new Point(340, 124),
                    new Point(346, 200),
                    new Point(338, 282),
                    new Point(240, 287),
                    new Point(120, 278),
                    new Point(114, 200))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Rectangle, result!.Kind);
        }

        [Fact]
        public void Recognize_GapHealingRectangle()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateOpenPolylineStroke(
                    new Point(120, 120),
                    new Point(340, 120),
                    new Point(340, 280),
                    new Point(120, 280),
                    new Point(130, 140))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Rectangle, result!.Kind);
        }

        [Fact]
        public void Recognize_MultiStrokeRectangle_FromFourEdges()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateStrokeBetween(new Point(120, 120), new Point(340, 120)),
                CreateStrokeBetween(new Point(340, 120), new Point(340, 280)),
                CreateStrokeBetween(new Point(340, 280), new Point(120, 280)),
                CreateStrokeBetween(new Point(120, 280), new Point(120, 120))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Rectangle, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokeDiamond()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateClosedPolylineStroke(
                    new Point(220, 90),
                    new Point(390, 200),
                    new Point(220, 310),
                    new Point(50, 200))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Diamond, result!.Kind);
        }

        [Fact]
        public void Recognize_SingleStrokeParallelogram()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateClosedPolylineStroke(
                    new Point(120, 140),
                    new Point(320, 140),
                    new Point(380, 280),
                    new Point(180, 280))
            });

            Assert.NotNull(result);
            Assert.Equal(RecognizedShapeKind.Parallelogram, result!.Kind);
        }

        [Fact]
        public void Reject_OpenScribble()
        {
            RecognizedShapeResult? result = InkRecognizeHelper.RecognizeShape(new StrokeCollection
            {
                CreateOpenPolylineStroke(
                    new Point(120, 120),
                    new Point(150, 160),
                    new Point(170, 140),
                    new Point(190, 180),
                    new Point(210, 150))
            });

            Assert.Null(result);
        }

        private static Stroke CreateEllipseStroke(Point center, double majorRadius, double minorRadius, double rotationDegrees)
        {
            return CreateArcStroke(center, majorRadius, minorRadius, rotationDegrees, 0, 2 * Math.PI);
        }

        private static Stroke CreateArcStroke(Point center, double majorRadius, double minorRadius, double rotationDegrees, double startRadians, double endRadians)
        {
            double rotationRadians = rotationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(rotationRadians);
            double sin = Math.Sin(rotationRadians);
            List<Point> points = [];
            for (double angle = startRadians; angle <= endRadians + 0.001; angle += 0.04)
            {
                double x = majorRadius * Math.Cos(angle);
                double y = minorRadius * Math.Sin(angle);
                points.Add(new Point(center.X + x * cos - y * sin, center.Y + x * sin + y * cos));
            }

            return CreateStroke(points);
        }

        private static Stroke CreateClosedPolylineStroke(params Point[] vertices)
        {
            List<Point> points = InterpolatePolyline(vertices);
            points.AddRange(InterpolateBetween(vertices[^1], vertices[0]));
            return CreateStroke(points);
        }

        private static Stroke CreateOpenPolylineStroke(params Point[] vertices)
        {
            return CreateStroke(InterpolatePolyline(vertices));
        }

        private static Stroke CreateStrokeBetween(Point start, Point end)
        {
            return CreateStroke(InterpolateBetween(start, end));
        }

        private static Stroke CreateStroke(IEnumerable<Point> points)
        {
            return new Stroke(new StylusPointCollection(points.Select(static point => new StylusPoint(point.X, point.Y))));
        }

        private static List<Point> InterpolatePolyline(IReadOnlyList<Point> vertices)
        {
            List<Point> points = [];
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                points.AddRange(InterpolateBetween(vertices[i], vertices[i + 1]));
            }

            return points;
        }

        private static List<Point> InterpolateBetween(Point start, Point end)
        {
            double distance = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
            int segments = Math.Max(2, (int)Math.Ceiling(distance / 8.0));
            List<Point> points = [];
            for (int i = 0; i <= segments; i++)
            {
                double ratio = i / (double)segments;
                points.Add(new Point(
                    start.X + (end.X - start.X) * ratio,
                    start.Y + (end.Y - start.Y) * ratio));
            }

            return points;
        }
    }
}
