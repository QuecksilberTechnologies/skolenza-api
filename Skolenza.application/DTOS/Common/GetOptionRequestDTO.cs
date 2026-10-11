// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Option.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
   /// <summary>
   /// Represents the GetOptionRequestDTO data transfer model.
   /// </summary>
   public class GetOptionRequestDTO
    {
        public string? UserStaffMemberId { get; set; }  
        public  DateTime? TodaysDate { get; set; }        
      
    }
}
