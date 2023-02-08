using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    internal static class MathUtils
    {
        static readonly Random _random = new();

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

        public static int RandomInt(int length)
        {
            var floor = Math.Floor(_random.NextDouble() * length - 0.01f);
            return (int)Clamp(floor, 0, length - 1);
        }

        public static int WeighedRandomIndex(IReadOnlyList<double> weights)
        {
            if (weights.Count == 0)
            {
                throw new InvalidOperationException("no length");
            }

            var weightSum = weights.Sum();
            var c = _random.NextDouble() * weightSum;
            for (var i = 0; i < weights.Count; i++)
            {
                var weight = weights[i];
                if (c < weight)
                {
                    return i;
                }

                c -= weight;
            }

            return 0;
        }
    }
}