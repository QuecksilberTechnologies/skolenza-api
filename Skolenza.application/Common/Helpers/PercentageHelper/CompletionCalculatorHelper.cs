using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.domain.Entity;
using Newtonsoft.Json.Linq;

namespace skolenza.application.Common.Helpers.PercentageHelper
{
    public static class CompletionCalculatorHelper
    {
        // =========================================================
        // STAFF_MEMBER BASIC COMPLETION
        // =========================================================
        public static double StaffMemberPropCalculate(StaffMember emp, bool hasPrimaryImage)
        {
            if (emp == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateOverviewRow(emp, hasPrimaryImage);
        }
        // =========================================================
        // Experience Prop Calculate  
        // =========================================================
        public static double ExperiencePropCalculate(GetStaffMemberExperienceResponseDTO exp)
        {
            if (exp == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateExperienceRow(exp);
        }

        // =========================================================
        // BANK DETAILS COMPLETION
        // =========================================================
        public static double BankPropCalculate_NoPrimary(GetBankResponseDTO record )
        {

            if (record == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateBankRow(record);
        }
        public static double BankPropCalculate(GetBankResponseDTO record)
        {

            if (record == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateBankRow(record);
        }

        // =========================================================
        // EDUCATION COMPLETION ✅ FIXED
        // =========================================================
        public static double EduPropCalculate(GetEducationResponseDTO edu)
        {
            if (edu == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateEducationRow(edu);
        }
        // =========================================================
        // DEPENDENT DETAILS COMPLETION
        // =========================================================
        public static double DependentPropCalculate(GetDependentResponseDTO dep)
        {
            if (dep == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateDependentRow(dep);
        }

        // =========================================================
        // CONTACT DETAILS COMPLETION
        // =========================================================
        public static double ContactPropCalculate_NoPrimary(GetContactResponseDTO contact)
        {
            if (contact == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateContactRow(contact);
        }
        public static double ContactPropCalculate(GetContactResponseDTO contact)
        {
            if (contact == null)
                return 0;

            return StaffMemberProfileCompletionCalculator.CalculateContactRow(contact);
        }

        // =========================================================
        // COMMON HELPERS
        // =========================================================
        private static double CalculatePercentage(int[] checks)
        {
            int completed = checks.Sum();
            int total = checks.Length;

            return total == 0
                ? 0
                : Math.Round((completed / (double)total) * 100, 0);
        }

        private static int IsFilled(string? value)
            => string.IsNullOrWhiteSpace(value) ? 0 : 1;
    }
}
