using System;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private const double DoubleComparisonTolerance = 0.000001;
        private const float PressureComparisonTolerance = 0.001f;
        private const float DefaultPressureFactor = 0.5f;

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
