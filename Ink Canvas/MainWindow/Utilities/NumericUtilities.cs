using System;
using System.Windows;

namespace Ink_Canvas
{
    internal static class NumericComparisonHelper
    {
        private const double DoubleComparisonTolerance = 0.000001;
        private const float PressureComparisonTolerance = 0.001f;

        internal static bool IsNearlyZero(double value) => Math.Abs(value) <= DoubleComparisonTolerance;

        internal static bool IsNearlyEqual(double left, double right) => Math.Abs(left - right) <= DoubleComparisonTolerance;

        internal static bool IsNearlyEqual(float left, float right) => Math.Abs(left - right) <= PressureComparisonTolerance;

        internal static bool AreNearlyEqual(Point left, Point right) =>
            IsNearlyEqual(left.X, right.X) && IsNearlyEqual(left.Y, right.Y);

        internal static float ToPressureFactor(double value) => (float)Math.Clamp(value, 0.0, 1.0);
    }

    public partial class MainWindow : Window
    {
        private const float DefaultPressureFactor = 0.5f;

        private static bool IsNearlyZero(double value) => NumericComparisonHelper.IsNearlyZero(value);

        private static bool IsNearlyEqual(double left, double right) => NumericComparisonHelper.IsNearlyEqual(left, right);

        private static bool IsNearlyEqual(float left, float right) => NumericComparisonHelper.IsNearlyEqual(left, right);

        private static bool AreNearlyEqual(Point left, Point right) => NumericComparisonHelper.AreNearlyEqual(left, right);

        private static float ToPressureFactor(double value) => NumericComparisonHelper.ToPressureFactor(value);
    }
}
