using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.domain.Entity;

namespace skolenza.application.Common.Helpers.PercentageHelper;

/// <summary>
/// Provides the common aggregation rules used by StaffMember profile completion sections.
/// Verification is deliberately metadata and never contributes to completion percentage.
/// </summary>
public static class StaffMemberProfileCompletionCalculator
{
    public static double CalculateOverallCompletion(
        IReadOnlyCollection<CompletionSectionDTO> sections) =>
        sections.Count == 0
            ? 0
            : Math.Round(sections.Average(section => section.CompletionPercent ?? 0), 0);

    public static double CalculateRowPercentage(params bool[] requiredFields)
    {
        if (requiredFields.Length == 0)
            return 0;

        return Math.Round(
            requiredFields.Count(isComplete => isComplete) * 100d / requiredFields.Length,
            0);
    }

    public static double CalculateOverviewRow(StaffMember staffMember, bool hasPrimaryImage, bool hasPosition = false)
    {
        ArgumentNullException.ThrowIfNull(staffMember);

        return CalculateOverviewRow(
            staffMember.FirstName,
            staffMember.LastName,
            staffMember.DateOfBirth,
            staffMember.DateOfOnBoarding,
            staffMember.DepartmentId,
            staffMember.OfficialEmail,
            hasPrimaryImage,
            hasPosition);
    }

    public static double CalculateOverviewRow(
        string? firstName,
        string? lastName,
        DateTime? dateOfBirth,
        DateTime? dateOfOnboarding,
        int? departmentId,
        string? officialEmail,
        bool hasPrimaryImage,
        bool hasPosition = false) =>
        CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(firstName),
            !string.IsNullOrWhiteSpace(lastName),
            dateOfBirth.HasValue,
            dateOfOnboarding.HasValue,
            hasPosition,
            departmentId > 0,
            !string.IsNullOrWhiteSpace(officialEmail),
            hasPrimaryImage);

    public static double CalculateBankRow(GetBankResponseDTO bank)
    {
        ArgumentNullException.ThrowIfNull(bank);

        var documentComplete = !bank.IsPrimaryAccount ||
            (bank.HasChequeDocUploaded &&
             !string.IsNullOrWhiteSpace(bank.FilePath) &&
             !string.IsNullOrWhiteSpace(bank.FileName));

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(bank.BankName),
            !string.IsNullOrWhiteSpace(bank.BranchName),
            !string.IsNullOrWhiteSpace(bank.IFSCCode),
            !string.IsNullOrWhiteSpace(bank.AccountNumber),
            !string.IsNullOrWhiteSpace(bank.AccountType),
            documentComplete);
    }

    public static double CalculateContactRow(GetContactResponseDTO contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(contact.ContactName),
            !string.IsNullOrWhiteSpace(contact.ContactNumber),
            contact.Relation > 0);
    }

    public static double CalculateExperienceRow(GetStaffMemberExperienceResponseDTO experience)
    {
        ArgumentNullException.ThrowIfNull(experience);

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(experience.CompanyName),
            !string.IsNullOrWhiteSpace(experience.PreviousPositionTitle),
            experience.StartDate.HasValue,
            experience.EndDate.HasValue);
    }

    public static double CalculateIdentityRow(
        string? identityValue,
        bool hasIdentityUploaded,
        bool isMandatory,
        bool hasSavedRecord)
    {
        if (!isMandatory && !hasSavedRecord)
            return 0;

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(identityValue),
            hasIdentityUploaded);
    }

    public static double CalculateInsuranceRow(
        int policyTypeId,
        int insurancePolicyId,
        DateTime? startDate,
        DateTime? endDate) =>
        CalculateRowPercentage(
            policyTypeId > 0,
            insurancePolicyId > 0,
            startDate.HasValue,
            endDate.HasValue);

    public static double CalculateEducationRow(GetEducationResponseDTO education)
    {
        ArgumentNullException.ThrowIfNull(education);

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(education.Degree),
            !string.IsNullOrWhiteSpace(education.InstituteName),
            education.StartDate.HasValue,
            education.EndDate.HasValue,
            education.HasEducationDocUploded,
            !string.IsNullOrWhiteSpace(education.ScoreType));
    }

    public static double CalculateDependentRow(GetDependentResponseDTO dependent)
    {
        ArgumentNullException.ThrowIfNull(dependent);

        return CalculateRowPercentage(
            !string.IsNullOrWhiteSpace(dependent.DependentName),
            dependent.Relation > 0,
            dependent.DateOfBirth.HasValue,
            dependent.HasProofUploaded);
    }

    public static CompletionSectionDTO CreateBankSection(IReadOnlyCollection<GetBankResponseDTO> rows)
    {
        var section = CreateSection(
            "Bank",
            rows.Select(CalculateBankRow).ToArray(),
            rows.Select(row => row.IsInfoVerified).ToArray(),
            rows.Select(row => row.IsEditAllowed).ToArray());

        if (rows.Count > 0 && rows.All(row => !row.IsPrimaryAccount))
            section.CompletionPercent = 0;

        return section;
    }

    public static CompletionSectionDTO CreateContactSection(IReadOnlyCollection<GetContactResponseDTO> rows)
    {
        var section = CreateSection(
            "Contact",
            rows.Select(CalculateContactRow).ToArray(),
            rows.Select(row => row.IsInfoVerified).ToArray(),
            rows.Select(row => row.IsEditAllowed).ToArray());

        if (rows.Count > 0 && rows.All(row => !row.IsPrimary))
            section.CompletionPercent = Math.Min(section.CompletionPercent ?? 0, 99);

        return section;
    }

    public static CompletionSectionDTO CreateEducationSection(IReadOnlyCollection<GetEducationResponseDTO> rows) =>
        CreateSection(
            "Education",
            rows.Select(CalculateEducationRow).ToArray(),
            rows.Select(row => row.IsInfoVerified).ToArray(),
            rows.Select(row => row.IsEditAllowed).ToArray());

    public static CompletionSectionDTO CreateIdentitySection(
        IReadOnlyCollection<double> applicableRowPercentages,
        IReadOnlyCollection<bool?> verificationStates,
        IReadOnlyCollection<bool?> editStates) =>
        CreateSection(
            "Identity",
            applicableRowPercentages,
            verificationStates,
            editStates);

    public static CompletionSectionDTO CreateSection(
        string sectionName,
        IReadOnlyCollection<double> rowPercentages,
        IReadOnlyCollection<bool?>? verificationStates = null,
        IReadOnlyCollection<bool?>? editStates = null)
    {
        bool hasRows = rowPercentages.Count > 0;

        return new CompletionSectionDTO
        {
            SectionName = sectionName,
            CompletionPercent = hasRows ? Math.Round(rowPercentages.Average(), 0) : 0,
            IsInfoVerified = hasRows && verificationStates?.Count > 0
                ? verificationStates.All(value => value == true)
                : false,
            IsEditAllowed = hasRows && verificationStates?.All(value => value == true) == true
                ? false
                : editStates?.Any(value => value == true) == true,
            IsSectionCreate = hasRows
        };
    }

    public static CompletionSectionDTO CreateAssignmentSection(string sectionName, bool exists)
    {
        return CreateSection(sectionName, exists ? new[] { 100d } : Array.Empty<double>());
    }

    /// <summary>
    /// Adds the server-owned update-bulk identifier only to sections whose entities
    /// persist IsInfoVerified and IsEditAllowed. Assignment summary sections are read-only.
    /// </summary>
    public static List<CompletionSectionDTO> ApplyVerificationContract(
        List<CompletionSectionDTO> sections)
    {
        foreach (var section in sections)
        {
            section.TabInfoType = section.SectionName switch
            {
                "Overview" => (int)TabInfoType.StaffMember,
                "Bank" => (int)TabInfoType.Bank,
                "Contact" => (int)TabInfoType.Contact,
                "Experience" => (int)TabInfoType.Experience,
                "Identity" => (int)TabInfoType.Identity,
                "Education" => (int)TabInfoType.Education,
                "Dependent" => (int)TabInfoType.Dependent,
                _ => null
            };
            section.CanUpdateVerificationStatus = section.TabInfoType.HasValue;
        }

        return sections;
    }
}
