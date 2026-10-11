// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving All Classification.
// ================================================================

using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using skolenza.application.DTOS.Common;

namespace skolenza.application.DTOS.TicketDTO.Classification
{
    /// <summary>
    /// Represents the GetAllClassificationRequestDTO data transfer model.
    /// </summary>
    public class GetAllClassificationRequestDTO : BaseRequest
    {

  



    }
    /// <summary>
    /// Represents the DDLClassificationRequestDTO data transfer model.
    /// </summary>
    public class DDLClassificationRequestDTO 
    {
    public required bool  IsActive { get; set; } = true;
    }
}
