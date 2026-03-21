using Ink_Canvas.Features.Ink.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Services
{
    internal static class InkStrokeV2Serializer
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("ICSV2");
        private const ushort MajorVersion = 2;
        private const ushort MinorVersion = 0;
        private const byte CompressionNone = 0;
        private const byte CompressionBrotli = 1;

        public static byte[] Serialize(StrokeCollection strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);

            List<InkStrokeModel> models = [];
            foreach (Stroke stroke in strokes)
            {
                models.Add(InkDocumentModelAdapter.FromStroke(stroke));
            }

            byte[] uncompressedPayload = BuildUncompressedPayload(models);
            byte[] compressedPayload = Compress(uncompressedPayload);
            bool useCompression = compressedPayload.Length < uncompressedPayload.Length;

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(Magic);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(useCompression ? CompressionBrotli : CompressionNone);
            writer.Write((uint)uncompressedPayload.Length);
            writer.Write(CalculateCrc32(uncompressedPayload));
            writer.Write(useCompression ? compressedPayload : uncompressedPayload);
            writer.Flush();
            return stream.ToArray();
        }

        public static StrokeCollection Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                throw new InvalidDataException("Ink stroke payload is empty.");
            }

            using MemoryStream stream = new(payload, writable: false);
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

            byte[] magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length || !Magic.AsSpan().SequenceEqual(magic))
            {
                throw new InvalidDataException("Invalid v2 stroke payload magic header.");
            }

            ushort major = reader.ReadUInt16();
            ushort minor = reader.ReadUInt16();
            if (major != MajorVersion)
            {
                throw new InvalidDataException($"Unsupported v2 stroke payload major version '{major}'.");
            }

            _ = minor; // reserved for forward-compatible extension fields

            byte compression = reader.ReadByte();
            uint uncompressedLength = reader.ReadUInt32();
            uint expectedCrc = reader.ReadUInt32();

            byte[] encodedPayload = reader.ReadBytes((int)(stream.Length - stream.Position));
            byte[] uncompressedPayload = compression switch
            {
                CompressionNone => encodedPayload,
                CompressionBrotli => Decompress(encodedPayload),
                _ => throw new InvalidDataException($"Unsupported compression algorithm '{compression}'.")
            };

            if (uncompressedPayload.Length != uncompressedLength)
            {
                throw new InvalidDataException("Corrupted v2 stroke payload length.");
            }

            uint actualCrc = CalculateCrc32(uncompressedPayload);
            if (actualCrc != expectedCrc)
            {
                throw new InvalidDataException("Corrupted v2 stroke payload CRC32.");
            }

            List<InkStrokeModel> models = ParseUncompressedPayload(uncompressedPayload);
            return InkDocumentModelAdapter.ToStrokeCollection(models);
        }

        private static byte[] BuildUncompressedPayload(IReadOnlyList<InkStrokeModel> models)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write((uint)models.Count);

            foreach (InkStrokeModel model in models)
            {
                writer.Write(model.StrokeId.ToByteArray());
                writer.Write(model.ToolKind);
                writer.Write(model.Flags);
                writer.Write(model.Argb);
                writer.Write(model.Width);
                writer.Write(model.Height);
                writer.Write(model.StylusTip);

                writer.Write((uint)model.Points.Count);
                foreach (InkStrokePointModel point in model.Points)
                {
                    writer.Write(point.X);
                    writer.Write(point.Y);
                    writer.Write(point.PressureQ15);
                    writer.Write(point.DeltaTimeMs);
                }

                writer.Write((ushort)model.Extensions.Count);
                foreach (KeyValuePair<ushort, byte[]> extension in model.Extensions)
                {
                    writer.Write(extension.Key);
                    byte[] value = extension.Value ?? [];
                    writer.Write((ushort)value.Length);
                    writer.Write(value);
                }
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static List<InkStrokeModel> ParseUncompressedPayload(byte[] payload)
        {
            using MemoryStream stream = new(payload, writable: false);
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

            uint strokeCount = reader.ReadUInt32();
            List<InkStrokeModel> models = new((int)strokeCount);

            for (int i = 0; i < strokeCount; i++)
            {
                InkStrokeModel model = new()
                {
                    StrokeId = new Guid(reader.ReadBytes(16)),
                    ToolKind = reader.ReadByte(),
                    Flags = reader.ReadByte(),
                    Argb = reader.ReadUInt32(),
                    Width = reader.ReadSingle(),
                    Height = reader.ReadSingle(),
                    StylusTip = reader.ReadByte()
                };

                uint pointCount = reader.ReadUInt32();
                for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                {
                    model.Points.Add(new InkStrokePointModel(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadUInt16(),
                        reader.ReadUInt16()));
                }

                ushort extensionCount = reader.ReadUInt16();
                for (int extensionIndex = 0; extensionIndex < extensionCount; extensionIndex++)
                {
                    ushort key = reader.ReadUInt16();
                    ushort length = reader.ReadUInt16();
                    byte[] value = reader.ReadBytes(length);
                    model.Extensions[key] = value;
                }

                models.Add(model);
            }

            return models;
        }

        private static byte[] Compress(byte[] payload)
        {
            using MemoryStream output = new();
            using (BrotliStream brotli = new(output, CompressionLevel.Fastest, leaveOpen: true))
            {
                brotli.Write(payload, 0, payload.Length);
            }

            return output.ToArray();
        }

        private static byte[] Decompress(byte[] payload)
        {
            using MemoryStream input = new(payload, writable: false);
            using BrotliStream brotli = new(input, CompressionMode.Decompress, leaveOpen: false);
            using MemoryStream output = new();
            brotli.CopyTo(output);
            return output.ToArray();
        }

        private static uint CalculateCrc32(byte[] payload)
        {
            const uint polynomial = 0xEDB88320;
            uint crc = 0xFFFFFFFF;

            foreach (byte value in payload)
            {
                crc ^= value;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 1) == 1
                        ? (crc >> 1) ^ polynomial
                        : crc >> 1;
                }
            }

            return ~crc;
        }
    }
}
