using Ink_Canvas.Features.Ink.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Features.Ink.Services
{
    internal static class InkDocumentModelAdapter
    {
        public static InkDocumentModel FromCanvas(InkCanvas inkCanvas)
        {
            ArgumentNullException.ThrowIfNull(inkCanvas);
            return FromStrokeAndElementCollections(inkCanvas.Strokes, inkCanvas.Children.Cast<UIElement>());
        }

        public static InkDocumentModel FromStrokeAndElementCollections(StrokeCollection strokes, IEnumerable<UIElement> elements)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            ArgumentNullException.ThrowIfNull(elements);

            InkDocumentModel document = new();
            foreach (Stroke stroke in strokes)
            {
                document.Strokes.Add(FromStroke(stroke));
            }

            foreach (UIElement element in elements)
            {
                document.ElementsRef.Add(CreateElementRef(element));
            }

            return document;
        }

        public static StrokeCollection ToStrokeCollection(IReadOnlyList<InkStrokeModel> models)
        {
            ArgumentNullException.ThrowIfNull(models);
            StrokeCollection strokes = new();
            foreach (InkStrokeModel model in models)
            {
                strokes.Add(ToStroke(model));
            }

            return strokes;
        }

        public static InkStrokeModel FromStroke(Stroke stroke)
        {
            ArgumentNullException.ThrowIfNull(stroke);

            InkStrokeModel model = new()
            {
                StrokeId = TryGetOrCreateStableStrokeId(stroke),
                Argb = ColorToArgb(stroke.DrawingAttributes.Color),
                Width = (float)stroke.DrawingAttributes.Width,
                Height = (float)stroke.DrawingAttributes.Height,
                StylusTip = (byte)stroke.DrawingAttributes.StylusTip,
                Flags = BuildFlags(stroke.DrawingAttributes)
            };

            foreach (StylusPoint point in stroke.StylusPoints)
            {
                model.Points.Add(new InkStrokePointModel(
                    (float)point.X,
                    (float)point.Y,
                    ToPressureQ15(point.PressureFactor),
                    0));
            }

            return model;
        }

        public static Stroke ToStroke(InkStrokeModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            StylusPointCollection points = new();
            foreach (InkStrokePointModel point in model.Points)
            {
                points.Add(new StylusPoint(point.X, point.Y, ToPressureFactor(point.PressureQ15)));
            }

            DrawingAttributes drawingAttributes = new()
            {
                Color = ArgbToColor(model.Argb),
                Width = model.Width,
                Height = model.Height,
                StylusTip = Enum.IsDefined(typeof(StylusTip), (int)model.StylusTip)
                    ? (StylusTip)model.StylusTip
                    : StylusTip.Ellipse,
                IsHighlighter = (model.Flags & 0b0000_0001) != 0,
                FitToCurve = (model.Flags & 0b0000_0010) != 0,
                IgnorePressure = (model.Flags & 0b0000_0100) != 0
            };

            Stroke stroke = new(points)
            {
                DrawingAttributes = drawingAttributes
            };
            TryAttachStableStrokeId(stroke, model.StrokeId);
            return stroke;
        }

        private static InkElementReference CreateElementRef(UIElement element)
        {
            string elementType = element.GetType().Name;
            string elementId = element is FrameworkElement frameworkElement && !string.IsNullOrWhiteSpace(frameworkElement.Name)
                ? frameworkElement.Name
                : $"{elementType}_{Guid.NewGuid():N}";

            return new InkElementReference
            {
                ElementId = elementId,
                ElementType = elementType
            };
        }

        private static Guid TryGetOrCreateStableStrokeId(Stroke stroke)
        {
            if (stroke.ContainsPropertyData(StrokeIdGuid))
            {
                object value = stroke.GetPropertyData(StrokeIdGuid);
                if (value is string valueText && Guid.TryParse(valueText, out Guid parsedGuid))
                {
                    return parsedGuid;
                }

                if (value is byte[] valueBytes && valueBytes.Length == 16)
                {
                    return new Guid(valueBytes);
                }

                if (value is Guid existing)
                {
                    return existing;
                }
            }

            Guid strokeId = Guid.NewGuid();
            TryAttachStableStrokeId(stroke, strokeId);
            return strokeId;
        }

        private static void TryAttachStableStrokeId(Stroke stroke, Guid strokeId)
        {
            try
            {
                stroke.AddPropertyData(StrokeIdGuid, strokeId.ToString("N"));
            }
            catch (ArgumentException)
            {
                // WPF Ink extended properties only accept a narrow type set.
                // Ignore attachment failure and keep runtime-generated stroke id.
            }
            catch (InvalidOperationException)
            {
                // Ignore attachment failure and keep runtime-generated stroke id.
            }
        }

        private static byte BuildFlags(DrawingAttributes attributes)
        {
            byte flags = 0;
            if (attributes.IsHighlighter)
            {
                flags |= 0b0000_0001;
            }

            if (attributes.FitToCurve)
            {
                flags |= 0b0000_0010;
            }

            if (attributes.IgnorePressure)
            {
                flags |= 0b0000_0100;
            }

            return flags;
        }

        private static uint ColorToArgb(Color color)
        {
            return ((uint)color.A << 24)
                   | ((uint)color.R << 16)
                   | ((uint)color.G << 8)
                   | color.B;
        }

        private static Color ArgbToColor(uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }

        private static ushort ToPressureQ15(float pressureFactor)
        {
            float clamped = Math.Clamp(pressureFactor, 0f, 1f);
            return (ushort)Math.Round(clamped * 65535f, MidpointRounding.AwayFromZero);
        }

        private static float ToPressureFactor(ushort pressureQ15)
        {
            return pressureQ15 / 65535f;
        }

        private static readonly Guid StrokeIdGuid = new("7A2B6A49-AB9E-46EA-9185-C348A3071A55");
    }
}
