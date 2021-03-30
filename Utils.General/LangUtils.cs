using System;

namespace Utils.General
{
    internal static class LangUtils
    {
        public static TimeSpan Seconds(this int self) => TimeSpan.FromSeconds(self);
        public static TimeSpan Seconds(this double self) => TimeSpan.FromSeconds(self);

        public static string OrNull(this string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }

        public static string OrderToString(int order)
        {
            switch (order % 10)
            {
                case 1: return $"{order}st";
                case 2: return $"{order}nd";
                case 3: return $"{order}rd";
                default: return $"{order}th";
            }
        }
    }
}