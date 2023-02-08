using System;
using System.Diagnostics;

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

        public static bool TryGetCharacterAt(this string self, int index, out char character)
        {
            if (self.Length > index)
            {
                character = self[index];
                return true;
            }

            character = default;
            return false;
        }

        // https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
        public static string[] ParseArguments(string commandLine)
        {
            var paramChars = commandLine.ToCharArray();
            var inQuote = false;
            for (var index = 0; index < paramChars.Length; index++)
            {
                if (paramChars[index] == '"')
                {
                    inQuote = !inQuote;
                }

                if (!inQuote && paramChars[index] == ' ')
                {
                    paramChars[index] = '\n';
                }
            }

            return new string(paramChars).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool TryFindType(string typeName, out Type t)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = a.GetType(typeName);
                if (t != null) return true;
            }

            t = default;
            return false;
        }

        public static double FromStopwatchTickToMs(long time)
        {
            return time * 1000.0D / Stopwatch.Frequency;
        }
    }
}