using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers.Converters
{
    public static class SafeParser
    {
        public static int TryParseInt(object? input, int defaultValue = 0)
        {
            if (input == null) return defaultValue;
            if (int.TryParse(input.ToString(), out int result))
                return result;

            return defaultValue;
        }

        public static long TryParseLong(object? input, long defaultValue = 0L)
        {
            if (input == null) return defaultValue;
            if (long.TryParse(input.ToString(), out long result))
                return result;

            return defaultValue;
        }

        public static decimal TryParseDecimal(object? input, decimal defaultValue = 0m)
        {
            if (input == null) return defaultValue;
            if (decimal.TryParse(input.ToString(), out decimal result))
                return result;

            return defaultValue;
        }

        public static double TryParseDouble(object? input, double defaultValue = 0d)
        {
            if (input == null) return defaultValue;
            if (double.TryParse(input.ToString(), out double result))
                return result;

            return defaultValue;
        }

        public static DateTime TryParseDateTime(object? input, DateTime? defaultValue = null)
        {
            if (input == null) return defaultValue ?? DateTime.MinValue;
            if (DateTime.TryParse(input.ToString(), out DateTime result))
                return result;

            return defaultValue ?? DateTime.MinValue;
        }

        public static bool TryParseBool(object? input, bool defaultValue = false)
        {
            if (input == null) return defaultValue;
            if (bool.TryParse(input.ToString(), out bool result))
                return result;

            return defaultValue;
        }
    }
}
