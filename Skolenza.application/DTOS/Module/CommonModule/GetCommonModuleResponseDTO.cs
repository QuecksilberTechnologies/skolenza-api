using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Module.CommonModule
{
    /// <summary>
    /// Represents the request model used to create a new parent module in the system.
    /// </summary>
    /// <remarks>
    /// This API allows the creation of a new parent module by providing essential details like
    /// staffMember, institution, and role identifiers, along with display configurations for both web and mobile UI.
    /// </remarks>
    public class GetCommonModuleResponseDTO
    {       
     
        public  int Id { get; set; } 
        public string? ModuleName { get; set; } 
        public string? DisplayName { get; set; }
        public bool IsModuleDisplayInUI { get; set; } 
        public bool IsActive { get; set; }
        public string? ImageIconWeb { get; set; }
        public string? ImageIconMobile { get; set; }
        public int? ItemPriority { get; set; }
        public string? Remark { get; set; }
        public long? AddedById { get; set; }
        public DateTime? AddedDateTime { get; set; }
        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }


}
