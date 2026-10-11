using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.EmailTemplate
{
    public class SendEmailTemplatRequestDTO
    {
        public string TemplateCode { get; set; }
        public string ToEmail { get; set; }
        public long? InstitutionId { get; set; }
        public Dictionary<string, string> Placeholders { get; set; }
    }
}
