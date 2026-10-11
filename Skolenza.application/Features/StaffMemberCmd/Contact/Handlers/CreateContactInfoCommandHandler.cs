// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Contact Info Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd.Contact.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateContactInfoCommand application component.
    /// </summary>
    public class CreateContactInfoCommand : IRequest<ApiResponse<List<GetContactResponseDTO>>>
    {
        public CreateContactRequestDTO DTO { get; set; }

        public CreateContactInfoCommand(CreateContactRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles CreateContactInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateContactInfoCommandHandler
      : IRequestHandler<CreateContactInfoCommand, ApiResponse<List<GetContactResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateContactInfoCommandHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public CreateContactInfoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateContactInfoCommandHandler> logger,
            IPermissionService permissionService,
            IConfiguration config,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _permissionService = permissionService;
            _config = config;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetContactResponseDTO>>> Handle(
    CreateContactInfoCommand request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("CreateContact started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 5️⃣ MAP ENTITY
                // ===============================
                var entity = _mapper.Map<StaffMemberContact>(request.DTO);

                entity.StaffMemberId = staffMemberId;
                entity.AddedById = validation.LoggedInStaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;
                entity.IsActive = true;
                entity.IsSoftDeleted = false;
                entity.IsEditAllowed = true;
                entity.IsInfoVerified = false;
                entity.IsPrimary = request.DTO.IsPrimary;

                // ===============================
                // 6️⃣ SAVE
                // ===============================
                var responseDTO =
                    await _unitOfWork.StaffMemberContactRepository
                        .CreateAsync(entity);

                if (responseDTO == null)
                    throw new ApiException("Failed to add contact info.", 500);

                // ===============================
                // 7️⃣ PROJECTION
                // ===============================
                var encryptedList =
                    ProjectionHelper.ToGetContactResponseDTOs(
                        responseDTO,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey
                    );

                // ===============================
                // 8️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("CreateContact success");

                // ===============================
                // 9️⃣ SUCCESS RESPONSE
                // ===============================
                return ApiResponse<List<GetContactResponseDTO>>.Success(
                    encryptedList,
                    "Contact info created successfully."
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "Error adding contact info | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }


    }


    #endregion
}

