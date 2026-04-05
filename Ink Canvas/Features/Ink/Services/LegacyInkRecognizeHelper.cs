using System;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Services
{
    /// <summary>
    /// Behavioral compatibility layer for the legacy InkAnalyzer-based recognizer.
    /// It stays managed-only so V1 can run in x64/ARM64 without loading the old x86 binaries.
    /// </summary>
    internal static class LegacyInkRecognizeHelper
    {
        public static RecognizedShapeResult? RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                return null;
            }

            int strokeCount = strokes.Count;
            RecognizedShapeResult?[] memo = new RecognizedShapeResult?[strokeCount];
            bool[] evaluated = new bool[strokeCount];

            RecognizedShapeResult? EvaluateSuffix(int startIndex)
            {
                if (!evaluated[startIndex])
                {
                    StrokeCollection candidateStrokes = CreateSuffixStrokeCollection(strokes, startIndex);
                    memo[startIndex] = InkRecognizeHelper.RecognizeShape(candidateStrokes);
                    evaluated[startIndex] = true;
                }

                return memo[startIndex];
            }

            RecognizedShapeResult? primaryCandidate = RecognizeLatestSupportedSuffix(strokeCount, EvaluateSuffix);
            RecognizedShapeResult? preferredClosedCandidate = RecognizePreferredRecentClosedShape(strokeCount, EvaluateSuffix);

            if (preferredClosedCandidate != null)
            {
                return preferredClosedCandidate;
            }

            if (primaryCandidate != null)
            {
                return primaryCandidate;
            }

            RecognizedShapeResult? bestCandidate = null;
            double bestScore = double.MinValue;
            for (int startIndex = strokeCount - 1; startIndex >= 0; startIndex--)
            {
                RecognizedShapeResult? candidate = EvaluateSuffix(startIndex);
                if (candidate == null || !IsSupportedKind(candidate.Kind))
                {
                    continue;
                }

                double score = GetLegacyScore(candidate, startIndex, strokeCount);
                if (bestCandidate == null || score > bestScore)
                {
                    bestCandidate = candidate;
                    bestScore = score;
                }
            }

            return bestCandidate;
        }

        private static StrokeCollection CreateSuffixStrokeCollection(StrokeCollection strokes, int startIndex)
        {
            if (startIndex < 0 || startIndex >= strokes.Count)
            {
                return [];
            }

            int count = strokes.Count - startIndex;
            Stroke[] suffixStrokes = new Stroke[count];
            for (int i = 0; i < count; i++)
            {
                suffixStrokes[i] = strokes[startIndex + i];
            }

            return new StrokeCollection(suffixStrokes);
        }

        private static bool IsSupportedKind(RecognizedShapeKind kind)
        {
            return kind is RecognizedShapeKind.Line
                or RecognizedShapeKind.Polyline
                or RecognizedShapeKind.Arc
                or RecognizedShapeKind.Circle
                or RecognizedShapeKind.Ellipse
                or RecognizedShapeKind.Triangle
                or RecognizedShapeKind.Rectangle
                or RecognizedShapeKind.Square
                or RecognizedShapeKind.Diamond
                or RecognizedShapeKind.Parallelogram;
        }

        private static RecognizedShapeResult? RecognizeLatestSupportedSuffix(int strokeCount, Func<int, RecognizedShapeResult?> evaluateSuffix)
        {
            for (int startIndex = 0; startIndex < strokeCount; startIndex++)
            {
                RecognizedShapeResult? candidate = evaluateSuffix(startIndex);
                if (candidate != null && IsSupportedKind(candidate.Kind))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static RecognizedShapeResult? RecognizePreferredRecentClosedShape(int strokeCount, Func<int, RecognizedShapeResult?> evaluateSuffix)
        {
            for (int startIndex = strokeCount - 1; startIndex >= 0; startIndex--)
            {
                RecognizedShapeResult? candidate = evaluateSuffix(startIndex);
                if (candidate?.Kind is RecognizedShapeKind.Circle or RecognizedShapeKind.Ellipse)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static double GetLegacyScore(RecognizedShapeResult result, int startIndex, int totalStrokeCount)
        {
            double kindWeight = result.Kind switch
            {
                RecognizedShapeKind.Circle => 4.0,
                RecognizedShapeKind.Ellipse => 4.0,
                RecognizedShapeKind.Triangle => 3.0,
                RecognizedShapeKind.Rectangle => 3.0,
                RecognizedShapeKind.Square => 3.0,
                RecognizedShapeKind.Diamond => 3.0,
                RecognizedShapeKind.Parallelogram => 3.0,
                RecognizedShapeKind.Arc => 2.0,
                RecognizedShapeKind.Line => 1.5,
                RecognizedShapeKind.Polyline => 1.25,
                _ => 0.0
            };

            double recencyWeight = totalStrokeCount <= 1
                ? 0.0
                : (double)startIndex / (totalStrokeCount - 1);

            return kindWeight
                + result.Confidence
                + (result.SourceStrokes.Count * 0.1)
                + (result.ClosureDisposition == ClosureDisposition.Closed ? 0.25 : 0.0)
                + recencyWeight * 0.35;
        }
    }
}
