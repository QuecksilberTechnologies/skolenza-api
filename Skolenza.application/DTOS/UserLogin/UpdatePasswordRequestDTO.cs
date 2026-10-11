// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Password.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.UserLogin
{
    /// <summary>
    /// Represents the UpdatePasswordRequestDTO data transfer model.
    /// </summary>
    public class UpdatePasswordRequestDTO
    {      
        public required string LoginId { get; set; }          // User's login ID
        public required string UserStaffMemberId { get; set; }          // User's login ID
        public required string StaffMemberId { get; set; }          // User's login ID     
        public required string NewPassword { get; set; }         // User's password       
        public required string OldPassword { get; set; }         // User's password       
    }
}
