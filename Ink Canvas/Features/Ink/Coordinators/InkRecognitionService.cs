using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas.Features.Ink.Coordinators
{
    internal sealed class InkRecognitionService
    {
        private const float DefaultPressureFactor = 0.5f;
        private const double DoubleComparisonTolerance = 0.000001;
        private const float PressureComparisonTolerance = 0.001f;
        private readonly IAppLogger logger;

        public InkRecognitionService(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkRecognitionService));
        }

        public void HandleStrokeCollected(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                InkCanvas inkCanvas = inkCanvasHost.InkCanvas;
                inkCanvas.Opacity = 1;

                if (inkCanvasHost.Settings.InkToShape.IsInkToShapeEnabled && !Environment.Is64BitProcess)
                {
                    ProcessInkToShape(inkCanvasHost, inkHistoryHost, shapeDrawingState, e);
                }

                ApplyPressureStyle(inkCanvasHost, shapeDrawingState, e);
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "InkToShape | Failed to process collected stroke");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "InkToShape | Failed to process collected stroke");
            }
        }

        private void ProcessInkToShape(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                InkCanvas inkCanvas = inkCanvasHost.InkCanvas;
                shapeDrawingState.NewStrokes.Add(e.Stroke);
                if (shapeDrawingState.NewStrokes.Count > 4)
                {
                    shapeDrawingState.NewStrokes.RemoveAt(0);
                }

                for (int i = 0; i < shapeDrawingState.NewStrokes.Count; i++)
                {
                    if (!inkCanvas.Strokes.Contains(shapeDrawingState.NewStrokes[i]))
                    {
                        shapeDrawingState.NewStrokes.RemoveAt(i--);
                    }
                }

                for (int i = 0; i < shapeDrawingState.Circles.Count; i++)
                {
                    if (!inkCanvas.Strokes.Contains(shapeDrawingState.Circles[i].Stroke))
                    {
                        shapeDrawingState.Circles.RemoveAt(i);
                    }
                }

                StrokeCollection strokeReco = new();
                dynamic result = InkRecognizeHelper.RecognizeShape(shapeDrawingState.NewStrokes);
                for (int i = shapeDrawingState.NewStrokes.Count - 1; i >= 0; i--)
                {
                    strokeReco.Add(shapeDrawingState.NewStrokes[i]);
                    dynamic newResult = InkRecognizeHelper.RecognizeShape(strokeReco);
                    string shapeName = newResult.InkDrawingNode.GetShapeName();
                    if (shapeName == "Circle" || shapeName == "Ellipse")
                    {
                        result = newResult;
                        break;
                    }
                }

                string recognizedShapeName = result.InkDrawingNode.GetShapeName();
                if (recognizedShapeName == "Circle")
                {
                    HandleCircleRecognition(inkCanvasHost, inkHistoryHost, shapeDrawingState, result);
                }
                else if (recognizedShapeName.Contains("Ellipse", StringComparison.Ordinal))
                {
                    HandleEllipseRecognition(inkCanvasHost, inkHistoryHost, shapeDrawingState, result);
                }
                else if (recognizedShapeName.Contains("Triangle", StringComparison.Ordinal))
                {
                    HandleTriangleRecognition(inkCanvasHost, inkHistoryHost, shapeDrawingState, result);
                }
                else if (recognizedShapeName.Contains("Rectangle", StringComparison.Ordinal)
                    || recognizedShapeName.Contains("Diamond", StringComparison.Ordinal)
                    || recognizedShapeName.Contains("Parallelogram", StringComparison.Ordinal)
                    || recognizedShapeName.Contains("Square", StringComparison.Ordinal))
                {
                    HandleRectangleRecognition(inkCanvasHost, inkHistoryHost, shapeDrawingState, result);
                }
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "InkToShape | Failed to recognize collected shape");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply recognized shape");
            }
        }

        private void HandleCircleRecognition(IInkCanvasHost inkCanvasHost, IInkHistoryHost inkHistoryHost, ShapeDrawingSessionState shapeDrawingState, dynamic result)
        {
            InkCanvas inkCanvas = inkCanvasHost.InkCanvas;
            dynamic shape = result.InkDrawingNode.GetShape();
            if (shape.Width <= 75)
            {
                return;
            }

            foreach (Circle circle in shapeDrawingState.Circles)
            {
                if (Math.Abs(result.Centroid.X - circle.Centroid.X) / shape.Width < 0.12
                    && Math.Abs(result.Centroid.Y - circle.Centroid.Y) / shape.Width < 0.12)
                {
                    result.Centroid = circle.Centroid;
                    break;
                }

                double distance = GetDistance(result.Centroid, circle.Centroid);
                double x = shape.Width / 2.0 + circle.R - distance;
                if (Math.Abs(x) / shape.Width < 0.1)
                {
                    double sinTheta = (result.Centroid.Y - circle.Centroid.Y) / distance;
                    double cosTheta = (result.Centroid.X - circle.Centroid.X) / distance;
                    result.Centroid = new Point(result.Centroid.X + x * cosTheta, result.Centroid.Y + x * sinTheta);
                }

                x = Math.Abs(circle.R - shape.Width / 2.0) - distance;
                if (Math.Abs(x) / shape.Width < 0.1)
                {
                    double sinTheta = (result.Centroid.Y - circle.Centroid.Y) / distance;
                    double cosTheta = (result.Centroid.X - circle.Centroid.X) / distance;
                    result.Centroid = new Point(result.Centroid.X + x * cosTheta, result.Centroid.Y + x * sinTheta);
                }
            }

            Point initialPoint = new(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
            Point endPoint = new(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);
            Stroke stroke = CreateStrokeFromPoints(inkCanvas, GenerateEllipseGeometry(initialPoint, endPoint));

            shapeDrawingState.Circles.Add(new Circle(result.Centroid, shape.Width / 2.0, stroke));
            ApplyRecognitionResult(inkCanvasHost, inkHistoryHost, shapeDrawingState, result.InkDrawingNode.Strokes, new StrokeCollection { stroke }, false);
        }

        private void HandleEllipseRecognition(IInkCanvasHost inkCanvasHost, IInkHistoryHost inkHistoryHost, ShapeDrawingSessionState shapeDrawingState, dynamic result)
        {
            InkCanvas inkCanvas = inkCanvasHost.InkCanvas;
            dynamic shape = result.InkDrawingNode.GetShape();
            dynamic hotPoints = result.InkDrawingNode.HotPoints;
            double a = GetDistance(ToPoint(hotPoints[0]), ToPoint(hotPoints[2])) / 2;
            double b = GetDistance(ToPoint(hotPoints[1]), ToPoint(hotPoints[3])) / 2;
            if (a < b)
            {
                (a, b) = (b, a);
            }

            result.Centroid = new Point((hotPoints[0].X + hotPoints[2].X) / 2, (hotPoints[0].Y + hotPoints[2].Y) / 2);
            bool needRotation = true;

            if (!(shape.Width > 75 || shape.Height > 75 && hotPoints.Count == 4))
            {
                return;
            }

            Point initialPoint = new(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
            Point endPoint = new(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);

            foreach (Circle circle in shapeDrawingState.Circles)
            {
                if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2
                    && Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                {
                    result.Centroid = circle.Centroid;
                    initialPoint = new Point(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
                    endPoint = new Point(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);

                    if (Math.Abs(a - circle.R) / a < 0.2)
                    {
                        if (shape.Width >= shape.Height)
                        {
                            initialPoint.X = result.Centroid.X - circle.R;
                            endPoint.X = result.Centroid.X + circle.R;
                            initialPoint.Y = result.Centroid.Y - b;
                            endPoint.Y = result.Centroid.Y + b;
                        }
                        else
                        {
                            initialPoint.Y = result.Centroid.Y - circle.R;
                            endPoint.Y = result.Centroid.Y + circle.R;
                            initialPoint.X = result.Centroid.X - a;
                            endPoint.X = result.Centroid.X + a;
                        }
                    }

                    break;
                }

                if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2)
                {
                    double sinTheta = Math.Abs(circle.Centroid.Y - result.Centroid.Y) / circle.R;
                    double cosTheta = Math.Sqrt(1 - sinTheta * sinTheta);
                    double newA = circle.R * cosTheta;
                    if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 && Math.Abs(newA - a) / newA < 0.3)
                    {
                        initialPoint.X = circle.Centroid.X - newA;
                        endPoint.X = circle.Centroid.X + newA;
                        initialPoint.Y = result.Centroid.Y - newA / 5;
                        endPoint.Y = result.Centroid.Y + newA / 5;

                        inkHistoryHost.BackupCurrentStrokes();
                        inkHistoryHost.SetCommitReason(CommitReason.ShapeRecognition);
                        inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                        shapeDrawingState.NewStrokes = new StrokeCollection();

                        Stroke stroke = CreateStrokeFromPoints(inkCanvas, GenerateEllipseGeometry(initialPoint, endPoint, false, true));
                        StrokeCollection dashedStroke = GenerateDashedLineEllipseStrokeCollection(inkCanvas, initialPoint, endPoint, true, false);
                        StrokeCollection strokes = new() { stroke };
                        strokes.Add(dashedStroke);
                        inkCanvas.Strokes.Add(strokes);
                        inkHistoryHost.SetCommitReason(CommitReason.UserInput);
                        return;
                    }
                }
                else if (Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                {
                    double cosTheta = Math.Abs(circle.Centroid.X - result.Centroid.X) / circle.R;
                    double sinTheta = Math.Sqrt(1 - cosTheta * cosTheta);
                    double newA = circle.R * sinTheta;
                    if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 && Math.Abs(newA - a) / newA < 0.3)
                    {
                        initialPoint.X = result.Centroid.X - newA / 5;
                        endPoint.X = result.Centroid.X + newA / 5;
                        initialPoint.Y = circle.Centroid.Y - newA;
                        endPoint.Y = circle.Centroid.Y + newA;
                        needRotation = false;
                    }
                }
            }

            Point[] correctedPoints = FixPointsDirection(ToPoint(hotPoints[0]), ToPoint(hotPoints[2]));
            hotPoints[0] = correctedPoints[0];
            hotPoints[2] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[1]), ToPoint(hotPoints[3]));
            hotPoints[1] = correctedPoints[0];
            hotPoints[3] = correctedPoints[1];

            Stroke ellipseStroke = CreateStrokeFromPoints(inkCanvas, GenerateEllipseGeometry(initialPoint, endPoint));
            if (needRotation)
            {
                Matrix matrix = new();
                double tanTheta = (hotPoints[2].Y - hotPoints[0].Y) / (hotPoints[2].X - hotPoints[0].X);
                double theta = Math.Atan(tanTheta);
                matrix.RotateAt(theta * 180.0 / Math.PI, result.Centroid.X, result.Centroid.Y);
                ellipseStroke.Transform(matrix, false);
            }

            ApplyRecognitionResult(inkCanvasHost, inkHistoryHost, shapeDrawingState, result.InkDrawingNode.Strokes, new StrokeCollection { ellipseStroke }, true);
        }

        private void HandleTriangleRecognition(IInkCanvasHost inkCanvasHost, IInkHistoryHost inkHistoryHost, ShapeDrawingSessionState shapeDrawingState, dynamic result)
        {
            dynamic hotPoints = result.InkDrawingNode.HotPoints;
            if (!((Math.Max(Math.Max(hotPoints[0].X, hotPoints[1].X), hotPoints[2].X) - Math.Min(Math.Min(hotPoints[0].X, hotPoints[1].X), hotPoints[2].X) >= 100
                || Math.Max(Math.Max(hotPoints[0].Y, hotPoints[1].Y), hotPoints[2].Y) - Math.Min(Math.Min(hotPoints[0].Y, hotPoints[1].Y), hotPoints[2].Y) >= 100)
                && hotPoints.Count == 3))
            {
                return;
            }

            Point[] correctedPoints = FixPointsDirection(ToPoint(hotPoints[0]), ToPoint(hotPoints[1]));
            hotPoints[0] = correctedPoints[0];
            hotPoints[1] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[0]), ToPoint(hotPoints[2]));
            hotPoints[0] = correctedPoints[0];
            hotPoints[2] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[1]), ToPoint(hotPoints[2]));
            hotPoints[1] = correctedPoints[0];
            hotPoints[2] = correctedPoints[1];

            StylusPointCollection points =
            [
                new StylusPoint(hotPoints[0].X, hotPoints[0].Y),
                new StylusPoint(hotPoints[1].X, hotPoints[1].Y),
                new StylusPoint(hotPoints[2].X, hotPoints[2].Y)
            ];
            Stroke stroke = new(GenerateFakePressureTriangle(points))
            {
                DrawingAttributes = inkCanvasHost.InkCanvas.DefaultDrawingAttributes.Clone()
            };

            ApplyRecognitionResult(inkCanvasHost, inkHistoryHost, shapeDrawingState, result.InkDrawingNode.Strokes, new StrokeCollection { stroke }, true);
        }

        private void HandleRectangleRecognition(IInkCanvasHost inkCanvasHost, IInkHistoryHost inkHistoryHost, ShapeDrawingSessionState shapeDrawingState, dynamic result)
        {
            dynamic hotPoints = result.InkDrawingNode.HotPoints;
            if (!((Math.Max(Math.Max(Math.Max(hotPoints[0].X, hotPoints[1].X), hotPoints[2].X), hotPoints[3].X)
                - Math.Min(Math.Min(Math.Min(hotPoints[0].X, hotPoints[1].X), hotPoints[2].X), hotPoints[3].X) >= 100
                || Math.Max(Math.Max(Math.Max(hotPoints[0].Y, hotPoints[1].Y), hotPoints[2].Y), hotPoints[3].Y)
                - Math.Min(Math.Min(Math.Min(hotPoints[0].Y, hotPoints[1].Y), hotPoints[2].Y), hotPoints[3].Y) >= 100)
                && hotPoints.Count == 4))
            {
                return;
            }

            Point[] correctedPoints = FixPointsDirection(ToPoint(hotPoints[0]), ToPoint(hotPoints[1]));
            hotPoints[0] = correctedPoints[0];
            hotPoints[1] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[1]), ToPoint(hotPoints[2]));
            hotPoints[1] = correctedPoints[0];
            hotPoints[2] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[2]), ToPoint(hotPoints[3]));
            hotPoints[2] = correctedPoints[0];
            hotPoints[3] = correctedPoints[1];
            correctedPoints = FixPointsDirection(ToPoint(hotPoints[3]), ToPoint(hotPoints[0]));
            hotPoints[3] = correctedPoints[0];
            hotPoints[0] = correctedPoints[1];

            List<Point> pointList = [ToPoint(hotPoints[0]), ToPoint(hotPoints[1]), ToPoint(hotPoints[2]), ToPoint(hotPoints[3]), ToPoint(hotPoints[0])];
            Stroke stroke = new(GenerateFakePressureRectangle(new StylusPointCollection(pointList)))
            {
                DrawingAttributes = inkCanvasHost.InkCanvas.DefaultDrawingAttributes.Clone()
            };

            ApplyRecognitionResult(inkCanvasHost, inkHistoryHost, shapeDrawingState, result.InkDrawingNode.Strokes, new StrokeCollection { stroke }, true);
        }

        private void ApplyRecognitionResult(
            IInkCanvasHost inkCanvasHost,
            IInkHistoryHost inkHistoryHost,
            ShapeDrawingSessionState shapeDrawingState,
            StrokeCollection replacedStrokes,
            StrokeCollection addedStrokes,
            bool hideSelectionCover)
        {
            InkCanvas inkCanvas = inkCanvasHost.InkCanvas;
            inkHistoryHost.BackupCurrentStrokes();
            inkHistoryHost.SetCommitReason(CommitReason.ShapeRecognition);
            inkCanvas.Strokes.Remove(replacedStrokes);
            inkCanvas.Strokes.Add(addedStrokes);
            inkHistoryHost.SetCommitReason(CommitReason.UserInput);
            if (hideSelectionCover)
            {
                inkCanvasHost.HideSelectionCover();
            }

            shapeDrawingState.NewStrokes = new StrokeCollection();
        }

        private void ApplyPressureStyle(IInkCanvasHost inkCanvasHost, ShapeDrawingSessionState shapeDrawingState, InkCanvasStrokeCollectedEventArgs e)
        {
            foreach (StylusPoint stylusPoint in e.Stroke.StylusPoints)
            {
                bool hasCustomPressure =
                    !IsNearlyEqual(stylusPoint.PressureFactor, DefaultPressureFactor)
                    && !IsNearlyZero(stylusPoint.PressureFactor);

                if (hasCustomPressure || inkCanvasHost.InkColor > 100)
                {
                    return;
                }
            }

            if (e.Stroke.StylusPoints.Count > 3)
            {
                Random random = new();
                double speed = GetPointSpeed(
                    e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                    e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                    e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint());

                        RandWindow.RandomSeed = (int)(speed * 100000 * 1000);
            }

            switch (inkCanvasHost.Settings.Canvas.InkStyle)
            {
                case 1:
                    ApplyInkStyleOne(e);
                    break;
                case 0:
                    ApplyInkStyleZero(e);
                    break;
                case 3:
                    ApplyInkStyleThree(shapeDrawingState, e);
                    break;
            }
        }

        private void ApplyInkStyleOne(InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                StylusPointCollection stylusPoints = new();
                int count = e.Stroke.StylusPoints.Count - 1;

                for (int i = 0; i <= count; i++)
                {
                    double speed = GetPointSpeed(
                        e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(),
                        e.Stroke.StylusPoints[i].ToPoint(),
                        e.Stroke.StylusPoints[Math.Min(i + 1, count)].ToPoint());

                    StylusPoint point = new()
                    {
                        PressureFactor = speed >= 0.25
                            ? ToPressureFactor(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2)
                            : speed >= 0.05
                                ? DefaultPressureFactor
                                : ToPressureFactor(0.5 + 0.4 * (0.05 - speed) / 0.05),
                        X = e.Stroke.StylusPoints[i].X,
                        Y = e.Stroke.StylusPoints[i].Y
                    };
                    stylusPoints.Add(point);
                }

                e.Stroke.StylusPoints = stylusPoints;
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 1 pressure");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 1 pressure");
            }
        }

        private void ApplyInkStyleZero(InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                StylusPointCollection stylusPoints = new();
                int count = e.Stroke.StylusPoints.Count - 1;
                double pressure = 0.1;
                int x = 10;
                if (count == 1)
                {
                    return;
                }

                if (count >= x)
                {
                    for (int i = 0; i < count - x; i++)
                    {
                        stylusPoints.Add(new StylusPoint(e.Stroke.StylusPoints[i].X, e.Stroke.StylusPoints[i].Y, DefaultPressureFactor));
                    }

                    for (int i = count - x; i <= count; i++)
                    {
                        stylusPoints.Add(new StylusPoint(
                            e.Stroke.StylusPoints[i].X,
                            e.Stroke.StylusPoints[i].Y,
                            ToPressureFactor((0.5 - pressure) * (count - i) / x + pressure)));
                    }
                }
                else
                {
                    for (int i = 0; i <= count; i++)
                    {
                        stylusPoints.Add(new StylusPoint(
                            e.Stroke.StylusPoints[i].X,
                            e.Stroke.StylusPoints[i].Y,
                            ToPressureFactor(0.4 * (count - i) / count + pressure)));
                    }
                }

                e.Stroke.StylusPoints = stylusPoints;
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 0 pressure");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 0 pressure");
            }
        }

        private void ApplyInkStyleThree(ShapeDrawingSessionState shapeDrawingState, InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                StylusPointCollection stylusPoints = new();
                int count = e.Stroke.StylusPoints.Count - 1;
                double pressure = 0.1;
                int x = 8;
                if (shapeDrawingState.LastTouchDownTime < shapeDrawingState.LastTouchUpTime)
                {
                    double k = (shapeDrawingState.LastTouchUpTime - shapeDrawingState.LastTouchDownTime) / (double)(count + 1);
                    x = (int)(1000 / k);
                }

                if (count >= x)
                {
                    for (int i = 0; i < count - x; i++)
                    {
                        stylusPoints.Add(new StylusPoint(e.Stroke.StylusPoints[i].X, e.Stroke.StylusPoints[i].Y, DefaultPressureFactor));
                    }

                    for (int i = count - x; i <= count; i++)
                    {
                        stylusPoints.Add(new StylusPoint(
                            e.Stroke.StylusPoints[i].X,
                            e.Stroke.StylusPoints[i].Y,
                            ToPressureFactor((0.5 - pressure) * (count - i) / x + pressure)));
                    }
                }
                else
                {
                    for (int i = 0; i <= count; i++)
                    {
                        stylusPoints.Add(new StylusPoint(
                            e.Stroke.StylusPoints[i].X,
                            e.Stroke.StylusPoints[i].Y,
                            ToPressureFactor(0.4 * (count - i) / count + pressure)));
                    }
                }

                e.Stroke.StylusPoints = stylusPoints;
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 3 pressure");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "InkToShape | Failed to apply pen style 3 pressure");
            }
        }

        private static Stroke CreateStrokeFromPoints(InkCanvas inkCanvas, IEnumerable<Point> points)
        {
            return new Stroke(new StylusPointCollection(points))
            {
                DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
            };
        }

        private static List<Point> GenerateEllipseGeometry(Point startPoint, Point endPoint, bool drawTop = true, bool drawBottom = true)
        {
            double a = 0.5 * (endPoint.X - startPoint.X);
            double b = 0.5 * (endPoint.Y - startPoint.Y);
            List<Point> pointList = [];

            if (drawTop && drawBottom)
            {
                for (double r = 0; r <= 2 * Math.PI; r += 0.01)
                {
                    pointList.Add(new Point(0.5 * (startPoint.X + endPoint.X) + a * Math.Cos(r), 0.5 * (startPoint.Y + endPoint.Y) + b * Math.Sin(r)));
                }
            }
            else
            {
                if (drawBottom)
                {
                    for (double r = 0; r <= Math.PI; r += 0.01)
                    {
                        pointList.Add(new Point(0.5 * (startPoint.X + endPoint.X) + a * Math.Cos(r), 0.5 * (startPoint.Y + endPoint.Y) + b * Math.Sin(r)));
                    }
                }

                if (drawTop)
                {
                    for (double r = Math.PI; r <= 2 * Math.PI; r += 0.01)
                    {
                        pointList.Add(new Point(0.5 * (startPoint.X + endPoint.X) + a * Math.Cos(r), 0.5 * (startPoint.Y + endPoint.Y) + b * Math.Sin(r)));
                    }
                }
            }

            return pointList;
        }

        private static StrokeCollection GenerateDashedLineEllipseStrokeCollection(InkCanvas inkCanvas, Point startPoint, Point endPoint, bool drawTop = true, bool drawBottom = true)
        {
            double a = 0.5 * (endPoint.X - startPoint.X);
            double b = 0.5 * (endPoint.Y - startPoint.Y);
            double step = 0.05;
            StrokeCollection strokes = [];

            if (drawBottom)
            {
                for (double i = 0.0; i < 1.0; i += step * 1.66)
                {
                    List<Point> pointList = [];
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r += 0.01)
                    {
                        pointList.Add(new Point(0.5 * (startPoint.X + endPoint.X) + a * Math.Cos(r), 0.5 * (startPoint.Y + endPoint.Y) + b * Math.Sin(r)));
                    }

                    strokes.Add(CreateStrokeFromPoints(inkCanvas, pointList));
                }
            }

            if (drawTop)
            {
                for (double i = 1.0; i < 2.0; i += step * 1.66)
                {
                    List<Point> pointList = [];
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r += 0.01)
                    {
                        pointList.Add(new Point(0.5 * (startPoint.X + endPoint.X) + a * Math.Cos(r), 0.5 * (startPoint.Y + endPoint.Y) + b * Math.Sin(r)));
                    }

                    strokes.Add(CreateStrokeFromPoints(inkCanvas, pointList));
                }
            }

            return strokes;
        }

        private static Point[] FixPointsDirection(Point p1, Point p2)
        {
            if (Math.Abs(p1.X - p2.X) / Math.Abs(p1.Y - p2.Y) > 8)
            {
                double x = Math.Abs(p1.Y - p2.Y) / 2;
                if (p1.Y > p2.Y)
                {
                    p1.Y -= x;
                    p2.Y += x;
                }
                else
                {
                    p1.Y += x;
                    p2.Y -= x;
                }
            }
            else if (Math.Abs(p1.Y - p2.Y) / Math.Abs(p1.X - p2.X) > 8)
            {
                double x = Math.Abs(p1.X - p2.X) / 2;
                if (p1.X > p2.X)
                {
                    p1.X -= x;
                    p2.X += x;
                }
                else
                {
                    p1.X += x;
                    p2.X -= x;
                }
            }

            return [p1, p2];
        }

        private static StylusPointCollection GenerateFakePressureTriangle(StylusPointCollection points)
        {
            StylusPointCollection newPoints = [];
            newPoints.Add(new StylusPoint(points[0].X, points[0].Y, ToPressureFactor(0.4)));
            StylusPoint centerPoint = GetCenterPoint(points[0], points[1]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[1].X, points[1].Y, ToPressureFactor(0.4)));
            newPoints.Add(new StylusPoint(points[1].X, points[1].Y, ToPressureFactor(0.4)));
            centerPoint = GetCenterPoint(points[1], points[2]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[2].X, points[2].Y, ToPressureFactor(0.4)));
            newPoints.Add(new StylusPoint(points[2].X, points[2].Y, ToPressureFactor(0.4)));
            centerPoint = GetCenterPoint(points[2], points[0]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[0].X, points[0].Y, ToPressureFactor(0.4)));
            return newPoints;
        }

        private static StylusPointCollection GenerateFakePressureRectangle(StylusPointCollection points)
        {
            StylusPointCollection newPoints = [];
            newPoints.Add(new StylusPoint(points[0].X, points[0].Y, ToPressureFactor(0.4)));
            StylusPoint centerPoint = GetCenterPoint(points[0], points[1]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[1].X, points[1].Y, ToPressureFactor(0.4)));
            newPoints.Add(new StylusPoint(points[1].X, points[1].Y, ToPressureFactor(0.4)));
            centerPoint = GetCenterPoint(points[1], points[2]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[2].X, points[2].Y, ToPressureFactor(0.4)));
            newPoints.Add(new StylusPoint(points[2].X, points[2].Y, ToPressureFactor(0.4)));
            centerPoint = GetCenterPoint(points[2], points[3]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[3].X, points[3].Y, ToPressureFactor(0.4)));
            newPoints.Add(new StylusPoint(points[3].X, points[3].Y, ToPressureFactor(0.4)));
            centerPoint = GetCenterPoint(points[3], points[0]);
            newPoints.Add(new StylusPoint(centerPoint.X, centerPoint.Y, ToPressureFactor(0.8)));
            newPoints.Add(new StylusPoint(points[0].X, points[0].Y, ToPressureFactor(0.4)));
            return newPoints;
        }

        private static StylusPoint GetCenterPoint(StylusPoint point1, StylusPoint point2)
        {
            return new StylusPoint((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        private static Point ToPoint(dynamic point)
        {
            return new Point(point.X, point.Y);
        }

        private static double GetDistance(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
        }

        private static double GetPointSpeed(Point point1, Point point2, Point point3)
        {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y))
                + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) + (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                / 20;
        }

        private static bool IsNearlyZero(double value)
        {
            return Math.Abs(value) <= DoubleComparisonTolerance;
        }

        private static bool IsNearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) <= PressureComparisonTolerance;
        }

        private static float ToPressureFactor(double value)
        {
            return (float)Math.Clamp(value, 0.0, 1.0);
        }
    }
}

