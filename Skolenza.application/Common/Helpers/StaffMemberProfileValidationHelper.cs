using skolenza.application.Exceptions;
using System.Text.RegularExpressions;

namespace skolenza.application.Common.Helpers;

/// <summary>
/// Provides validation rules shared by StaffMember profile create and update handlers.
/// </summary>
public static class StaffMemberProfileValidationHelper
{
    /// <summary>
    /// Preserves legacy ten-digit numbers and accepts the international format sent by Contact UI.
    /// Country-specific national-number validation remains with the selected-country control.
    /// </summary>
    public static void ValidateContactNumber(string value, string fieldName)
    {
        if (!Regex.IsMatch(value, @"^(?:[0-9]{10}|\+[1-9][0-9]{6,14})$"))
        {
            throw new ValidationErrorException($"Invalid {fieldName}.");
        }
    }

    public static void RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationErrorException($"{fieldName} is required.");
    }

    public static void ValidateDateRange(
        DateTime? startDate,
        DateTime? endDate,
        string startFieldName = "Start date",
        string endFieldName = "End date")
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
        {
            throw new ValidationErrorException(
                $"{endFieldName} cannot be before {startFieldName.ToLowerInvariant()}.");
        }
    }

    public static void ValidateDateOfBirth(DateTime? dateOfBirth)
    {
        if (dateOfBirth.HasValue && dateOfBirth.Value.Date > DateTime.UtcNow.Date)
            throw new ValidationErrorException("Date of birth cannot be in the future.");
    }

    public static void ValidateGap(
        bool hasGap,
        double gapYears,
        string? reason,
        string gapLabel)
    {
        if (!hasGap)
            return;

        if (gapYears <= 0)
            throw new ValidationErrorException($"{gapLabel} years must be greater than zero.");

        RequireText(reason, $"{gapLabel} reason");
    }
}
