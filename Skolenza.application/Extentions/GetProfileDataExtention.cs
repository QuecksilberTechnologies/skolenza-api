using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Extentions
{
    public static class GetProfileDataExtention
    {
              // ------------------ EDUCATION ENTITY EXTENSION ------------------
        public static CompletionSectionDTO CalculateEducationCompletion(this IEnumerable<StaffMemberEducation> items)
        {
            if (items == null || !items.Any())
                return CreateEmptyResponse("Education");

            int totalRows = items.Count();
            double totalPercent = 0;

            foreach (var edu in items)
            {
                int totalFields = 6;
                int filled = 0;

                if (!string.IsNullOrEmpty(edu.Degree)) filled++;
                if (!string.IsNullOrEmpty(edu.InstituteName)) filled++;
                if (edu.StartDate != null) filled++;
                if (edu.EndDate != null) filled++;
                if (edu.HasEducationDocUploded == true) filled++;
                if (edu.ScoreType != null && edu.ScoreType != 0) filled++;


                totalPercent += Math.Round((filled / (double)totalFields) * 100);
            }

            var first = items.First();
            double finalPercent = Math.Round(totalPercent / totalRows);

            return new CompletionSectionDTO
            {
                SectionName = "Education",
                CompletionPercent = finalPercent,
                IsInfoVerified = first.IsInfoVerified,
                IsEditAllowed = first.IsInfoVerified == true ? false : first.IsEditAllowed,
                IsSectionCreate = true
            };
        }

        // ------------------ EDUCATION DTO EXTENSION ------------------
    
        public static CompletionSectionDTO CalculateEducationCompletionDTO(
           this IEnumerable<GetEducationResponseDTO> items)
        {
            var rows = items?.ToArray() ?? Array.Empty<GetEducationResponseDTO>();
            return StaffMemberProfileCompletionCalculator.CreateEducationSection(rows);
        }

        // ------------------ BANK DTO-----------------
        public static CompletionSectionDTO CalculateBankCompletionDTO(
         this IEnumerable<GetBankResponseDTO> items)
        {
            var rows = items?.ToArray() ?? Array.Empty<GetBankResponseDTO>();
            return StaffMemberProfileCompletionCalculator.CreateBankSection(rows);
        }

        #region Contact Extension
        public static CompletionSectionDTO CalculateContactCompletionDTO(
           this IEnumerable<GetContactResponseDTO> items)
        {
            var rows = items?.ToArray() ?? Array.Empty<GetContactResponseDTO>();
            return StaffMemberProfileCompletionCalculator.CreateContactSection(rows);
        }

        #endregion

        public static CompletionSectionDTO CalculateExperienceCompletion(this IEnumerable<dynamic> items)
        {
            if (items == null || !items.Any())
            {
                return new CompletionSectionDTO
                {
                    SectionName = "Experiecne",
                    CompletionPercent = 0,
                    IsInfoVerified = false,
                    IsEditAllowed = false,
                    IsSectionCreate = false
                };
            }

            int totalRows = 0;
            double totalPercent = 0;

            foreach (var item in items)
            {
                dynamic edu = item;   // ⭐⭐ IMPORTANT FIX

                totalRows++;

                int totalFields = 4;
                int filled = 0;

                if (!string.IsNullOrEmpty((string?)edu.Degree)) filled++;
                if (!string.IsNullOrEmpty((string?)edu.InstituteName)) filled++;
                if (edu.StartDate != null) filled++;
                if (edu.EndDate != null) filled++;

                double percent = Math.Round((filled / (double)totalFields) * 100, 0);

                totalPercent += percent;
            }

            dynamic first = items.First();

            double finalPercent = Math.Round(totalPercent / totalRows, 0);

            return new CompletionSectionDTO
            {
                SectionName = "Education",
                CompletionPercent = finalPercent,
                IsInfoVerified = first.IsInfoVerified,
                IsEditAllowed = first.IsEditAllowed,
                IsSectionCreate = true
            };
        }

        // ------------------ COMMON EMPTY FACTORY ------------------
        private static CompletionSectionDTO CreateEmptyResponse(string sectionName)
        {
            return new CompletionSectionDTO
            {
                SectionName = sectionName,
                CompletionPercent = 0,
                IsInfoVerified = false,
                IsEditAllowed = false,
                IsSectionCreate = false
            };
        }
   
    
    }

}





 



