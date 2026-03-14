using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;

namespace Ink_Canvas.Features.Ink.Services
{
    internal sealed class InkCanvasArchiveElementsSerializer
    {
        internal const string DependencyFolderName = "dependencies";
        internal const string LegacyDependencyFolderName = "File Dependency";

        private const string RootElementName = "InkCanvasElements";
        private const string RootVersion = "2";

        private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        private static int generatedElementIndex;
        private readonly IAppLogger logger;

        public InkCanvasArchiveElementsSerializer(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(InkCanvasArchiveElementsSerializer));
        }

        public InkCanvasElementsSaveResult SaveElements(InkCanvas inkCanvas, Stream outputStream)
        {
            ArgumentNullException.ThrowIfNull(inkCanvas);
            ArgumentNullException.ThrowIfNull(outputStream);

            List<string> dependencyFilePaths = [];
            XElement root = new(RootElementName, new XAttribute("version", RootVersion));
            int serializedElementCount = 0;

            foreach (UIElement element in inkCanvas.Children)
            {
                SerializedElementResult? serializedElement = SerializeElement(element);
                if (serializedElement == null)
                {
                    logger.Trace("Elements Save | Skipped unsupported or invalid element while serializing archive.");
                    continue;
                }

                root.Add(serializedElement.Value.Element);
                serializedElementCount++;
                if (!string.IsNullOrWhiteSpace(serializedElement.Value.DependencyFilePath))
                {
                    dependencyFilePaths.Add(serializedElement.Value.DependencyFilePath);
                }
            }

            XDocument document = new(root);
            XmlWriterSettings settings = new()
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(false),
                Indent = true
            };

            using XmlWriter writer = XmlWriter.Create(outputStream, settings);
            document.Save(writer);
            writer.Flush();

            return new InkCanvasElementsSaveResult(serializedElementCount, dependencyFilePaths);
        }

        public InkCanvasElementsLoadResult LoadElements(Stream inputStream, string dependencyDirectory)
        {
            ArgumentNullException.ThrowIfNull(inputStream);
            ArgumentException.ThrowIfNullOrWhiteSpace(dependencyDirectory);

            string dependencyRoot = Path.GetFullPath(dependencyDirectory);
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                CloseInput = false
            };

            using XmlReader reader = XmlReader.Create(inputStream, settings);
            XDocument document = XDocument.Load(reader, LoadOptions.None);
            XElement root = document.Root ?? throw new InvalidDataException("Elements archive is empty.");

            bool useDependencyFileAttribute;
            IEnumerable<XElement> candidates;
            if (string.Equals(root.Name.LocalName, RootElementName, StringComparison.Ordinal))
            {
                useDependencyFileAttribute = true;
                candidates = root.Elements();
            }
            else if (string.Equals(root.Name.LocalName, nameof(InkCanvas), StringComparison.Ordinal))
            {
                useDependencyFileAttribute = false;
                candidates = root.Elements().Where(element => IsSupportedElementName(element.Name.LocalName));
            }
            else
            {
                throw new InvalidDataException($"Unsupported elements root '{root.Name.LocalName}'.");
            }

            List<UIElement> elements = [];
            int skippedElementCount = 0;
            foreach (XElement candidate in candidates)
            {
                try
                {
                    UIElement? element = DeserializeElement(candidate, dependencyRoot, useDependencyFileAttribute);
                    if (element != null)
                    {
                        elements.Add(element);
                    }
                }
                catch (Exception ex) when (IsRecoverableElementException(ex))
                {
                    skippedElementCount++;
                    logger.Error(ex, $"Elements Load | Skipped element '{candidate.Name.LocalName}' during archive restore");
                }
            }

            string? warningMessage = skippedElementCount > 0
                ? $"部分元素未恢复，已跳过 {skippedElementCount} 个元素"
                : null;
            return new InkCanvasElementsLoadResult(elements, skippedElementCount, warningMessage);
        }

        public bool TryGetDependencySourcePath(UIElement element, out string sourcePath)
        {
            sourcePath = null;

            switch (element)
            {
                case Image image:
                    return TryGetImageSourcePath(image.Source, out sourcePath);
                case MediaElement mediaElement when mediaElement.Source?.IsFile is true:
                    sourcePath = mediaElement.Source.LocalPath;
                    return !string.IsNullOrWhiteSpace(sourcePath);
                default:
                    return false;
            }
        }

        private SerializedElementResult? SerializeElement(UIElement element)
        {
            switch (element)
            {
                case Image image:
                    return SerializeImage(image);
                case MediaElement mediaElement:
                    return SerializeMediaElement(mediaElement);
                default:
                    return null;
            }
        }

        private SerializedElementResult? SerializeImage(Image image)
        {
            if (!TryGetImageSourcePath(image.Source, out string sourcePath))
            {
                logger.Error("Elements Save | Image source could not be resolved to a local file.");
                return null;
            }

            XElement element = CreateElementHeader("Image", image, sourcePath);
            element.Add(new XAttribute("Stretch", image.Stretch));
            AppendRenderTransform(element, image.RenderTransform);
            return new SerializedElementResult(element, sourcePath);
        }

        private SerializedElementResult? SerializeMediaElement(MediaElement mediaElement)
        {
            if (mediaElement.Source?.IsFile is not true || string.IsNullOrWhiteSpace(mediaElement.Source.LocalPath))
            {
                logger.Error("Elements Save | Media source could not be resolved to a local file.");
                return null;
            }

            XElement element = CreateElementHeader("MediaElement", mediaElement, mediaElement.Source.LocalPath);
            element.Add(new XAttribute("Stretch", mediaElement.Stretch));
            element.Add(new XAttribute("Volume", ToInvariantString(mediaElement.Volume)));
            element.Add(new XAttribute("Balance", ToInvariantString(mediaElement.Balance)));
            element.Add(new XAttribute("IsMuted", mediaElement.IsMuted));
            element.Add(new XAttribute("ScrubbingEnabled", mediaElement.ScrubbingEnabled));
            AppendRenderTransform(element, mediaElement.RenderTransform);
            return new SerializedElementResult(element, mediaElement.Source.LocalPath);
        }

        private static XElement CreateElementHeader(string elementName, FrameworkElement element, string sourcePath)
        {
            string dependencyFileName = PathSafetyHelper.NormalizeLeafName(Path.GetFileName(sourcePath), "dependency.bin");
            XElement header = new(elementName,
                new XAttribute("Name", EnsureValidElementName(element.Name, elementName)),
                new XAttribute("DependencyFile", dependencyFileName),
                new XAttribute("Opacity", ToInvariantString(element.Opacity)));

            if (!double.IsNaN(element.Width))
            {
                header.Add(new XAttribute("Width", ToInvariantString(element.Width)));
            }

            if (!double.IsNaN(element.Height))
            {
                header.Add(new XAttribute("Height", ToInvariantString(element.Height)));
            }

            double left = InkCanvas.GetLeft(element);
            if (!double.IsNaN(left))
            {
                header.Add(new XAttribute("Left", ToInvariantString(left)));
            }

            double top = InkCanvas.GetTop(element);
            if (!double.IsNaN(top))
            {
                header.Add(new XAttribute("Top", ToInvariantString(top)));
            }

            return header;
        }

        private UIElement? DeserializeElement(XElement element, string dependencyRoot, bool useDependencyFileAttribute)
        {
            switch (element.Name.LocalName)
            {
                case "Image":
                    return DeserializeImage(element, dependencyRoot, useDependencyFileAttribute);
                case "MediaElement":
                    return DeserializeMediaElement(element, dependencyRoot, useDependencyFileAttribute);
                default:
                    logger.Trace($"Elements Load | Unsupported element '{element.Name.LocalName}' was skipped.");
                    return null;
            }
        }

        private Image DeserializeImage(XElement element, string dependencyRoot, bool useDependencyFileAttribute)
        {
            Uri sourceUri = ResolveDependencyUri(element, dependencyRoot, useDependencyFileAttribute);
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = sourceUri;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            Image image = new()
            {
                Source = bitmapImage,
                Stretch = ParseEnumValue(element, nameof(Image.Stretch), Stretch.Uniform),
                Width = ParseDoubleAttribute(element, nameof(Image.Width), double.NaN),
                Height = ParseDoubleAttribute(element, nameof(Image.Height), double.NaN),
                Opacity = ParseDoubleAttribute(element, nameof(UIElement.Opacity), 1d)
            };

            ApplyCommonProperties(image, element);
            return image;
        }

        private MediaElement DeserializeMediaElement(XElement element, string dependencyRoot, bool useDependencyFileAttribute)
        {
            Uri sourceUri = ResolveDependencyUri(element, dependencyRoot, useDependencyFileAttribute);
            MediaElement mediaElement = new()
            {
                Source = sourceUri,
                Stretch = ParseEnumValue(element, nameof(MediaElement.Stretch), Stretch.Uniform),
                Width = ParseDoubleAttribute(element, nameof(MediaElement.Width), double.NaN),
                Height = ParseDoubleAttribute(element, nameof(MediaElement.Height), double.NaN),
                Opacity = ParseDoubleAttribute(element, nameof(UIElement.Opacity), 1d),
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Volume = ParseDoubleAttribute(element, nameof(MediaElement.Volume), 0.5d),
                Balance = ParseDoubleAttribute(element, nameof(MediaElement.Balance), 0d),
                IsMuted = ParseBoolAttribute(element, nameof(MediaElement.IsMuted), false),
                ScrubbingEnabled = ParseBoolAttribute(element, nameof(MediaElement.ScrubbingEnabled), false)
            };

            mediaElement.Loaded += async (_, __) =>
            {
                mediaElement.Play();
                await System.Threading.Tasks.Task.Delay(100);
                mediaElement.Pause();
            };

            ApplyCommonProperties(mediaElement, element);
            return mediaElement;
        }

        private void ApplyCommonProperties(FrameworkElement element, XElement sourceElement)
        {
            element.Name = EnsureValidElementName(GetAttributeValue(sourceElement, "Name"), element.GetType().Name);

            double left = ParseDoubleAttribute(sourceElement, "Canvas.Left", double.NaN);
            if (double.IsNaN(left))
            {
                left = ParseDoubleAttribute(sourceElement, "Left", 0d);
            }

            double top = ParseDoubleAttribute(sourceElement, "Canvas.Top", double.NaN);
            if (double.IsNaN(top))
            {
                top = ParseDoubleAttribute(sourceElement, "Top", 0d);
            }

            InkCanvas.SetLeft(element, left);
            InkCanvas.SetTop(element, top);

            Transform? renderTransform = ParseRenderTransform(sourceElement);
            if (renderTransform != null)
            {
                element.RenderTransform = renderTransform;
            }
        }

        private Transform? ParseRenderTransform(XElement element)
        {
            foreach (XElement child in element.Elements())
            {
                string localName = child.Name.LocalName;
                if (string.Equals(localName, "RenderTransform", StringComparison.Ordinal) ||
                    localName.EndsWith(".RenderTransform", StringComparison.Ordinal))
                {
                    XElement? transformElement = child.Elements().FirstOrDefault();
                    return ParseTransform(transformElement);
                }
            }

            return null;
        }

        private Transform? ParseTransform(XElement? transformElement)
        {
            if (transformElement == null)
            {
                return null;
            }

            switch (transformElement.Name.LocalName)
            {
                case nameof(TransformGroup):
                    TransformGroup transformGroup = new();
                    foreach (XElement child in transformElement.Elements())
                    {
                        Transform? childTransform = ParseTransform(child);
                        if (childTransform != null)
                        {
                            transformGroup.Children.Add(childTransform);
                        }
                    }

                    return transformGroup;
                case nameof(ScaleTransform):
                    return new ScaleTransform(
                        ParseDoubleAttribute(transformElement, nameof(ScaleTransform.ScaleX), 1d),
                        ParseDoubleAttribute(transformElement, nameof(ScaleTransform.ScaleY), 1d),
                        ParseDoubleAttribute(transformElement, nameof(ScaleTransform.CenterX), 0d),
                        ParseDoubleAttribute(transformElement, nameof(ScaleTransform.CenterY), 0d));
                case nameof(TranslateTransform):
                    return new TranslateTransform(
                        ParseDoubleAttribute(transformElement, nameof(TranslateTransform.X), 0d),
                        ParseDoubleAttribute(transformElement, nameof(TranslateTransform.Y), 0d));
                case nameof(RotateTransform):
                    return new RotateTransform(
                        ParseDoubleAttribute(transformElement, nameof(RotateTransform.Angle), 0d),
                        ParseDoubleAttribute(transformElement, nameof(RotateTransform.CenterX), 0d),
                        ParseDoubleAttribute(transformElement, nameof(RotateTransform.CenterY), 0d));
                case nameof(SkewTransform):
                    return new SkewTransform(
                        ParseDoubleAttribute(transformElement, nameof(SkewTransform.AngleX), 0d),
                        ParseDoubleAttribute(transformElement, nameof(SkewTransform.AngleY), 0d),
                        ParseDoubleAttribute(transformElement, nameof(SkewTransform.CenterX), 0d),
                        ParseDoubleAttribute(transformElement, nameof(SkewTransform.CenterY), 0d));
                case nameof(MatrixTransform):
                    return new MatrixTransform(new Matrix(
                        ParseDoubleAttribute(transformElement, "M11", 1d),
                        ParseDoubleAttribute(transformElement, "M12", 0d),
                        ParseDoubleAttribute(transformElement, "M21", 0d),
                        ParseDoubleAttribute(transformElement, "M22", 1d),
                        ParseDoubleAttribute(transformElement, nameof(Matrix.OffsetX), 0d),
                        ParseDoubleAttribute(transformElement, nameof(Matrix.OffsetY), 0d)));
                default:
                    logger.Trace($"Elements Load | Unsupported transform '{transformElement.Name.LocalName}' was skipped.");
                    return null;
            }
        }

        private void AppendRenderTransform(XElement element, Transform renderTransform)
        {
            XElement? transformElement = SerializeTransform(renderTransform);
            if (transformElement == null)
            {
                return;
            }

            element.Add(new XElement("RenderTransform", transformElement));
        }

        private XElement? SerializeTransform(Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

            switch (transform)
            {
                case TransformGroup transformGroup:
                    XElement groupElement = new(nameof(TransformGroup));
                    foreach (Transform child in transformGroup.Children)
                    {
                        XElement? childElement = SerializeTransform(child);
                        if (childElement != null)
                        {
                            groupElement.Add(childElement);
                        }
                    }

                    return groupElement.HasElements ? groupElement : null;
                case ScaleTransform scaleTransform:
                    return new XElement(nameof(ScaleTransform),
                        new XAttribute(nameof(ScaleTransform.ScaleX), ToInvariantString(scaleTransform.ScaleX)),
                        new XAttribute(nameof(ScaleTransform.ScaleY), ToInvariantString(scaleTransform.ScaleY)),
                        new XAttribute(nameof(ScaleTransform.CenterX), ToInvariantString(scaleTransform.CenterX)),
                        new XAttribute(nameof(ScaleTransform.CenterY), ToInvariantString(scaleTransform.CenterY)));
                case TranslateTransform translateTransform:
                    return new XElement(nameof(TranslateTransform),
                        new XAttribute(nameof(TranslateTransform.X), ToInvariantString(translateTransform.X)),
                        new XAttribute(nameof(TranslateTransform.Y), ToInvariantString(translateTransform.Y)));
                case RotateTransform rotateTransform:
                    return new XElement(nameof(RotateTransform),
                        new XAttribute(nameof(RotateTransform.Angle), ToInvariantString(rotateTransform.Angle)),
                        new XAttribute(nameof(RotateTransform.CenterX), ToInvariantString(rotateTransform.CenterX)),
                        new XAttribute(nameof(RotateTransform.CenterY), ToInvariantString(rotateTransform.CenterY)));
                case SkewTransform skewTransform:
                    return new XElement(nameof(SkewTransform),
                        new XAttribute(nameof(SkewTransform.AngleX), ToInvariantString(skewTransform.AngleX)),
                        new XAttribute(nameof(SkewTransform.AngleY), ToInvariantString(skewTransform.AngleY)),
                        new XAttribute(nameof(SkewTransform.CenterX), ToInvariantString(skewTransform.CenterX)),
                        new XAttribute(nameof(SkewTransform.CenterY), ToInvariantString(skewTransform.CenterY)));
                case MatrixTransform matrixTransform:
                    Matrix matrix = matrixTransform.Matrix;
                    return new XElement(nameof(MatrixTransform),
                        new XAttribute("M11", ToInvariantString(matrix.M11)),
                        new XAttribute("M12", ToInvariantString(matrix.M12)),
                        new XAttribute("M21", ToInvariantString(matrix.M21)),
                        new XAttribute("M22", ToInvariantString(matrix.M22)),
                        new XAttribute(nameof(Matrix.OffsetX), ToInvariantString(matrix.OffsetX)),
                        new XAttribute(nameof(Matrix.OffsetY), ToInvariantString(matrix.OffsetY)));
                default:
                    logger.Trace($"Elements Save | Unsupported transform '{transform.GetType().Name}' was skipped.");
                    return null;
            }
        }

        private static Uri ResolveDependencyUri(XElement element, string dependencyRoot, bool useDependencyFileAttribute)
        {
            string? sourceValue = useDependencyFileAttribute
                ? GetAttributeValue(element, "DependencyFile")
                : GetAttributeValue(element, "Source", "DependencyFile");

            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                throw new InvalidDataException($"Element '{element.Name.LocalName}' is missing its source path.");
            }

            if (!useDependencyFileAttribute)
            {
                if (Uri.TryCreate(sourceValue, UriKind.Absolute, out Uri? absoluteUri))
                {
                    if (!absoluteUri.IsFile)
                    {
                        throw new InvalidDataException("Only local file sources are supported.");
                    }

                    if (File.Exists(absoluteUri.LocalPath))
                    {
                        return new Uri(Path.GetFullPath(absoluteUri.LocalPath));
                    }

                    sourceValue = absoluteUri.LocalPath;
                }
                else if (Path.IsPathRooted(sourceValue) && File.Exists(sourceValue))
                {
                    return new Uri(Path.GetFullPath(sourceValue));
                }
            }

            string dependencyFileName = PathSafetyHelper.NormalizeLeafName(Path.GetFileName(sourceValue), "dependency.bin");
            string candidatePath = Path.GetFullPath(Path.Join(dependencyRoot, dependencyFileName));
            EnsurePathIsUnderRoot(candidatePath, dependencyRoot, "Element dependency");

            if (!File.Exists(candidatePath))
            {
                throw new FileNotFoundException("Element dependency file was not extracted.", candidatePath);
            }

            return new Uri(candidatePath);
        }

        private static void EnsurePathIsUnderRoot(string candidatePath, string rootPath, string context)
        {
            string normalizedRoot = PathSafetyHelper.AppendDirectorySeparator(Path.GetFullPath(rootPath));
            string normalizedCandidate = Path.GetFullPath(candidatePath);

            if (!normalizedCandidate.StartsWith(normalizedRoot, PathComparison))
            {
                throw new InvalidDataException($"{context} path escaped the target directory.");
            }
        }

        private static bool TryGetImageSourcePath(ImageSource imageSource, out string sourcePath)
        {
            switch (imageSource)
            {
                case BitmapImage bitmapImage when bitmapImage.UriSource?.IsFile == true:
                    sourcePath = bitmapImage.UriSource.LocalPath;
                    return !string.IsNullOrWhiteSpace(sourcePath);
                case TransformedBitmap transformedBitmap:
                    return TryGetImageSourcePath(transformedBitmap.Source, out sourcePath);
                default:
                    sourcePath = null;
                    return false;
            }
        }

        private static bool IsSupportedElementName(string elementName)
        {
            return string.Equals(elementName, "Image", StringComparison.Ordinal) ||
                   string.Equals(elementName, "MediaElement", StringComparison.Ordinal);
        }

        private static bool IsRecoverableElementException(Exception exception)
        {
            return exception is ArgumentException
                or FileNotFoundException
                or InvalidDataException
                or InvalidOperationException
                or IOException
                or NotSupportedException
                or UriFormatException;
        }

        private static TEnum ParseEnumValue<TEnum>(XElement element, string attributeName, TEnum defaultValue)
            where TEnum : struct
        {
            string? value = GetAttributeValue(element, attributeName);
            return Enum.TryParse(value, true, out TEnum parsedValue) ? parsedValue : defaultValue;
        }

        private static bool ParseBoolAttribute(XElement element, string attributeName, bool defaultValue)
        {
            string? value = GetAttributeValue(element, attributeName);
            return bool.TryParse(value, out bool parsedValue) ? parsedValue : defaultValue;
        }

        private static double ParseDoubleAttribute(XElement element, string attributeName, double defaultValue)
        {
            string? value = GetAttributeValue(element, attributeName);
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        private static string? GetAttributeValue(XElement element, params string[] attributeNames)
        {
            foreach (string attributeName in attributeNames)
            {
                string? value = FindAttributeValue(element, attributeName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string? FindAttributeValue(XElement element, string attributeName)
        {
            string suffix = attributeName.Contains('.')
                ? attributeName[(attributeName.LastIndexOf('.') + 1)..]
                : attributeName;

            foreach (XAttribute attribute in element.Attributes())
            {
                string localName = attribute.Name.LocalName;
                if (string.Equals(localName, attributeName, StringComparison.Ordinal) ||
                    string.Equals(localName, suffix, StringComparison.Ordinal) ||
                    localName.EndsWith("." + suffix, StringComparison.Ordinal))
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        private static string EnsureValidElementName(string? requestedName, string prefix)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                return GenerateElementName(prefix);
            }

            StringBuilder builder = new(requestedName.Length);
            foreach (char character in requestedName.Where(character => char.IsLetterOrDigit(character) || character == '_'))
            {
                builder.Append(character);
            }

            if (builder.Length == 0 || (!char.IsLetter(builder[0]) && builder[0] != '_'))
            {
                return GenerateElementName(prefix);
            }

            return builder.ToString();
        }

        private static string GenerateElementName(string prefix)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "element" : prefix.ToLowerInvariant();
            return $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{generatedElementIndex++}";
        }

        private static string ToInvariantString(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private readonly record struct SerializedElementResult(XElement Element, string? DependencyFilePath);
    }
}
