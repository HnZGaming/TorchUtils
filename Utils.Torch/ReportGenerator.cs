using System;
using Utils.General;

namespace Utils.Torch
{
    internal static class ReportGenerator
    {
        static readonly Random _numberGenerator = new Random();

        public static string Log(object self, Exception e)
        {
            var errorId = $"{_numberGenerator.Next(0, 999999):000000}";
            self.GetFullNameLogger().Error(e, errorId);
            return errorId;
        }
    }
}