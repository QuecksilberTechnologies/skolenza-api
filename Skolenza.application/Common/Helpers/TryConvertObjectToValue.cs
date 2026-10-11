using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class TryConvertObjectToValue
    {
        public static bool TryConvertValue(object? input, Type targetType, out object? result)
        {
            try
            {
                result = null;
                if (input == null || (targetType == typeof(string) && string.IsNullOrWhiteSpace(input.ToString())))
                    return true;

                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                var inputStr = input.ToString();

                if (string.IsNullOrWhiteSpace(inputStr))
                {
                    result = null;
                    return true;
                }

                if (underlyingType == typeof(int))
                {
                    if (int.TryParse(inputStr, out int val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(long))
                {
                    if (long.TryParse(inputStr, out long val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(float))
                {
                    if (float.TryParse(inputStr, out float val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(double))
                {
                    if (double.TryParse(inputStr, out double val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(decimal))
                {
                    if (decimal.TryParse(inputStr, out decimal val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(bool))
                {
                    if (bool.TryParse(inputStr, out bool val))
                    {
                        result = val;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(DateTime))
                {
                    var formats = new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-ddTHH:mm:ss" };
                    if (DateTime.TryParseExact(inputStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        result = dt;
                        return true;
                    }

                    if (DateTime.TryParse(inputStr, out dt)) // fallback
                    {
                        result = dt;
                        return true;
                    }

                    return false;
                }

                if (underlyingType == typeof(DateOnly))
                {
                    if (DateOnly.TryParse(inputStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dOnly))
                    {
                        result = dOnly;
                        return true;
                    }
                    return false;
                }

                if (underlyingType == typeof(Guid))
                {
                    if (Guid.TryParse(inputStr, out var guid))
                    {
                        result = guid;
                        return true;
                    }
                    return false;
                }

                if (underlyingType.IsEnum)
                {
                    if (Enum.TryParse(underlyingType, inputStr, true, out object? enumVal))
                    {
                        result = enumVal;
                        return true;
                    }
                    return false;
                }

                // Last fallback
                result = Convert.ChangeType(inputStr, underlyingType);
                return true;
            }
            catch (Exception ex)
            {
                result = null;
                return false;
            }
        }



    }

}
