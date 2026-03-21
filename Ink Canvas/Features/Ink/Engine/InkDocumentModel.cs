using System;
using System.Collections.Generic;

namespace Ink_Canvas.Features.Ink.Engine
{
    internal sealed class InkDocumentModel
    {
        public List<InkStrokeModel> Strokes { get; } = [];

        public List<InkElementReference> ElementsRef { get; } = [];
    }

    internal sealed class InkStrokeModel
    {
        public Guid StrokeId { get; set; } = Guid.NewGuid();

        public byte ToolKind { get; set; }

        public byte Flags { get; set; }

        public uint Argb { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public byte StylusTip { get; set; }

        public List<InkStrokePointModel> Points { get; } = [];

        public Dictionary<ushort, byte[]> Extensions { get; } = [];
    }

    internal readonly record struct InkStrokePointModel(float X, float Y, ushort PressureQ15, ushort DeltaTimeMs);

    internal sealed class InkElementReference
    {
        public string ElementId { get; set; } = string.Empty;

        public string ElementType { get; set; } = string.Empty;
    }

    internal sealed class InkDocumentSnapshot
    {
        public InkDocumentSnapshot(InkDocumentModel document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public InkDocumentModel Document { get; }
    }
}
