using System;

namespace Utils.General
{
    internal static class MathUtils
    {
        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        // (0, 1, 0.1) -> 0.1; (0, 10, 0.1) -> 1
        public static double Lerp(double min, double max, double value)
        {
            return (max - min) * value + min;
        }

        // (0, 1, 0.1) -> 0.1; (0, 10, 1) -> 0.1
        public static double InverseLerp(double min, double max, double value)
        {
            return (value - min) / (max - min);
        }

        public static double Remap(double min1, double max1, double min2, double max2, double value1)
        {
            return Lerp(min2, max2, InverseLerp(min1, max1, value1));
        }
    }
}