using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{
    /// <summary>
    /// post-request to insert operation
    /// </summary>
    public class CreateOperationRequestDTO
    {

        /// <summary>
        /// Gets or sets the legacy product-owner identifier retained for request compatibility.
        /// The authenticated Host user is used for audit attribution.
        /// </summary>

        public required int  ProductOwnerId { get; set; }
      
        public  int ProductOwnerRoleId { get; set; }
        /// <summary> Operation Name Required</summary>

        public required string OperationName { get; set; }  
        /// <summary> Operation Type Required</summary>
        public required int OperationType { get; set; }   
        /// <summary> Operation Remark Required</summary>

        public required string? Remark { get; set; } // Nullable
        public required bool? IsActive { get; set; } = false; 
        public  string? IconImage { get; set; }  
 
    }
}
