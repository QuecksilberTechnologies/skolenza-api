// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get All Ticket Type Query Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Holiday;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketType.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetAllTicketTypeQuery application component.
    /// </summary>
    public class GetAllTicketTypeQuery
        : IRequest<ApiResponse<List<GetTicketTypeResponseDTO>>>
    {
        public GetTicketTypeRequestDTO DTO { get; }

        public GetAllTicketTypeQuery(GetTicketTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }
    /// <summary>
    /// Handles GetAllTicketTypeQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetAllTicketTypeQueryHandler: IRequestHandler<GetAllTicketTypeQuery, ApiResponse<List<GetTicketTypeResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllTicketTypeQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetAllTicketTypeQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAllTicketTypeQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetTicketTypeResponseDTO>>> Handle(
     GetAllTicketTypeQuery request,
     CancellationToken cancellationToken)
        {
            try
            {
                // ===============================
                // 1️⃣ COMMON VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ RBAC CHECK
                // ===============================
                //await _commonRequestService.HasAccessAsync(
                //    ModuleEnum.Ticket,
                //    OperationEnum.View);

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

                // ===============================
                // 4️⃣ DEFAULT PAGINATION (IMPORTANT )
                // ===============================
                request.DTO.PageNumber = request.DTO.PageNumber <= 0 ? 1 : request.DTO.PageNumber;
                request.DTO.PageSize = request.DTO.PageSize <= 0 ? 10 : request.DTO.PageSize;

                // ===============================
                // 5️⃣ REPOSITORY CALL
                // ===============================
                var result = await _unitOfWork.TicketTypeRepository
                    .AllAsync(validation.InstitutionId, request.DTO);

                if (result == null)
                    throw new ApiException("Failed to fetch TicketTypes.", 500);

                // ===============================
                // 6️⃣ RETURN (YOUR PATTERN )
                // ===============================
                return ApiResponse<List<GetTicketTypeResponseDTO>>
                    .SuccessPaginatedOnly(
                        result.Data,
                        result.PageNumber,
                        result.PageSize,
                        result.TotalCount,
                        result.TotalPages,
                        "TicketTypes fetched successfully."
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TicketTypes");
                throw;
            }
        }
    }

    #endregion
}

