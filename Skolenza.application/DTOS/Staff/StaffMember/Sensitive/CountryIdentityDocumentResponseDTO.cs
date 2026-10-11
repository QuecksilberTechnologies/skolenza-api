using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Sensitive
{
    public class CountryIdentityDocumentResponseDTO
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;

        public string IdentityCategoryName { get; set; } = string.Empty;

        public long IdentityCategoryDocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;

        public bool IsMandatory { get; set; }
    }

}
