using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Common
{
    public class UpdateSingleFieldRequestDTO
    {
         
            public string TableName { get; set; } = string.Empty;
            public long StaffMemberId { get; set; }
            public string FieldName { get; set; } = string.Empty;
            public object? NewValue { get; set; }
            public long UpdatedById { get; set; }
         
    }
}
