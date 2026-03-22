using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas.Features.Ink.Services
{
    public static class InkRecognizeHelper
    {
        private const double MinimumClosureDistance = 24.0;
        private const double ClosureDistanceRatio = 0.18;
        private const double MinimumResampleSpacing = 6.0;
        private const double MaximumResampleSpacing = 12.0;
        private const double EndpointSnapRatio = 0.06;
        private const double MinimumEndpointSnap = 14.0;
        private const double MaximumEndpointSnap = 28.0;
        private const double NearlyEqualDistance = 0.5;
        private const int CornerWindow = 3;
        private const double StrawThresholdRatio = 0.95;
        private const double EndpointPathRatioThreshold = 1.18;
        private const double EndpointDirectionDeviationThreshold = 25.0;
        private const double MinimumLineLength = 120.0;
        private const double MinimumOpenSegmentLength = 18.0;
        private const double MaximumSegmentRms = 6.0;
        private const double MinimumPolygonDimension = 100.0;
        private const double MinimumEllipseDimension = 75.0;
        private const double CircleAxisRatioMin = 0.85;
        private const double CircleAxisRatioMax = 1.15;
        private const double CircleResidualThreshold = 0.18;
        private const double EllipseResidualThreshold = 0.22;
        private const double ArcResidualThreshold = 0.20;
        private const double ArcNearClosedMinimumSweep = 280.0;
        private const double ArcNearClosedMaximumSweep = 330.0;
        private const double ArcMinimumSweep = 25.0;
        private const double ArcMaximumSweep = 330.0;
        private const double ShallowArcSweepThreshold = 45.0;
        private const double MinimumArcSagittaRatio = 0.08;
        private const double OvertraceRatioThreshold = 1.35;
        private const double MinimumCoverageRatio = 0.40;
        private const double TargetCoverageRatio = 0.60;
        private const double DirectionClusterToleranceDegrees = 12.0;
        private const double RightAngleToleranceDegrees = 16.0;
        private const double ParallelToleranceDegrees = 12.0;
        private const double EqualSideRatioThreshold = 1.15;
        private const double OpenCandidateMismatchPenalty = 0.12;
        private const double ClosedCandidateMismatchPenalty = 0.18;
        private const double MinimumClosedConfidence = 0.45;
        private const double MinimumOpenConfidence = 0.45;
        private const double MinimumGapHealingConfidence = 0.52;
        private const double MinimumNearClosedPolygonConfidence = 0.62;
        private const double ArcNearClosedWinDelta = 0.08;
        private const double MinimumNearClosedArcConfidence = 0.40;
        private const double GapHealingMaximumRatio = 0.08;

        public static RecognizedShapeResult? RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                return null;
            }

            NormalizedInkTrace? trace = NormalizeInput(strokes);
            if (trace == null || trace.Points.Count < 4)
            {
                return null;
            }

            CornerDetectionResult corners = DetectCorners(trace);
            List<PrimitiveCandidate> candidates = FitPrimitives(trace, corners);
            if (candidates.Count == 0)
            {
                return null;
            }

            ClosureDisposition closureDisposition = ResolveClosureDisposition(trace, candidates);
            PrimitiveCandidate? bestCandidate = SelectBestCandidate(trace, candidates, closureDisposition);
            return bestCandidate?.ToResult(trace.SourceStrokes, closureDisposition);
        }

        private static NormalizedInkTrace? NormalizeInput(StrokeCollection strokes)
        {
            List<List<Point>> strokePoints = strokes
                .Cast<Stroke>()
                .Select(CollectStrokePoints)
                .Where(points => points.Count >= 2)
                .ToList();

            if (strokePoints.Count == 0)
            {
                return null;
            }

            Rect rawBounds = strokePoints
                .Select(GetBounds)
                .Aggregate(static (left, right) => Rect.Union(left, right));

            double diagonal = GetDiagonal(rawBounds);
            double spacing = Math.Clamp(diagonal / 40.0, MinimumResampleSpacing, MaximumResampleSpacing);
            List<List<Point>> resampledStrokePoints = strokePoints
                .Select(points => ResamplePolyline(points, spacing))
                .Where(points => points.Count >= 2)
                .ToList();

            if (resampledStrokePoints.Count == 0)
            {
                return null;
            }

            int trimmedEndpointPoints = 0;
            if (resampledStrokePoints.Count == 1)
            {
                double initialClosureThreshold = Math.Max(MinimumClosureDistance, diagonal * ClosureDistanceRatio);
                if (GetDistance(resampledStrokePoints[0][0], resampledStrokePoints[0][^1]) > initialClosureThreshold)
                {
                    (resampledStrokePoints[0], trimmedEndpointPoints) = TrimNoisyEndpoints(resampledStrokePoints[0]);
                    if (resampledStrokePoints[0].Count < 2)
                    {
                        return null;
                    }
                }
            }

            List<List<Point>> orientedStrokePoints = OrientStrokePoints(resampledStrokePoints);
            List<Point> flattenedPoints = FlattenStrokePoints(orientedStrokePoints);
            if (flattenedPoints.Count < 4)
            {
                return null;
            }

            Rect bounds = GetBounds(flattenedPoints);
            diagonal = GetDiagonal(bounds);
            double closureThreshold = Math.Max(MinimumClosureDistance, diagonal * ClosureDistanceRatio);
            double endpointSnapRadius = Math.Max(MinimumEndpointSnap, Math.Min(MaximumEndpointSnap, diagonal * EndpointSnapRatio));

            return new NormalizedInkTrace(
                CloneStrokeReferences(strokes),
                orientedStrokePoints,
                flattenedPoints,
                BuildStrokeBoundaries(orientedStrokePoints),
                bounds,
                GetPathLength(flattenedPoints),
                GetDistance(flattenedPoints[0], flattenedPoints[^1]),
                closureThreshold,
                endpointSnapRadius,
                GetDistance(flattenedPoints[0], flattenedPoints[^1]) <= closureThreshold,
                trimmedEndpointPoints);
        }

        private static CornerDetectionResult DetectCorners(NormalizedInkTrace trace)
        {
            List<int> corners = DetectCornersWithStraw(trace.Points);
            corners.AddRange(trace.StrokeBoundaries.Where(boundary => boundary > 0 && boundary < trace.Points.Count - 1));

            corners = corners.Distinct().OrderBy(static index => index).ToList();
            corners = RefineCornerIndices(trace.Points, corners, trace.EndpointSnapRadius);
            return new CornerDetectionResult(
                corners,
                corners.Select(index => trace.Points[index]).ToList(),
                CountStrongCorners(trace.Points, corners));
        }

        private static List<PrimitiveCandidate> FitPrimitives(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            List<PrimitiveCandidate> candidates = [];
            if (trace.StrokePointSets.Count <= 3)
            {
                AddCandidate(candidates, TryFitLine(trace, corners));
                AddCandidate(candidates, TryFitPolyline(trace, corners));
                AddCandidate(candidates, TryFitArc(trace, corners));
            }

            if (trace.IsLikelyClosed)
            {
                AddCandidate(candidates, TryFitCircle(trace, corners));
                AddCandidate(candidates, TryFitEllipse(trace, corners));
            }

            candidates.AddRange(TryFitPolygons(trace, corners));
            return candidates;
        }

        private static void AddCandidate(List<PrimitiveCandidate> candidates, PrimitiveCandidate? candidate)
        {
            if (candidate != null)
            {
                candidates.Add(candidate);
            }
        }

        private static ClosureDisposition ResolveClosureDisposition(NormalizedInkTrace trace, IReadOnlyList<PrimitiveCandidate> candidates)
        {
            if (!trace.IsLikelyClosed)
            {
                return ClosureDisposition.Open;
            }

            PrimitiveCandidate? arcCandidate = candidates
                .Where(static candidate => candidate.Kind == RecognizedShapeKind.Arc)
                .OrderByDescending(static candidate => candidate.Confidence)
                .FirstOrDefault();

            if (arcCandidate != null
                && arcCandidate.SweepAngleDegrees >= ArcNearClosedMinimumSweep
                && arcCandidate.SweepAngleDegrees <= ArcNearClosedMaximumSweep)
            {
                return ClosureDisposition.NearClosed;
            }

            return ClosureDisposition.Closed;
        }

        private static PrimitiveCandidate? SelectBestCandidate(
            NormalizedInkTrace trace,
            List<PrimitiveCandidate> candidates,
            ClosureDisposition closureDisposition)
        {
            return closureDisposition switch
            {
                ClosureDisposition.Open => SelectOpenCandidate(trace, candidates),
                ClosureDisposition.NearClosed => SelectNearClosedCandidate(trace, candidates),
                _ => SelectClosedCandidate(candidates),
            };
        }

        private static PrimitiveCandidate? SelectClosedCandidate(IEnumerable<PrimitiveCandidate> candidates)
        {
            List<PrimitiveCandidate> closedCandidates = candidates
                .Where(static candidate => candidate.Kind is RecognizedShapeKind.Triangle
                    or RecognizedShapeKind.Rectangle
                    or RecognizedShapeKind.Square
                    or RecognizedShapeKind.Diamond
                    or RecognizedShapeKind.Parallelogram
                    or RecognizedShapeKind.Circle
                    or RecognizedShapeKind.Ellipse)
                .Where(static candidate => candidate.Confidence >= MinimumClosedConfidence)
                .OrderByDescending(static candidate => candidate.Confidence)
                .ToList();

            PrimitiveCandidate? bestCandidate = closedCandidates.FirstOrDefault();
            PrimitiveCandidate? bestEllipseCandidate = closedCandidates
                .Where(static candidate => candidate.Kind is RecognizedShapeKind.Circle or RecognizedShapeKind.Ellipse)
                .FirstOrDefault();

            if (bestCandidate != null
                && bestEllipseCandidate != null
                && bestCandidate.Kind is RecognizedShapeKind.Rectangle
                    or RecognizedShapeKind.Square
                    or RecognizedShapeKind.Diamond
                    or RecognizedShapeKind.Parallelogram
                && bestEllipseCandidate.Confidence >= bestCandidate.Confidence - 0.08)
            {
                return bestEllipseCandidate;
            }

            return bestCandidate;
        }

        private static PrimitiveCandidate? SelectNearClosedCandidate(
            NormalizedInkTrace trace,
            IEnumerable<PrimitiveCandidate> candidates)
        {
            List<PrimitiveCandidate> closedCandidates = candidates
                .Where(static candidate => candidate.Kind is RecognizedShapeKind.Triangle
                    or RecognizedShapeKind.Rectangle
                    or RecognizedShapeKind.Square
                    or RecognizedShapeKind.Diamond
                    or RecognizedShapeKind.Parallelogram
                    or RecognizedShapeKind.Circle
                    or RecognizedShapeKind.Ellipse)
                .Where(static candidate => candidate.Confidence >= MinimumOpenConfidence)
                .OrderByDescending(static candidate => candidate.Confidence)
                .ToList();

            PrimitiveCandidate? bestClosedCandidate = closedCandidates.FirstOrDefault();
            if (trace.StrokePointSets.Count > 1)
            {
                return bestClosedCandidate;
            }

            PrimitiveCandidate? arcCandidate = candidates
                .Where(static candidate => candidate.Kind == RecognizedShapeKind.Arc)
                .OrderByDescending(static candidate => candidate.Confidence)
                .FirstOrDefault();

            if (arcCandidate == null)
            {
                return bestClosedCandidate;
            }

            if (arcCandidate.Confidence < MinimumNearClosedArcConfidence)
            {
                return bestClosedCandidate;
            }

            if (bestClosedCandidate == null)
            {
                return arcCandidate;
            }

            bool polygonDominates = bestClosedCandidate.Kind is RecognizedShapeKind.Triangle
                or RecognizedShapeKind.Rectangle
                or RecognizedShapeKind.Square
                or RecognizedShapeKind.Diamond
                or RecognizedShapeKind.Parallelogram;
            if (polygonDominates && bestClosedCandidate.Confidence >= MinimumNearClosedPolygonConfidence)
            {
                return bestClosedCandidate;
            }

            return arcCandidate.Confidence >= bestClosedCandidate.Confidence + ArcNearClosedWinDelta
                ? arcCandidate
                : bestClosedCandidate;
        }

        private static PrimitiveCandidate? SelectOpenCandidate(
            NormalizedInkTrace trace,
            IEnumerable<PrimitiveCandidate> candidates)
        {
            PrimitiveCandidate? gapHealingCandidate = candidates
                .Where(static candidate => candidate.IsGapHealingCandidate)
                .Where(candidate => candidate.Confidence >= MinimumGapHealingConfidence
                    && trace.EndpointDistance <= trace.EndpointSnapRadius
                    && GetGapRatio(trace) <= GapHealingMaximumRatio)
                .OrderByDescending(static candidate => candidate.Confidence)
                .FirstOrDefault();
            if (gapHealingCandidate != null)
            {
                return gapHealingCandidate;
            }

            return candidates
                .Where(static candidate => candidate.Kind is RecognizedShapeKind.Line
                    or RecognizedShapeKind.Polyline
                    or RecognizedShapeKind.Arc)
                .Where(static candidate => candidate.Confidence >= MinimumOpenConfidence)
                .OrderByDescending(static candidate => candidate.Confidence)
                .FirstOrDefault();
        }

        private static PrimitiveCandidate? TryFitLine(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            if (trace.PathLength < MinimumLineLength)
            {
                return null;
            }

            Point start = trace.Points[0];
            Point end = trace.Points[^1];
            double chordLength = GetDistance(start, end);
            if (chordLength <= NearlyEqualDistance)
            {
                return null;
            }

            double chordToPathRatio = chordLength / Math.Max(trace.PathLength, NearlyEqualDistance);
            if (chordToPathRatio < 0.94)
            {
                return null;
            }

            double maxAllowedDeviation = Math.Max(6.0, chordLength * 0.035);
            double maxPerpDeviation = trace.Points.Max(point => GetPerpendicularDistance(point, start, end));
            if (maxPerpDeviation > maxAllowedDeviation)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreateLineScoreCard(trace, chordToPathRatio, maxPerpDeviation, maxAllowedDeviation);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Line,
                scoreCard.Confidence,
                maxPerpDeviation,
                0.0,
                [start, end],
                GetAveragePoint([start, end]),
                chordLength / 2.0,
                0.0,
                NormalizeDegrees(GetAngleDegrees(start, end)),
                1.0,
                false);
        }

        private static PrimitiveCandidate? TryFitPolyline(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            if (trace.IsLikelyClosed)
            {
                return null;
            }

            List<Point> vertices = GetOpenVertices(trace, corners);
            int internalCornerCount = Math.Max(0, vertices.Count - 2);
            if (internalCornerCount == 0 || vertices.Count < 3 || vertices.Count > 4)
            {
                return null;
            }

            double minimumSegmentLength = Math.Max(MinimumOpenSegmentLength, trace.PathLength * 0.08);
            if (vertices.Zip(vertices.Skip(1), GetDistance).Any(length => length < minimumSegmentLength))
            {
                return null;
            }

            double[] segmentResiduals = CalculateOpenSegmentResiduals(trace.Points, vertices);
            if (segmentResiduals.Length == 0 || segmentResiduals.Any(residual => residual > MaximumSegmentRms))
            {
                return null;
            }

            double averageSegmentResidual = segmentResiduals.Average();
            double weakCornerPenalty = CalculateWeakCornerPenalty(vertices);
            if (weakCornerPenalty > 0.35)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreatePolylineScoreCard(trace, averageSegmentResidual, weakCornerPenalty);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Polyline,
                scoreCard.Confidence,
                averageSegmentResidual,
                0.0,
                vertices,
                GetAveragePoint(vertices),
                0.0,
                0.0,
                0.0,
                1.0,
                false);
        }

        private static PrimitiveCandidate? TryFitArc(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            if (trace.StrokePointSets.Count != 1)
            {
                return null;
            }

            if (!TryFitCircleArc(trace.Points, out CircleFit circleFit))
            {
                return null;
            }

            if (circleFit.SweepAngleDegrees < ArcMinimumSweep || circleFit.SweepAngleDegrees > ArcMaximumSweep)
            {
                return null;
            }

            if (corners.StrongCornerCount > 8 || circleFit.Residual > ArcResidualThreshold)
            {
                return null;
            }

            double chordLength = GetDistance(trace.Points[0], trace.Points[^1]);
            if (circleFit.SweepAngleDegrees < ShallowArcSweepThreshold
                && circleFit.Sagitta / Math.Max(chordLength, NearlyEqualDistance) < MinimumArcSagittaRatio)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreateArcScoreCard(trace, corners.StrongCornerCount, circleFit);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Arc,
                scoreCard.Confidence,
                circleFit.Residual,
                circleFit.SweepAngleDegrees,
                [trace.Points[0], trace.Points[^1]],
                circleFit.Center,
                circleFit.Radius,
                circleFit.Radius,
                0.0,
                1.0,
                false,
                circleFit.StartAngleDegrees,
                circleFit.SweepAngleDegrees);
        }

        private static PrimitiveCandidate? TryFitCircle(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            if (Math.Max(trace.Bounds.Width, trace.Bounds.Height) < MinimumEllipseDimension)
            {
                return null;
            }

            if (!TryFitEllipseFamily(trace.Points, out EllipseFit ellipseFit))
            {
                return null;
            }

            if (ellipseFit.AxisRatio < CircleAxisRatioMin
                || ellipseFit.AxisRatio > CircleAxisRatioMax
                || ellipseFit.Residual > CircleResidualThreshold
                || ellipseFit.PathLengthToPerimeterRatio > OvertraceRatioThreshold)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreateEllipseScoreCard(trace, corners.StrongCornerCount, ellipseFit.Residual, ellipseFit.PathLengthToPerimeterRatio);
            double radius = 0.5 * (ellipseFit.MajorRadius + ellipseFit.MinorRadius);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Circle,
                scoreCard.Confidence,
                ellipseFit.Residual,
                360.0,
                Array.Empty<Point>(),
                ellipseFit.Center,
                radius,
                radius,
                ellipseFit.RotationDegrees,
                1.0,
                false);
        }

        private static PrimitiveCandidate? TryFitEllipse(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            if (Math.Max(trace.Bounds.Width, trace.Bounds.Height) < MinimumEllipseDimension)
            {
                return null;
            }

            if (!TryFitEllipseFamily(trace.Points, out EllipseFit ellipseFit))
            {
                return null;
            }

            if (ellipseFit.Residual > EllipseResidualThreshold
                || ellipseFit.PathLengthToPerimeterRatio > OvertraceRatioThreshold)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreateEllipseScoreCard(trace, corners.StrongCornerCount, ellipseFit.Residual, ellipseFit.PathLengthToPerimeterRatio);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Ellipse,
                scoreCard.Confidence,
                ellipseFit.Residual,
                360.0,
                Array.Empty<Point>(),
                ellipseFit.Center,
                ellipseFit.MajorRadius,
                ellipseFit.MinorRadius,
                ellipseFit.RotationDegrees,
                1.0,
                false);
        }

        private static IEnumerable<PrimitiveCandidate> TryFitPolygons(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            List<PrimitiveCandidate> candidates = [];
            bool allowGapHealing = !trace.IsLikelyClosed
                && trace.EndpointDistance <= trace.EndpointSnapRadius
                && GetGapRatio(trace) <= GapHealingMaximumRatio;
            if (!trace.IsLikelyClosed && !allowGapHealing)
            {
                return candidates;
            }

            List<Point> contourPoints = trace.IsLikelyClosed
                ? RemoveClosingDuplicate(trace.Points, trace.EndpointSnapRadius)
                : [.. trace.Points, trace.Points[0]];
            if (contourPoints.Count < 4)
            {
                return candidates;
            }

            List<Point>? vertices = TryExtractLegacyPolygonVertices(contourPoints, trace.Bounds);
            if (vertices?.Count == 3)
            {
                PrimitiveCandidate? triangleCandidate = CreateTriangleCandidate(trace, vertices);
                if (triangleCandidate != null)
                {
                    candidates.Add(triangleCandidate);
                }

                return candidates;
            }

            if (vertices?.Count == 4)
            {
                PrimitiveCandidate? quadrilateralCandidate = CreateQuadrilateralCandidate(trace, vertices, corners.StrongCornerCount, allowGapHealing);
                if (quadrilateralCandidate != null)
                {
                    candidates.Add(quadrilateralCandidate);
                }
            }

            PrimitiveCandidate? rectangleFallback = TryFitRectangleFromBoundingBox(trace, corners.StrongCornerCount, allowGapHealing);
            if (rectangleFallback != null)
            {
                candidates.Add(rectangleFallback);
            }

            return candidates;
        }

        private static PrimitiveCandidate? CreateTriangleCandidate(NormalizedInkTrace trace, IReadOnlyList<Point> vertices)
        {
            Rect bounds = GetBounds(vertices);
            if (Math.Max(bounds.Width, bounds.Height) < MinimumPolygonDimension)
            {
                return null;
            }

            double coverageRatio = CalculateEdgeCoverageRatio(trace.Points, vertices);
            if (coverageRatio < MinimumCoverageRatio)
            {
                return null;
            }

            double meanSegmentRms = CalculateMeanSegmentResidual(trace.Points, vertices, true);
            RecognitionScoreCard scoreCard = CreatePolygonScoreCard(trace, meanSegmentRms, 0.0, coverageRatio);
            return new PrimitiveCandidate(
                RecognizedShapeKind.Triangle,
                scoreCard.Confidence,
                meanSegmentRms,
                0.0,
                vertices.ToList(),
                GetAveragePoint(vertices),
                bounds.Width / 2.0,
                bounds.Height / 2.0,
                0.0,
                coverageRatio,
                false);
        }

        private static PrimitiveCandidate? CreateQuadrilateralCandidate(
            NormalizedInkTrace trace,
            IReadOnlyList<Point> vertices,
            int strongCornerCount,
            bool isGapHealingCandidate)
        {
            if (!isGapHealingCandidate && strongCornerCount < 3)
            {
                return null;
            }

            Rect bounds = GetBounds(vertices);
            if (Math.Max(bounds.Width, bounds.Height) < MinimumPolygonDimension)
            {
                return null;
            }

            double coverageRatio = CalculateEdgeCoverageRatio(trace.Points, vertices);
            if (coverageRatio < MinimumCoverageRatio)
            {
                return null;
            }

            double[] sideLengths = GetClosedSideLengths(vertices);
            double sideRatio = sideLengths.Max() / Math.Max(sideLengths.Min(), NearlyEqualDistance);
            double[] angles = GetInteriorAngles(vertices);
            double meanRightAngleDeviation = angles.Average(angle => Math.Abs(angle - 90.0));
            bool isRectangle = angles.All(angle => Math.Abs(angle - 90.0) <= RightAngleToleranceDegrees);
            bool isSquare = isRectangle && sideRatio <= EqualSideRatioThreshold;
            bool isDiamond = sideRatio <= EqualSideRatioThreshold
                && angles.Count(angle => Math.Abs(angle - 90.0) > RightAngleToleranceDegrees) >= 2;
            bool isParallelogram = HasParallelOppositeSides(vertices);

            RecognizedShapeKind? kind = null;
            double anglePenalty = meanRightAngleDeviation;
            if (isSquare)
            {
                kind = RecognizedShapeKind.Square;
            }
            else if (isRectangle)
            {
                kind = RecognizedShapeKind.Rectangle;
            }
            else if (isDiamond)
            {
                kind = RecognizedShapeKind.Diamond;
                anglePenalty = Math.Max(0.0, RightAngleToleranceDegrees - meanRightAngleDeviation);
            }
            else if (isParallelogram)
            {
                kind = RecognizedShapeKind.Parallelogram;
                anglePenalty = GetParallelAnglePenalty(vertices);
            }

            if (kind == null)
            {
                return null;
            }

            double meanSegmentRms = CalculateMeanSegmentResidual(trace.Points, vertices, true);
            if (meanSegmentRms > 12.0)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreatePolygonScoreCard(trace, meanSegmentRms, anglePenalty, coverageRatio);
            return new PrimitiveCandidate(
                kind.Value,
                scoreCard.Confidence,
                meanSegmentRms,
                0.0,
                vertices.ToList(),
                GetAveragePoint(vertices),
                bounds.Width / 2.0,
                bounds.Height / 2.0,
                NormalizeDegrees(GetAngleDegrees(vertices[0], vertices[1])),
                coverageRatio,
                isGapHealingCandidate);
        }

        private static RecognitionScoreCard CreateLineScoreCard(
            NormalizedInkTrace trace,
            double chordToPathRatio,
            double maxPerpDeviation,
            double maxAllowedDeviation)
        {
            double normalizedOrthogonalError = Clamp01(maxPerpDeviation / Math.Max(maxAllowedDeviation, NearlyEqualDistance));
            double chordRatioPenalty = Clamp01((0.94 - chordToPathRatio) / 0.06);
            double endpointInstability = CalculateEndpointInstability(trace);
            double weightedPenalty = 0.55 * normalizedOrthogonalError
                + 0.25 * chordRatioPenalty
                + 0.20 * endpointInstability;
            return new RecognitionScoreCard(Clamp01(1.0 - weightedPenalty), normalizedOrthogonalError, chordRatioPenalty, endpointInstability);
        }

        private static RecognitionScoreCard CreatePolylineScoreCard(
            NormalizedInkTrace trace,
            double averageSegmentResidual,
            double weakCornerPenalty)
        {
            double averageSegmentFitError = Clamp01(averageSegmentResidual / MaximumSegmentRms);
            double endpointInstability = CalculateEndpointInstability(trace);
            double weightedPenalty = 0.45 * averageSegmentFitError
                + 0.30 * weakCornerPenalty
                + 0.25 * endpointInstability;
            return new RecognitionScoreCard(Clamp01(1.0 - weightedPenalty), averageSegmentFitError, weakCornerPenalty, endpointInstability);
        }

        private static RecognitionScoreCard CreateArcScoreCard(
            NormalizedInkTrace trace,
            int strongCornerCount,
            CircleFit circleFit)
        {
            double arcFitResidual = Clamp01(circleFit.Residual / ArcResidualThreshold);
            double endpointTangencyPenalty = Clamp01(circleFit.EndpointTangencyMismatchDegrees / 30.0);
            double cornerPenalty = Clamp01(strongCornerCount / 3.0);
            double closureMismatchPenalty = trace.IsLikelyClosed ? OpenCandidateMismatchPenalty : 0.0;
            double weightedPenalty = 0.45 * arcFitResidual
                + 0.20 * endpointTangencyPenalty
                + 0.20 * cornerPenalty
                + 0.15 * closureMismatchPenalty;
            return new RecognitionScoreCard(Clamp01(1.0 - weightedPenalty), arcFitResidual, endpointTangencyPenalty, cornerPenalty);
        }

        private static RecognitionScoreCard CreateEllipseScoreCard(
            NormalizedInkTrace trace,
            int strongCornerCount,
            double residual,
            double pathLengthToPerimeterRatio)
        {
            double ellipseResidual = Clamp01(residual / EllipseResidualThreshold);
            double overtracePenalty = Clamp01((pathLengthToPerimeterRatio - 1.0) / 0.35);
            double cornerPenalty = Clamp01(strongCornerCount / 3.0);
            double closureMismatchPenalty = trace.IsLikelyClosed ? 0.0 : ClosedCandidateMismatchPenalty;
            double weightedPenalty = 0.50 * ellipseResidual
                + 0.20 * overtracePenalty
                + 0.15 * cornerPenalty
                + 0.15 * closureMismatchPenalty;
            return new RecognitionScoreCard(Clamp01(1.0 - weightedPenalty), ellipseResidual, overtracePenalty, cornerPenalty);
        }

        private static RecognitionScoreCard CreatePolygonScoreCard(
            NormalizedInkTrace trace,
            double meanSegmentResidual,
            double meanAngleDeviation,
            double coverageRatio)
        {
            double edgeFitResidual = Clamp01(meanSegmentResidual / MaximumSegmentRms);
            double anglePenalty = Clamp01(meanAngleDeviation / RightAngleToleranceDegrees);
            double edgeCoveragePenalty = Clamp01((TargetCoverageRatio - coverageRatio) / (TargetCoverageRatio - MinimumCoverageRatio));
            double closureGapPenalty = Clamp01(GetGapRatio(trace) / GapHealingMaximumRatio);
            double weightedPenalty = 0.40 * edgeFitResidual
                + 0.25 * anglePenalty
                + 0.20 * edgeCoveragePenalty
                + 0.15 * closureGapPenalty;
            return new RecognitionScoreCard(Clamp01(1.0 - weightedPenalty), edgeFitResidual, anglePenalty, edgeCoveragePenalty);
        }

        private static List<Point> CollectStrokePoints(Stroke stroke)
        {
            List<Point> points = [];
            Point? lastPoint = null;
            foreach (StylusPoint stylusPoint in stroke.StylusPoints)
            {
                Point point = new(stylusPoint.X, stylusPoint.Y);
                if (lastPoint == null || GetDistance(lastPoint.Value, point) > NearlyEqualDistance)
                {
                    points.Add(point);
                    lastPoint = point;
                }
            }

            return points;
        }

        private static List<List<Point>> OrientStrokePoints(IReadOnlyList<List<Point>> strokePointSets)
        {
            List<List<Point>> ordered = [];
            if (strokePointSets.Count == 0)
            {
                return ordered;
            }

            ordered.Add(new List<Point>(strokePointSets[0]));
            for (int i = 1; i < strokePointSets.Count; i++)
            {
                List<Point> current = new(strokePointSets[i]);
                Point previousEnd = ordered[^1][^1];
                if (GetDistance(previousEnd, current[^1]) < GetDistance(previousEnd, current[0]))
                {
                    current.Reverse();
                }

                ordered.Add(current);
            }

            return ordered;
        }

        private static List<Point> FlattenStrokePoints(IReadOnlyList<List<Point>> strokePointSets)
        {
            return strokePointSets
                .SelectMany(static strokePoints => strokePoints)
                .Aggregate(new List<Point>(), (points, point) =>
                {
                    if (points.Count == 0 || GetDistance(points[^1], point) > NearlyEqualDistance)
                    {
                        points.Add(point);
                    }

                    return points;
                });
        }

        private static List<int> BuildStrokeBoundaries(IReadOnlyList<List<Point>> strokePointSets)
        {
            List<int> boundaries = [];
            int runningCount = 0;
            for (int i = 0; i < strokePointSets.Count - 1; i++)
            {
                runningCount += strokePointSets[i].Count;
                boundaries.Add(Math.Max(0, runningCount - 1));
            }

            return boundaries;
        }

        private static (List<Point> Points, int TrimmedCount) TrimNoisyEndpoints(IReadOnlyList<Point> points)
        {
            List<Point> trimmed = [.. points];
            int trimmedCount = 0;
            int k = Math.Min(6, trimmed.Count / 4);
            if (k < 2)
            {
                return (trimmed, trimmedCount);
            }

            bool trimmedAny;
            do
            {
                trimmedAny = false;
                if (trimmed.Count <= k * 2 + 2)
                {
                    break;
                }

                if (ShouldTrimEndpoint(trimmed, true, k))
                {
                    trimmed.RemoveAt(0);
                    trimmedCount++;
                    trimmedAny = true;
                }

                if (trimmed.Count <= k * 2 + 2)
                {
                    break;
                }

                if (ShouldTrimEndpoint(trimmed, false, k))
                {
                    trimmed.RemoveAt(trimmed.Count - 1);
                    trimmedCount++;
                    trimmedAny = true;
                }
            }
            while (trimmedAny);

            return (trimmed, trimmedCount);
        }

        private static bool ShouldTrimEndpoint(IReadOnlyList<Point> points, bool fromStart, int k)
        {
            List<Point> segment = fromStart
                ? points.Take(k + 1).ToList()
                : points.Skip(points.Count - (k + 1)).ToList();
            if (!fromStart)
            {
                segment.Reverse();
            }

            double localPathLength = GetPathLength(segment);
            double chordLength = GetDistance(segment[0], segment[^1]);
            if (localPathLength / Math.Max(chordLength, NearlyEqualDistance) >= EndpointPathRatioThreshold)
            {
                return true;
            }

            List<double> directions = [];
            for (int i = 1; i < segment.Count; i++)
            {
                directions.Add(GetAngleDegrees(segment[i - 1], segment[i]));
            }

            double meanDirection = directions.Average();
            double deviation = Math.Sqrt(directions.Average(direction =>
            {
                double delta = NormalizeDegrees(direction - meanDirection);
                return delta * delta;
            }));

            return deviation >= EndpointDirectionDeviationThreshold;
        }

        private static List<int> DetectCornersWithStraw(IReadOnlyList<Point> points)
        {
            List<int> corners = [0];
            if (points.Count <= CornerWindow * 2 + 1)
            {
                corners.Add(points.Count - 1);
                return corners;
            }

            List<double> straws = [];
            for (int i = CornerWindow; i < points.Count - CornerWindow; i++)
            {
                straws.Add(GetDistance(points[i - CornerWindow], points[i + CornerWindow]));
            }

            double threshold = GetMedian(straws) * StrawThresholdRatio;
            for (int i = 0; i < straws.Count; i++)
            {
                if (straws[i] >= threshold)
                {
                    continue;
                }

                bool isLocalMinimum = true;
                for (int j = Math.Max(0, i - 1); j <= Math.Min(straws.Count - 1, i + 1); j++)
                {
                    if (straws[j] < straws[i])
                    {
                        isLocalMinimum = false;
                        break;
                    }
                }

                if (isLocalMinimum)
                {
                    corners.Add(i + CornerWindow);
                }
            }

            corners.Add(points.Count - 1);
            return corners;
        }

        private static List<int> RefineCornerIndices(IReadOnlyList<Point> points, List<int> corners, double endpointSnapRadius)
        {
            List<int> refined = corners.Distinct().OrderBy(static index => index).ToList();
            bool changed;
            do
            {
                changed = false;
                for (int i = 1; i < refined.Count; i++)
                {
                    int start = refined[i - 1];
                    int end = refined[i];
                    if (end - start < 2)
                    {
                        continue;
                    }

                    if (!SegmentFitsLine(points, start, end, MaximumSegmentRms))
                    {
                        int insertedIndex = FindMaximumDeviationIndex(points, start, end);
                        if (insertedIndex > start && insertedIndex < end && !refined.Contains(insertedIndex))
                        {
                            refined.Insert(i, insertedIndex);
                            changed = true;
                            break;
                        }
                    }
                }
            }
            while (changed);

            for (int i = 1; i < refined.Count - 1; i++)
            {
                if (SegmentFitsLine(points, refined[i - 1], refined[i + 1], endpointSnapRadius / 2.0))
                {
                    refined.RemoveAt(i);
                    i--;
                }
            }

            return refined;
        }

        private static int CountStrongCorners(IReadOnlyList<Point> points, IReadOnlyList<int> corners)
        {
            int strongCorners = 0;
            for (int i = 1; i < corners.Count - 1; i++)
            {
                double angle = GetTurnAngle(points[corners[i - 1]], points[corners[i]], points[corners[i + 1]]);
                if (angle <= 158.0)
                {
                    strongCorners++;
                }
            }

            return strongCorners;
        }

        private static List<Point> GetOpenVertices(NormalizedInkTrace trace, CornerDetectionResult corners)
        {
            List<Point> simplified = SimplifyOpenPolyline(trace.Points, Math.Max(8.0, GetDiagonal(trace.Bounds) * 0.015));
            simplified = simplified.OrderBy(point => FindNearestPointIndex(trace.Points, point)).ToList();
            simplified = MergeNearbyVertices(simplified, trace.EndpointSnapRadius / 2.0);
            simplified = RemoveNearlyCollinearVertices(simplified, false);

            if (GetDistance(simplified[0], trace.Points[0]) > trace.EndpointSnapRadius / 2.0)
            {
                simplified.Insert(0, trace.Points[0]);
            }

            if (GetDistance(simplified[^1], trace.Points[^1]) > trace.EndpointSnapRadius / 2.0)
            {
                simplified.Add(trace.Points[^1]);
            }

            return simplified;
        }

        private static List<Point> SimplifyContour(IReadOnlyList<Point> points, Rect bounds)
        {
            double epsilon = Math.Max(8.0, GetDiagonal(bounds) * 0.018);
            List<Point> simplified = SimplifyClosedPolyline(points, epsilon);
            simplified = MergeNearbyVertices(simplified, Math.Max(10.0, GetDiagonal(bounds) * 0.02));
            return RemoveNearlyCollinearVertices(simplified, true);
        }

        private static List<Point>? TryExtractLegacyPolygonVertices(IReadOnlyList<Point> polygonPoints, Rect bounds)
        {
            List<Point> simplified = SimplifyClosedPolyline(polygonPoints, Math.Max(10.0, GetDiagonal(bounds) * 0.02));
            if (simplified.Count < 3)
            {
                return null;
            }

            List<Point> vertices = MergeNearbyVertices(simplified, Math.Max(18.0, GetDiagonal(bounds) * 0.03));
            vertices = RemoveNearlyCollinearVertices(vertices, true);
            return vertices.Count is 3 or 4 ? vertices : null;
        }

        private static List<Point> CollapsePolygonVertices(IReadOnlyList<Point> vertices, NormalizedInkTrace trace)
        {
            List<Point> mergedVertices = MergeNearbyVertices(vertices, trace.EndpointSnapRadius / 2.0);
            mergedVertices = RemoveNearlyCollinearVertices(mergedVertices, true);
            return mergedVertices.Count == 4 ? mergedVertices : vertices.ToList();
        }

        private static PrimitiveCandidate? TryFitRectangleFromBoundingBox(NormalizedInkTrace trace, int strongCornerCount, bool isGapHealingCandidate)
        {
            if (!isGapHealingCandidate && strongCornerCount < 3)
            {
                return null;
            }

            List<Point> polygonPoints = trace.IsLikelyClosed
                ? RemoveClosingDuplicate(trace.Points, trace.EndpointSnapRadius)
                : new List<Point>(trace.Points);
            if (polygonPoints.Count < 8)
            {
                return null;
            }

            Point centroid = GetAveragePoint(polygonPoints);
            ComputeCovariance(polygonPoints, centroid, out double xx, out double xy, out double yy);
            GetPrincipalAxis(xx, xy, yy, out Vector majorAxis, out Vector minorAxis);
            List<(double U, double V)> projections = [];
            double minU = double.PositiveInfinity;
            double maxU = double.NegativeInfinity;
            double minV = double.PositiveInfinity;
            double maxV = double.NegativeInfinity;
            foreach (Point point in polygonPoints)
            {
                Vector offset = point - centroid;
                double u = Vector.Multiply(offset, majorAxis);
                double v = Vector.Multiply(offset, minorAxis);
                projections.Add((u, v));
                minU = Math.Min(minU, u);
                maxU = Math.Max(maxU, u);
                minV = Math.Min(minV, v);
                maxV = Math.Max(maxV, v);
            }

            double width = maxU - minU;
            double height = maxV - minV;
            if (Math.Max(width, height) < MinimumPolygonDimension || Math.Min(width, height) < 24.0)
            {
                return null;
            }

            Point[] vertices =
            [
                ProjectToWorld(centroid, majorAxis, minorAxis, minU, minV),
                ProjectToWorld(centroid, majorAxis, minorAxis, maxU, minV),
                ProjectToWorld(centroid, majorAxis, minorAxis, maxU, maxV),
                ProjectToWorld(centroid, majorAxis, minorAxis, minU, maxV)
            ];
            double coverageRatio = CalculateEdgeCoverageRatio(trace.Points, vertices);
            if (coverageRatio < MinimumCoverageRatio)
            {
                return null;
            }

            double meanSegmentRms = CalculateMeanSegmentResidual(trace.Points, vertices, true);
            if (meanSegmentRms > 12.0)
            {
                return null;
            }

            RecognitionScoreCard scoreCard = CreatePolygonScoreCard(trace, meanSegmentRms, 0.0, coverageRatio);
            return new PrimitiveCandidate(
                Math.Max(width, height) / Math.Max(Math.Min(width, height), NearlyEqualDistance) <= EqualSideRatioThreshold
                    ? RecognizedShapeKind.Square
                    : RecognizedShapeKind.Rectangle,
                scoreCard.Confidence,
                meanSegmentRms,
                0.0,
                vertices,
                centroid,
                width / 2.0,
                height / 2.0,
                NormalizeDegrees(Math.Atan2(majorAxis.Y, majorAxis.X) * 180.0 / Math.PI),
                coverageRatio,
                isGapHealingCandidate);
        }

        private static bool TryFitCircleArc(IReadOnlyList<Point> points, out CircleFit circleFit)
        {
            circleFit = default;
            if (points.Count < 6)
            {
                return false;
            }

            double meanX = points.Average(static point => point.X);
            double meanY = points.Average(static point => point.Y);
            double suu = 0.0;
            double svv = 0.0;
            double suv = 0.0;
            double suuu = 0.0;
            double svvv = 0.0;
            double suvv = 0.0;
            double svuu = 0.0;

            foreach (Point point in points)
            {
                double u = point.X - meanX;
                double v = point.Y - meanY;
                double uu = u * u;
                double vv = v * v;
                suu += uu;
                svv += vv;
                suv += u * v;
                suuu += uu * u;
                svvv += vv * v;
                suvv += u * vv;
                svuu += v * uu;
            }

            double denominator = 2.0 * (suu * svv - suv * suv);
            if (Math.Abs(denominator) <= NearlyEqualDistance)
            {
                return false;
            }

            double uc = (svv * (suuu + suvv) - suv * (svvv + svuu)) / denominator;
            double vc = (suu * (svvv + svuu) - suv * (suuu + suvv)) / denominator;
            Point center = new(meanX + uc, meanY + vc);
            double radius = points.Average(point => GetDistance(point, center));
            if (radius <= NearlyEqualDistance)
            {
                return false;
            }

            List<double> angles = points.Select(point => Math.Atan2(point.Y - center.Y, point.X - center.X) * 180.0 / Math.PI).ToList();
            List<double> unwrapped = [angles[0]];
            for (int i = 1; i < angles.Count; i++)
            {
                double delta = NormalizeDegrees(angles[i] - angles[i - 1]);
                unwrapped.Add(unwrapped[^1] + delta);
            }

            double sweep = Math.Abs(unwrapped[^1] - unwrapped[0]);
            double residual = points.Average(point => Math.Abs(GetDistance(point, center) - radius)) / radius;
            circleFit = new CircleFit(
                center,
                radius,
                residual,
                NormalizeDegrees(unwrapped[0]),
                sweep,
                GetSagitta(points, center, radius),
                CalculateEndpointTangencyMismatch(points, center));
            return true;
        }

        private static bool TryFitEllipseFamily(IReadOnlyList<Point> points, out EllipseFit ellipseFit)
        {
            ellipseFit = default;
            if (points.Count < 6)
            {
                return false;
            }

            Point center = GetAveragePoint(points);
            ComputeCovariance(points, center, out double xx, out double xy, out double yy);
            GetPrincipalAxis(xx, xy, yy, out Vector majorAxis, out Vector minorAxis);

            List<(double U, double V)> projections = [];
            double majorRadius = 0.0;
            double minorRadius = 0.0;
            foreach (Point point in points)
            {
                Vector offset = point - center;
                double u = Vector.Multiply(offset, majorAxis);
                double v = Vector.Multiply(offset, minorAxis);
                projections.Add((u, v));
                majorRadius = Math.Max(majorRadius, Math.Abs(u));
                minorRadius = Math.Max(minorRadius, Math.Abs(v));
            }

            if (majorRadius < minorRadius)
            {
                (majorRadius, minorRadius) = (minorRadius, majorRadius);
                projections = projections.Select(static projection => (projection.V, projection.U)).ToList();
                Vector swappedMajorAxis = minorAxis;
                majorAxis = swappedMajorAxis;
                minorAxis = new Vector(-swappedMajorAxis.Y, swappedMajorAxis.X);
            }

            if (majorRadius <= NearlyEqualDistance || minorRadius <= NearlyEqualDistance)
            {
                return false;
            }

            double residual = CalculateEllipseResidual(projections, majorRadius, minorRadius);
            ellipseFit = new EllipseFit(
                center,
                majorRadius,
                minorRadius,
                residual,
                majorRadius / minorRadius,
                NormalizeDegrees(Math.Atan2(majorAxis.Y, majorAxis.X) * 180.0 / Math.PI),
                GetPathLength(points) / Math.Max(CalculateEllipsePerimeter(majorRadius, minorRadius), NearlyEqualDistance));
            return true;
        }

        private static double[] CalculateOpenSegmentResiduals(IReadOnlyList<Point> points, IReadOnlyList<Point> vertices)
        {
            List<double> residuals = [];
            int startIndex = 0;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                int endIndex = FindNearestPointIndex(points, vertices[i + 1], startIndex);
                if (endIndex <= startIndex)
                {
                    continue;
                }

                residuals.Add(CalculateSegmentResidual(points, startIndex, endIndex, vertices[i], vertices[i + 1]));
                startIndex = endIndex;
            }

            return residuals.ToArray();
        }

        private static double CalculateMeanSegmentResidual(IReadOnlyList<Point> points, IReadOnlyList<Point> vertices, bool isClosed)
        {
            if (vertices.Count < 2)
            {
                return 1.0;
            }

            if (isClosed)
            {
                double total = 0.0;
                foreach (Point point in points)
                {
                    double minimumDistance = double.PositiveInfinity;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        minimumDistance = Math.Min(minimumDistance, GetDistanceToSegment(point, vertices[i], vertices[(i + 1) % vertices.Count]));
                    }

                    total += minimumDistance;
                }

                return total / Math.Max(points.Count, 1);
            }

            List<double> residuals = [];
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                residuals.Add(points.Average(point => GetDistanceToSegment(point, vertices[i], vertices[i + 1])));
            }

            return residuals.Average();
        }

        private static double CalculateEdgeCoverageRatio(IReadOnlyList<Point> points, IReadOnlyList<Point> vertices)
        {
            double tolerance = Math.Max(6.0, GetDiagonal(GetBounds(vertices)) * 0.02);
            int coveredCount = points.Count(point =>
            {
                double minimumDistance = double.PositiveInfinity;
                for (int i = 0; i < vertices.Count; i++)
                {
                    minimumDistance = Math.Min(minimumDistance, GetDistanceToSegment(point, vertices[i], vertices[(i + 1) % vertices.Count]));
                }

                return minimumDistance <= tolerance;
            });

            return coveredCount / (double)Math.Max(points.Count, 1);
        }

        private static double[] GetClosedSideLengths(IReadOnlyList<Point> vertices)
        {
            double[] lengths = new double[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                lengths[i] = GetDistance(vertices[i], vertices[(i + 1) % vertices.Count]);
            }

            return lengths;
        }

        private static double[] GetInteriorAngles(IReadOnlyList<Point> vertices)
        {
            double[] angles = new double[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                angles[i] = GetTurnAngle(vertices[(i - 1 + vertices.Count) % vertices.Count], vertices[i], vertices[(i + 1) % vertices.Count]);
            }

            return angles;
        }

        private static bool HasParallelOppositeSides(IReadOnlyList<Point> vertices)
        {
            return Math.Abs(NormalizeDegrees180(GetOrientationDegrees(vertices[0], vertices[1]) - GetOrientationDegrees(vertices[2], vertices[3]))) <= ParallelToleranceDegrees
                && Math.Abs(NormalizeDegrees180(GetOrientationDegrees(vertices[1], vertices[2]) - GetOrientationDegrees(vertices[3], vertices[0]))) <= ParallelToleranceDegrees;
        }

        private static double GetParallelAnglePenalty(IReadOnlyList<Point> vertices)
        {
            double pairOne = Math.Abs(NormalizeDegrees180(GetOrientationDegrees(vertices[0], vertices[1]) - GetOrientationDegrees(vertices[2], vertices[3])));
            double pairTwo = Math.Abs(NormalizeDegrees180(GetOrientationDegrees(vertices[1], vertices[2]) - GetOrientationDegrees(vertices[3], vertices[0])));
            return (pairOne + pairTwo) / 2.0;
        }

        private static double CalculateWeakCornerPenalty(IReadOnlyList<Point> vertices)
        {
            int internalCornerCount = Math.Max(0, vertices.Count - 2);
            if (internalCornerCount == 0)
            {
                return 0.0;
            }

            int weakCorners = 0;
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                if (GetTurnAngle(vertices[i - 1], vertices[i], vertices[i + 1]) < 22.0)
                {
                    weakCorners++;
                }
            }

            return Clamp01(weakCorners / (double)internalCornerCount);
        }

        private static double CalculateEndpointInstability(NormalizedInkTrace trace)
        {
            if (trace.StrokePointSets.Count != 1)
            {
                return 0.0;
            }

            int k = Math.Min(6, trace.Points.Count / 4);
            return k <= 0 ? 0.0 : Clamp01((double)trace.TrimmedEndpointPoints / (2.0 * k));
        }

        private static double GetGapRatio(NormalizedInkTrace trace)
        {
            return trace.EndpointDistance / Math.Max(trace.PathLength, NearlyEqualDistance);
        }

        private static double CalculateEndpointTangencyMismatch(IReadOnlyList<Point> points, Point center)
        {
            if (points.Count < 4)
            {
                return 0.0;
            }

            Vector startDirection = points[1] - points[0];
            Vector endDirection = points[^1] - points[^2];
            Vector startRadius = points[0] - center;
            Vector endRadius = points[^1] - center;
            return (GetTangencyMismatchDegrees(startDirection, startRadius) + GetTangencyMismatchDegrees(endDirection, endRadius)) / 2.0;
        }

        private static double GetTangencyMismatchDegrees(Vector direction, Vector radius)
        {
            if (direction.Length <= NearlyEqualDistance || radius.Length <= NearlyEqualDistance)
            {
                return 90.0;
            }

            double cosine = Clamp(Math.Abs(Vector.Multiply(direction, radius) / (direction.Length * radius.Length)), 0.0, 1.0);
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double GetSagitta(IReadOnlyList<Point> points, Point center, double radius)
        {
            Point midpoint = new((points[0].X + points[^1].X) / 2.0, (points[0].Y + points[^1].Y) / 2.0);
            return Math.Abs(radius - GetDistance(midpoint, center));
        }

        private static bool SegmentFitsLine(IReadOnlyList<Point> points, int startIndex, int endIndex, double rmsThreshold)
        {
            return endIndex <= startIndex + 1
                || CalculateSegmentResidual(points, startIndex, endIndex, points[startIndex], points[endIndex]) <= rmsThreshold;
        }

        private static double CalculateSegmentResidual(IReadOnlyList<Point> points, int startIndex, int endIndex, Point lineStart, Point lineEnd)
        {
            if (endIndex <= startIndex)
            {
                return 0.0;
            }

            double total = 0.0;
            int count = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                total += Math.Pow(GetPerpendicularDistance(points[i], lineStart, lineEnd), 2);
                count++;
            }

            return Math.Sqrt(total / Math.Max(count, 1));
        }

        private static int FindMaximumDeviationIndex(IReadOnlyList<Point> points, int startIndex, int endIndex)
        {
            int maxIndex = startIndex;
            double maxDistance = double.MinValue;
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                double distance = GetPerpendicularDistance(points[i], points[startIndex], points[endIndex]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        private static int FindNearestPointIndex(IReadOnlyList<Point> points, Point target, int minIndex = 0)
        {
            int nearestIndex = minIndex;
            double nearestDistance = double.PositiveInfinity;
            for (int i = minIndex; i < points.Count; i++)
            {
                double distance = GetDistance(points[i], target);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private static List<Point> ResamplePolyline(IReadOnlyList<Point> points, double spacing)
        {
            if (points.Count == 0)
            {
                return [];
            }

            List<Point> resampled = [points[0]];
            double accumulator = 0.0;
            Point previous = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                Point current = points[i];
                double segmentLength = GetDistance(previous, current);
                if (segmentLength <= NearlyEqualDistance)
                {
                    previous = current;
                    continue;
                }

                while (accumulator + segmentLength >= spacing)
                {
                    double ratio = (spacing - accumulator) / segmentLength;
                    Point interpolated = Interpolate(previous, current, ratio);
                    resampled.Add(interpolated);
                    previous = interpolated;
                    segmentLength = GetDistance(previous, current);
                    accumulator = 0.0;
                }

                accumulator += segmentLength;
                previous = current;
            }

            if (GetDistance(resampled[^1], points[^1]) > NearlyEqualDistance)
            {
                resampled.Add(points[^1]);
            }

            return resampled;
        }

        private static List<Point> RemoveClosingDuplicate(IReadOnlyList<Point> points, double tolerance)
        {
            List<Point> normalized = [.. points];
            if (normalized.Count > 1 && GetDistance(normalized[0], normalized[^1]) <= tolerance)
            {
                normalized.RemoveAt(normalized.Count - 1);
            }

            return normalized;
        }

        private static List<Point> SimplifyOpenPolyline(IReadOnlyList<Point> points, double epsilon)
        {
            return SimplifyDouglasPeucker(points, epsilon);
        }

        private static List<Point> SimplifyClosedPolyline(IReadOnlyList<Point> points, double epsilon)
        {
            List<Point> normalized = RemoveClosingDuplicate(points, epsilon);
            if (normalized.Count < 3)
            {
                return normalized;
            }

            int splitIndex = FindFarthestPointIndex(normalized, normalized[0]);
            if (splitIndex <= 0 || splitIndex >= normalized.Count - 1)
            {
                splitIndex = normalized.Count / 2;
            }

            List<Point> firstHalf = normalized.Take(splitIndex + 1).ToList();
            List<Point> secondHalf = normalized.Skip(splitIndex).ToList();
            secondHalf.Add(normalized[0]);
            List<Point> simplifiedFirst = SimplifyDouglasPeucker(firstHalf, epsilon);
            List<Point> simplifiedSecond = SimplifyDouglasPeucker(secondHalf, epsilon);
            simplifiedFirst.RemoveAt(simplifiedFirst.Count - 1);
            simplifiedSecond.RemoveAt(simplifiedSecond.Count - 1);
            simplifiedFirst.AddRange(simplifiedSecond);
            return simplifiedFirst;
        }

        private static List<Point> SimplifyDouglasPeucker(IReadOnlyList<Point> points, double epsilon)
        {
            if (points.Count < 3)
            {
                return [.. points];
            }

            int index = -1;
            double maxDistance = 0.0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                double distance = GetPerpendicularDistance(points[i], points[0], points[^1]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }

            if (maxDistance <= epsilon || index < 0)
            {
                return [points[0], points[^1]];
            }

            List<Point> left = SimplifyDouglasPeucker(points.Take(index + 1).ToList(), epsilon);
            List<Point> right = SimplifyDouglasPeucker(points.Skip(index).ToList(), epsilon);
            left.RemoveAt(left.Count - 1);
            left.AddRange(right);
            return left;
        }

        private static int FindFarthestPointIndex(IReadOnlyList<Point> points, Point anchor)
        {
            int farthestIndex = 0;
            double maxDistance = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                double distance = GetDistance(points[i], anchor);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestIndex = i;
                }
            }

            return farthestIndex;
        }

        private static List<Point> MergeNearbyVertices(IReadOnlyList<Point> points, double minimumDistance)
        {
            return points.Aggregate(new List<Point>(), (merged, point) =>
            {
                if (merged.Count == 0 || GetDistance(merged[^1], point) > minimumDistance)
                {
                    merged.Add(point);
                }

                return merged;
            });
        }

        private static List<Point> RemoveNearlyCollinearVertices(List<Point> points, bool isClosed)
        {
            if (points.Count < 3)
            {
                return points;
            }

            bool removedPoint;
            do
            {
                removedPoint = false;
                int startIndex = isClosed ? 0 : 1;
                int endIndex = isClosed ? points.Count : points.Count - 1;
                for (int i = startIndex; i < endIndex; i++)
                {
                    int previousIndex = i == 0 ? points.Count - 1 : i - 1;
                    int nextIndex = (i + 1) % points.Count;
                    double angle = GetTurnAngle(points[previousIndex], points[i], points[nextIndex]);
                    if (Math.Abs(180.0 - angle) <= 12.0)
                    {
                        points.RemoveAt(i);
                        removedPoint = true;
                        break;
                    }
                }
            }
            while (removedPoint && points.Count >= (isClosed ? 3 : 2));

            return points;
        }

        private static Rect GetBounds(IReadOnlyList<Point> points)
        {
            double minX = points.Min(static point => point.X);
            double minY = points.Min(static point => point.Y);
            double maxX = points.Max(static point => point.X);
            double maxY = points.Max(static point => point.Y);
            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        private static double GetDiagonal(Rect bounds) => Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height);

        private static double GetPathLength(IReadOnlyList<Point> points)
        {
            double length = 0.0;
            for (int i = 1; i < points.Count; i++)
            {
                length += GetDistance(points[i - 1], points[i]);
            }

            return length;
        }

        private static void ComputeCovariance(IReadOnlyList<Point> points, Point centroid, out double xx, out double xy, out double yy)
        {
            xx = 0.0;
            xy = 0.0;
            yy = 0.0;
            foreach (Point point in points)
            {
                double dx = point.X - centroid.X;
                double dy = point.Y - centroid.Y;
                xx += dx * dx;
                xy += dx * dy;
                yy += dy * dy;
            }

            xx /= points.Count;
            xy /= points.Count;
            yy /= points.Count;
        }

        private static void GetPrincipalAxis(double xx, double xy, double yy, out Vector majorAxis, out Vector minorAxis)
        {
            if (Math.Abs(xy) <= NearlyEqualDistance && Math.Abs(xx - yy) <= NearlyEqualDistance)
            {
                majorAxis = new Vector(1.0, 0.0);
                minorAxis = new Vector(0.0, 1.0);
                return;
            }

            double trace = xx + yy;
            double determinant = xx * yy - xy * xy;
            double root = Math.Sqrt(Math.Max(0.0, trace * trace / 4.0 - determinant));
            double majorEigenValue = trace / 2.0 + root;
            Vector axis = Math.Abs(xy) > NearlyEqualDistance
                ? new Vector(majorEigenValue - yy, xy)
                : xx >= yy ? new Vector(1.0, 0.0) : new Vector(0.0, 1.0);
            axis.Normalize();
            majorAxis = axis;
            minorAxis = new Vector(-axis.Y, axis.X);
        }

        private static double CalculateEllipseResidual(IReadOnlyList<(double U, double V)> projections, double majorRadius, double minorRadius)
        {
            double majorSquared = majorRadius * majorRadius;
            double minorSquared = minorRadius * minorRadius;
            return projections.Average(projection => Math.Abs((projection.U * projection.U) / majorSquared + (projection.V * projection.V) / minorSquared - 1.0));
        }

        private static double CalculateEllipsePerimeter(double majorRadius, double minorRadius)
        {
            double h = Math.Pow(majorRadius - minorRadius, 2) / Math.Pow(majorRadius + minorRadius, 2);
            return Math.PI * (majorRadius + minorRadius) * (1 + (3 * h) / (10 + Math.Sqrt(4 - 3 * h)));
        }

        private static Point GetAveragePoint(IReadOnlyList<Point> points) => new(points.Average(static point => point.X), points.Average(static point => point.Y));

        private static Point Interpolate(Point start, Point end, double ratio) => new(start.X + (end.X - start.X) * ratio, start.Y + (end.Y - start.Y) * ratio);

        private static Point ProjectToWorld(Point centroid, Vector majorAxis, Vector minorAxis, double u, double v)
        {
            return centroid + majorAxis * u + minorAxis * v;
        }

        private static StrokeCollection CloneStrokeReferences(StrokeCollection strokes)
        {
            StrokeCollection clone = new();
            foreach (Stroke stroke in strokes)
            {
                clone.Add(stroke);
            }

            return clone;
        }

        private static double GetDistance(Point first, Point second) => Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));

        private static double GetPerpendicularDistance(Point point, Point lineStart, Point lineEnd)
        {
            if (GetDistance(lineStart, lineEnd) <= NearlyEqualDistance)
            {
                return GetDistance(point, lineStart);
            }

            double numerator = Math.Abs((lineEnd.Y - lineStart.Y) * point.X - (lineEnd.X - lineStart.X) * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
            return numerator / GetDistance(lineStart, lineEnd);
        }

        private static double GetDistanceToSegment(Point point, Point start, Point end)
        {
            Vector segment = end - start;
            if (segment.LengthSquared <= NearlyEqualDistance)
            {
                return GetDistance(point, start);
            }

            double t = Clamp(Vector.Multiply(point - start, segment) / segment.LengthSquared, 0.0, 1.0);
            return GetDistance(point, start + segment * t);
        }

        private static double GetAngleDegrees(Point start, Point end) => Math.Atan2(end.Y - start.Y, end.X - start.X) * 180.0 / Math.PI;

        private static double GetOrientationDegrees(Point start, Point end)
        {
            double angle = GetAngleDegrees(start, end);
            while (angle < 0.0)
            {
                angle += 180.0;
            }

            while (angle >= 180.0)
            {
                angle -= 180.0;
            }

            return angle;
        }

        private static double GetMedian(IReadOnlyList<double> values)
        {
            List<double> ordered = values.OrderBy(static value => value).ToList();
            if (ordered.Count == 0)
            {
                return 0.0;
            }

            int midpoint = ordered.Count / 2;
            return ordered.Count % 2 == 0 ? (ordered[midpoint - 1] + ordered[midpoint]) / 2.0 : ordered[midpoint];
        }

        private static double GetTurnAngle(Point previous, Point current, Point next)
        {
            Vector left = previous - current;
            Vector right = next - current;
            if (left.Length <= NearlyEqualDistance || right.Length <= NearlyEqualDistance)
            {
                return 180.0;
            }

            double cosine = Clamp(Vector.Multiply(left, right) / (left.Length * right.Length), -1.0, 1.0);
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double NormalizeDegrees(double degrees)
        {
            while (degrees <= -180.0)
            {
                degrees += 360.0;
            }

            while (degrees > 180.0)
            {
                degrees -= 360.0;
            }

            return degrees;
        }

        private static double NormalizeDegrees180(double degrees)
        {
            while (degrees <= -90.0)
            {
                degrees += 180.0;
            }

            while (degrees > 90.0)
            {
                degrees -= 180.0;
            }

            return degrees;
        }

        private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        private static double Clamp01(double value) => Clamp(value, 0.0, 1.0);
    }

    public enum RecognizedShapeKind
    {
        Line,
        Polyline,
        Arc,
        Circle,
        Ellipse,
        Triangle,
        Rectangle,
        Square,
        Diamond,
        Parallelogram
    }

    public enum ClosureDisposition
    {
        Open,
        NearClosed,
        Closed
    }

    public sealed class RecognizedShapeResult
    {
        public RecognizedShapeResult(RecognizedShapeKind kind, StrokeCollection sourceStrokes, Point centroid, IReadOnlyList<Point> orderedVertices, double width, double height, double rotationDegrees, double majorRadius, double minorRadius, double startAngleDegrees, double sweepAngleDegrees, ClosureDisposition closureDisposition, double confidence)
        {
            Kind = kind;
            SourceStrokes = sourceStrokes ?? throw new ArgumentNullException(nameof(sourceStrokes));
            Centroid = centroid;
            OrderedVertices = orderedVertices ?? throw new ArgumentNullException(nameof(orderedVertices));
            Width = width;
            Height = height;
            RotationDegrees = rotationDegrees;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
            StartAngleDegrees = startAngleDegrees;
            SweepAngleDegrees = sweepAngleDegrees;
            ClosureDisposition = closureDisposition;
            Confidence = confidence;
        }

        public RecognizedShapeKind Kind { get; }
        public StrokeCollection SourceStrokes { get; }
        public Point Centroid { get; set; }
        public IReadOnlyList<Point> OrderedVertices { get; }
        public double Width { get; }
        public double Height { get; }
        public double RotationDegrees { get; }
        public double MajorRadius { get; }
        public double MinorRadius { get; }
        public double StartAngleDegrees { get; }
        public double SweepAngleDegrees { get; }
        public ClosureDisposition ClosureDisposition { get; }
        public double Confidence { get; }
    }

    public class Circle
    {
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        public Point Centroid { get; set; }
        public double R { get; set; }
        public Stroke Stroke { get; set; }
    }

    internal sealed record NormalizedInkTrace(StrokeCollection SourceStrokes, IReadOnlyList<List<Point>> StrokePointSets, IReadOnlyList<Point> Points, IReadOnlyList<int> StrokeBoundaries, Rect Bounds, double PathLength, double EndpointDistance, double ClosureThreshold, double EndpointSnapRadius, bool IsLikelyClosed, int TrimmedEndpointPoints);
    internal sealed record CornerDetectionResult(IReadOnlyList<int> CornerIndices, IReadOnlyList<Point> CornerPoints, int StrongCornerCount);
    internal sealed record RecognitionScoreCard(double Confidence, double PrimaryPenalty, double SecondaryPenalty, double TertiaryPenalty);
    internal readonly record struct CircleFit(Point Center, double Radius, double Residual, double StartAngleDegrees, double SweepAngleDegrees, double Sagitta, double EndpointTangencyMismatchDegrees);
    internal readonly record struct EllipseFit(Point Center, double MajorRadius, double MinorRadius, double Residual, double AxisRatio, double RotationDegrees, double PathLengthToPerimeterRatio);

    internal sealed class PrimitiveCandidate
    {
        public PrimitiveCandidate(RecognizedShapeKind kind, double confidence, double residual, double sweepAngleDegrees, IReadOnlyList<Point> orderedVertices, Point centroid, double majorRadius, double minorRadius, double rotationDegrees, double coverageRatio, bool isGapHealingCandidate, double startAngleDegrees = 0.0, double? explicitSweepAngleDegrees = null)
        {
            Kind = kind;
            Confidence = confidence;
            Residual = residual;
            SweepAngleDegrees = explicitSweepAngleDegrees ?? sweepAngleDegrees;
            OrderedVertices = orderedVertices;
            Centroid = centroid;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
            RotationDegrees = rotationDegrees;
            CoverageRatio = coverageRatio;
            IsGapHealingCandidate = isGapHealingCandidate;
            StartAngleDegrees = startAngleDegrees;
        }

        public RecognizedShapeKind Kind { get; }
        public double Confidence { get; }
        public double Residual { get; }
        public double SweepAngleDegrees { get; }
        public IReadOnlyList<Point> OrderedVertices { get; }
        public Point Centroid { get; }
        public double MajorRadius { get; }
        public double MinorRadius { get; }
        public double RotationDegrees { get; }
        public double CoverageRatio { get; }
        public bool IsGapHealingCandidate { get; }
        public double StartAngleDegrees { get; }

        public RecognizedShapeResult ToResult(StrokeCollection sourceStrokes, ClosureDisposition closureDisposition)
        {
            double width = Kind switch
            {
                RecognizedShapeKind.Circle => MajorRadius * 2.0,
                RecognizedShapeKind.Ellipse => MajorRadius * 2.0,
                RecognizedShapeKind.Arc => MajorRadius * 2.0,
                RecognizedShapeKind.Line => OrderedVertices.Count >= 2 ? Distance(OrderedVertices[0], OrderedVertices[1]) : 0.0,
                _ => OrderedVertices.Count > 0 ? Bounds(OrderedVertices).Width : 0.0,
            };
            double height = Kind switch
            {
                RecognizedShapeKind.Circle => MinorRadius * 2.0,
                RecognizedShapeKind.Ellipse => MinorRadius * 2.0,
                RecognizedShapeKind.Arc => MinorRadius * 2.0,
                _ => OrderedVertices.Count > 0 ? Bounds(OrderedVertices).Height : 0.0,
            };
            return new RecognizedShapeResult(Kind, sourceStrokes, Centroid, OrderedVertices, width, height, RotationDegrees, MajorRadius, MinorRadius, StartAngleDegrees, SweepAngleDegrees, closureDisposition, Confidence);
        }

        private static Rect Bounds(IReadOnlyList<Point> points)
        {
            double minX = points.Min(static point => point.X);
            double minY = points.Min(static point => point.Y);
            double maxX = points.Max(static point => point.X);
            double maxY = points.Max(static point => point.Y);
            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        private static double Distance(Point first, Point second) => Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));
    }
}
