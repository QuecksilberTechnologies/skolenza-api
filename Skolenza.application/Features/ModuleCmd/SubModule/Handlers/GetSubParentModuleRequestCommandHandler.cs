// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves child modules for a parent module.
// ================================================================

using skolenza.application.DTOs.Leave;
using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOS.Module.ManualModule;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.application.Features.ModuleCmd.SubModule.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to retrieve child modules for a parent module.
    /// </summary>
    public class GetSubParentModuleRequestCommand : IRequest<ApiResponse<List<GetModuleChildInversResponseDTO>>>
    {
        public GetSubParentModulRequestDTO DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubParentModuleRequestCommand"/> class.
        /// </summary>
        /// <param name="dTO">The parent module request data.</param>
        public GetSubParentModuleRequestCommand(GetSubParentModulRequestDTO dTO)
        {
            DTO = dTO;
        }
    }

    #endregion
}

// Handler implementation is intentionally pending.
// Request was relocated here as part of CQRS structural consolidation.
