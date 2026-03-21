using System;

namespace Ink_Canvas.Features.Ink.Services
{
    internal enum InkBackendKind
    {
        Legacy = 0,
        SkiaV1 = 1
    }

    internal enum InkRecognizerKind
    {
        V1 = 1,
        V2 = 2
    }

    internal enum InkArchiveWriteFormatKind
    {
        V3 = 3,
        V4 = 4
    }

    internal readonly record struct InkRuntimeRouting(
        InkBackendKind Backend,
        InkRecognizerKind Recognizer,
        InkArchiveWriteFormatKind ArchiveWriteFormat,
        bool IsEmergencyFallbackEnabled);

    internal static class InkRuntimeSettingsResolver
    {
        public static InkRuntimeRouting Resolve(global::Ink_Canvas.Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings.Advanced.InkEngineEmergencyFallback)
            {
                return new InkRuntimeRouting(
                    InkBackendKind.Legacy,
                    InkRecognizerKind.V1,
                    InkArchiveWriteFormatKind.V3,
                    true);
            }

            InkBackendKind backend = ParseBackend(settings.Advanced.InkBackendOverride, settings.Canvas.InkBackend);
            InkRecognizerKind recognizer = ParseRecognizer(settings.Advanced.RecognizerOverride, settings.InkToShape.RecognizerVersion);
            InkArchiveWriteFormatKind writeFormat = ParseArchiveWriteFormat(
                settings.Advanced.ArchiveWriteFormatOverride,
                settings.Canvas.ArchiveWriteFormat);

            return new InkRuntimeRouting(backend, recognizer, writeFormat, false);
        }

        public static bool Normalize(global::Ink_Canvas.Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            bool changed = false;
            changed |= NormalizeValue(
                settings.Canvas.InkBackend,
                ParseBackend(null, settings.Canvas.InkBackend) == InkBackendKind.Legacy ? InkRuntimeDefaults.InkBackendLegacy : InkRuntimeDefaults.InkBackendSkiaV1,
                v => settings.Canvas.InkBackend = v);
            changed |= NormalizeValue(
                settings.InkToShape.RecognizerVersion,
                ParseRecognizer(null, settings.InkToShape.RecognizerVersion) == InkRecognizerKind.V1 ? InkRuntimeDefaults.RecognizerV1 : InkRuntimeDefaults.RecognizerV2,
                v => settings.InkToShape.RecognizerVersion = v);
            changed |= NormalizeValue(
                settings.Canvas.ArchiveWriteFormat,
                ParseArchiveWriteFormat(null, settings.Canvas.ArchiveWriteFormat) == InkArchiveWriteFormatKind.V3 ? InkRuntimeDefaults.ArchiveWriteFormatV3 : InkRuntimeDefaults.ArchiveWriteFormatV4,
                v => settings.Canvas.ArchiveWriteFormat = v);

            changed |= NormalizeOptionalOverride(
                settings.Advanced.InkBackendOverride,
                value => ParseBackend(null, value) == InkBackendKind.Legacy ? InkRuntimeDefaults.InkBackendLegacy : InkRuntimeDefaults.InkBackendSkiaV1,
                value => settings.Advanced.InkBackendOverride = value);
            changed |= NormalizeOptionalOverride(
                settings.Advanced.RecognizerOverride,
                value => ParseRecognizer(null, value) == InkRecognizerKind.V1 ? InkRuntimeDefaults.RecognizerV1 : InkRuntimeDefaults.RecognizerV2,
                value => settings.Advanced.RecognizerOverride = value);
            changed |= NormalizeOptionalOverride(
                settings.Advanced.ArchiveWriteFormatOverride,
                value => ParseArchiveWriteFormat(null, value) == InkArchiveWriteFormatKind.V3 ? InkRuntimeDefaults.ArchiveWriteFormatV3 : InkRuntimeDefaults.ArchiveWriteFormatV4,
                value => settings.Advanced.ArchiveWriteFormatOverride = value);

            return changed;
        }

        public static bool IsRoutingSettingProperty(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            return propertyName == "InkBackend"
                || propertyName == "RecognizerVersion"
                || propertyName == "ArchiveWriteFormat"
                || propertyName == "InkEngineEmergencyFallback"
                || propertyName == "InkBackendOverride"
                || propertyName == "RecognizerOverride"
                || propertyName == "ArchiveWriteFormatOverride";
        }

        private static bool NormalizeValue(string current, string target, Action<string> assign)
        {
            if (string.Equals(current, target, StringComparison.Ordinal))
            {
                return false;
            }

            assign(target);
            return true;
        }

        private static bool NormalizeOptionalOverride(string? current, Func<string, string> normalize, Action<string?> assign)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                if (current is null)
                {
                    return false;
                }

                assign(null);
                return true;
            }

            string normalized = normalize(current);
            if (string.Equals(current, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            assign(normalized);
            return true;
        }

        private static InkBackendKind ParseBackend(string? overrideValue, string? regularValue)
        {
            string? value = string.IsNullOrWhiteSpace(overrideValue) ? regularValue : overrideValue;
            if (string.Equals(value, InkRuntimeDefaults.InkBackendLegacy, StringComparison.OrdinalIgnoreCase))
            {
                return InkBackendKind.Legacy;
            }

            return InkBackendKind.SkiaV1;
        }

        private static InkRecognizerKind ParseRecognizer(string? overrideValue, string? regularValue)
        {
            string? value = string.IsNullOrWhiteSpace(overrideValue) ? regularValue : overrideValue;
            if (string.Equals(value, InkRuntimeDefaults.RecognizerV1, StringComparison.OrdinalIgnoreCase))
            {
                return InkRecognizerKind.V1;
            }

            return InkRecognizerKind.V2;
        }

        private static InkArchiveWriteFormatKind ParseArchiveWriteFormat(string? overrideValue, string? regularValue)
        {
            string? value = string.IsNullOrWhiteSpace(overrideValue) ? regularValue : overrideValue;
            if (string.Equals(value, InkRuntimeDefaults.ArchiveWriteFormatV3, StringComparison.OrdinalIgnoreCase))
            {
                return InkArchiveWriteFormatKind.V3;
            }

            return InkArchiveWriteFormatKind.V4;
        }
    }
}
