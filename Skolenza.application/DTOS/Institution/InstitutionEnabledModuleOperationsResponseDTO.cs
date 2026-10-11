using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class InstitutionEnabledOperationsResponseDTO
    {
       
            long? Id ;
            public long? InstitutionId { get; set; }                  
            public List<EnabledModuleActiveDTO>? Modules { get; set; }
    }

        public class EnabledModuleActiveDTO
        {
            public int? Id { get; set; }
            public int? ParentModuleId { get; set; }
             public string ParentModuleName { get; set; } = string.Empty;         
            public string? ModuleName { get; set; } 
            public bool IsEnabled { get; set; }
            public List<EnabledOperationActiveDTO>? Operations { get; set; }
        
        }

        public class EnabledOperationActiveDTO
        {

            public int Id { get; set; }
            public string? OperationName { get; set; }
             public int? OperationType { get; set; }
             public bool IsEnabled { get; set; }

       }



    }
 
