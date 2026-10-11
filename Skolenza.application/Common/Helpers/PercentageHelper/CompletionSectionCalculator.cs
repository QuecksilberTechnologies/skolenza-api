using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Education;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers.PercentageHelper
{
    public static class CompletionSectionCalculator
    {
        // Calculates SECTION completion (average of rows)
        public static CompletionSectionDTO CalculateBankSection(
            IEnumerable<GetBankResponseDTO> records)
        {
            if (records == null || !records.Any())
                return new CompletionSectionDTO
                {
                    SectionName = "Bank",
                    CompletionPercent = 0,
                    IsSectionCreate = false
                };

            // Row-level % reused (NO DUPLICATION)
            double average = records
                .Average(r => CompletionCalculatorHelper.BankPropCalculate(r));

            var first = records.First();

            return new CompletionSectionDTO
            {
                SectionName = "Bank",
                CompletionPercent = Math.Round(average, 2),
                IsInfoVerified = first.IsInfoVerified,
                IsEditAllowed = first.IsEditAllowed,
                IsSectionCreate = true
            };
        }
    }
}
