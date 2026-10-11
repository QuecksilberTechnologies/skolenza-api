// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution-enabled operations from module operations.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Features.OperationCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to update institution-enabled operations from module operations.
    /// </summary>
    public class UpdateInstitutionEnabledOperationFromModuleOperationCommand : IRequest<ApiResponse<UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO>>
    {
        public UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO dto { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInstitutionEnabledOperationFromModuleOperationCommand"/> class.
        /// </summary>
        /// <param name="dto">The institution-enabled operation data.</param>
        public UpdateInstitutionEnabledOperationFromModuleOperationCommand(UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO dto)
        {
            this.dto = dto;
        }
    }

    #endregion
}

namespace skolenza.application.Features.OperationCmd.Handlers
{
    #region Handler

    public class UpdateInstitutionEnabledOperationFromModuleOperationCommandHandler
    {
    }

    #endregion
}
