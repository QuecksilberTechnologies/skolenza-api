// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for processing Set New Password Link.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.domain.Entity; 
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
namespace skolenza.application.DTOs.UserLogin
{
    /// <summary>
    /// Represents the SetNewPasswordLinkRequestDTO data transfer model.
    /// </summary>
    public class SetNewPasswordLinkRequestDTO 
    {
         
        public string UserLoginId { get; set; } = string.Empty;


    }
}
