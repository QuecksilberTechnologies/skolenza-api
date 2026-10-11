using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using skolenza.application.DTOS.Institution;
using skolenza.domain.Entity;

namespace skolenza.application.Common.Helpers;

/// <summary>
/// Formats and recognizes staffMember codes using the existing institution pattern fields.
/// Callers supply the original joining date; this helper never substitutes today's date.
/// </summary>
public static class StaffMemberCodePatternFormatter
{
    #region Pattern Formatting

    /// <summary>
    /// Builds a code using the established prefix/year/month/department/sequence order.
    /// </summary>
    public static string Format(
        StaffMemberCodePattern pattern,
        DateTime joiningDate,
        int? departmentId,
        int runningNumber)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (runningNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(runningNumber));
        }

        if (pattern.RunningNumberLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pattern.RunningNumberLength));
        }

        if ((pattern.IncludeYear || pattern.IncludeMonth) && joiningDate == default)
        {
            throw new ArgumentException("Original joining date is required.", nameof(joiningDate));
        }

        if (pattern.IncludeDepartment && departmentId is not > 0)
        {
            throw new ArgumentException("Department is required by this pattern.", nameof(departmentId));
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(pattern.Prefix))
        {
            parts.Add(pattern.Prefix);
        }

        if (pattern.IncludeYear)
        {
            parts.Add(joiningDate.Year.ToString(CultureInfo.InvariantCulture));
        }

        if (pattern.IncludeMonth)
        {
            parts.Add(joiningDate.ToString("MMM", CultureInfo.InvariantCulture).ToUpperInvariant());
        }

        if (pattern.IncludeDepartment)
        {
            parts.Add(departmentId!.Value.ToString(CultureInfo.InvariantCulture));
        }

        parts.Add(runningNumber.ToString(CultureInfo.InvariantCulture)
            .PadLeft(pattern.RunningNumberLength, '0'));

        return string.Join(pattern.Separator, parts);
    }

    #endregion

    #region Existing Code Recognition

    /// <summary>
    /// Recognizes a complete matching code without splitting on a potentially empty
    /// separator or treating digits in the prefix/year as part of the running number.
    /// </summary>
    public static bool TryGetRunningNumber(
        StaffMemberCodePattern pattern,
        DateTime joiningDate,
        int? departmentId,
        string? code,
        out int runningNumber)
    {
        runningNumber = 0;
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var sample = Format(pattern, joiningDate, departmentId, 1);
        var prefixLength = sample.Length - pattern.RunningNumberLength;
        if (code.Length < prefixLength + pattern.RunningNumberLength ||
            !code.StartsWith(sample[..prefixLength], StringComparison.Ordinal))
        {
            return false;
        }

        var digits = code[prefixLength..];
        if (digits.Any(character => character is < '0' or > '9') ||
            !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed <= 0 ||
            !string.Equals(code, Format(pattern, joiningDate, departmentId, parsed), StringComparison.Ordinal))
        {
            return false;
        }

        runningNumber = parsed;
        return true;
    }

    /// <summary>
    /// Returns the next number after the highest preserved sequence, with overflow protection.
    /// </summary>
    public static int NextRunningNumber(IEnumerable<int> preservedNumbers)
    {
        ArgumentNullException.ThrowIfNull(preservedNumbers);
        var numbers = preservedNumbers.ToArray();
        if (numbers.Any(number => number <= 0))
        {
            throw new ArgumentException("Preserved running numbers must be positive.", nameof(preservedNumbers));
        }

        return checked(numbers.DefaultIfEmpty(0).Max() + 1);
    }

    #endregion

    #region Existing StaffMember Change Preview

    /// <summary>
    /// Preserves recognized sequences when changing a pattern. Unrecognized codes,
    /// missing dates and collisions block the entire change instead of being guessed.
    /// </summary>
    public static SaveStaffMemberCodePatternResponseDTO PreviewChanges(
        long institutionId,
        StaffMemberCodePattern? current,
        StaffMemberCodePattern proposed,
        IReadOnlyList<StaffMember> staffMembers)
    {
        var result = new SaveStaffMemberCodePatternResponseDTO();
        foreach (var staffMember in staffMembers.OrderBy(item => item.Id))
        {
            var row = new StaffMemberCodeChangeDTO
            {
                StaffMemberId = staffMember.Id,
                Name = string.Join(" ", new[] { staffMember.FirstName, staffMember.MiddleName, staffMember.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                CurrentCode = staffMember.EmployementCode,
                JoiningDate = staffMember.DateOfOnBoarding
            };
            result.StaffMembers.Add(row);
            try
            {
                if (staffMember.InstitutionId != institutionId)
                {
                    throw new ArgumentException("StaffMember does not belong to this institution.");
                }

                var date = staffMember.DateOfOnBoarding ?? default;
                var matchesNew = TryGetRunningNumber(proposed, date, staffMember.DepartmentId,
                    staffMember.EmployementCode, out var number);
                if (!matchesNew && (current is null || !TryGetRunningNumber(current, date,
                        staffMember.DepartmentId, staffMember.EmployementCode, out number)))
                {
                    // Legacy onboarding generated the code using account creation time.
                    // Recognize only that exact persisted date; the new code still uses joining date.
                    if (current is null || staffMember.AddedDateTime == default ||
                        !TryGetRunningNumber(current, staffMember.AddedDateTime,
                            staffMember.DepartmentId, staffMember.EmployementCode, out number))
                    {
                        throw new ArgumentException("Existing code does not match the current or selected pattern. Correct it before confirming.");
                    }
                }

                row.ProposedCode = matchesNew
                    ? staffMember.EmployementCode
                    : Format(proposed, date, staffMember.DepartmentId, number);
                if (row.ProposedCode!.Length > 50)
                {
                    throw new ArgumentException("StaffMember code exceeds the existing 50-character field limit.");
                }

                result.LastUsedNumber = Math.Max(result.LastUsedNumber, number);
            }
            catch (ArgumentException exception)
            {
                row.Errors.Add(exception.Message);
            }
        }

        foreach (var duplicate in result.StaffMembers.Where(row => row.ProposedCode is not null)
                     .GroupBy(row => row.ProposedCode, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            foreach (var row in duplicate)
            {
                row.Errors.Add("Proposed staffMember code conflicts with another staffMember.");
            }
        }

        if (result.StaffMembers.Any(row => row.Errors.Count > 0))
        {
            result.Errors.Add("Resolve staffMember code/date errors before confirming the pattern change.");
        }

        // Do not recycle an already issued number when the last staffMember was removed.
        result.LastUsedNumber = Math.Max(result.LastUsedNumber, current?.LastUsedNumber ?? 0);
        var snapshot = JsonSerializer.Serialize(new
        {
            institutionId,
            currentPattern = current is null ? null : new
            {
                current.Id, current.Prefix, current.Separator, current.IncludeYear,
                current.IncludeMonth, current.IncludeDepartment, current.RunningNumberLength,
                current.LastUsedNumber, current.UpdatedDateTime
            },
            proposedPattern = new
            {
                proposed.Prefix, proposed.Separator, proposed.IncludeYear,
                proposed.IncludeMonth, proposed.IncludeDepartment, proposed.RunningNumberLength
            },
            staffMembers = staffMembers.OrderBy(staffMember => staffMember.Id).Select(staffMember => new
            {
                staffMember.Id, staffMember.InstitutionId, staffMember.EmployementCode,
                staffMember.DateOfOnBoarding, staffMember.AddedDateTime, staffMember.DepartmentId, staffMember.UpdatedDateTime
            })
        });
        result.PreviewHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        return result;
    }

    #endregion
}
