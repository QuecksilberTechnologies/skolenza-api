// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Base StaffMember Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Command



    /// <summary>
    /// Represents the UpdateStaffMemberCommand application component.
    /// </summary>
    public class UpdateStaffMemberCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateStaffMemberRequestDTO DTO { get; set; }

        public UpdateStaffMemberCommand(UpdateStaffMemberRequestDTO dto)
        {
            DTO = dto;
        }

    }
    /// <summary>
    /// Handles UpdateBaseStaffMemberCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateBaseStaffMemberCommandHandler : IRequestHandler<UpdateStaffMemberCommand, ApiResponse<bool>>
    {
        private readonly IBaseStaffMemberRepository _staffMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateBaseStaffMemberCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPermissionService _permissionService;

        public UpdateBaseStaffMemberCommandHandler(
            IBaseStaffMemberRepository staffMemberRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateBaseStaffMemberCommandHandler> logger,
            IMapper mapper,
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService,
            IIdEncoderService idEncoderService, ICommonRequestService commonRequestService, IPermissionService permissionService)
        {
            _staffMemberRepository = staffMemberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _permissionService = permissionService;

        }

        public async Task<ApiResponse<bool>> Handle(
      UpdateStaffMemberCommand request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("UpdateStaffMember started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

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
                // 4️⃣ FETCH EXISTING
                // ===============================
                var staffMember =
                    await _unitOfWork.StaffMembers.GetByIdAsync(
                        staffMemberId,
                        validation.InstitutionId,
                        true);

                if (staffMember == null)
                    throw new ApiException("StaffMember not found.", 404);

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (staffMember.IsInfoVerified || !staffMember.IsEditAllowed))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                // ===============================
                // 5️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 6️⃣ PATCH UPDATE
                // ===============================
                if (!string.IsNullOrWhiteSpace(request.DTO.FirstName))
                    staffMember.FirstName = request.DTO.FirstName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.MiddleName))
                    staffMember.MiddleName = request.DTO.MiddleName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.LastName))
                    staffMember.LastName = request.DTO.LastName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.Description))
                    staffMember.Description = request.DTO.Description.Trim();

                if (request.DTO.DateOfBirth.HasValue)
                    staffMember.DateOfBirth = request.DTO.DateOfBirth.Value;

                if (request.DTO.GenderId is > 0)
                    staffMember.GenderId = request.DTO.GenderId.Value;

                if (request.DTO.Relation.HasValue)
                    staffMember.Relation = request.DTO.Relation.Value;

                if (!string.IsNullOrWhiteSpace(request.DTO.EmergencyContactPerson))
                    staffMember.EmergencyContactPerson = request.DTO.EmergencyContactPerson;

                if (!string.IsNullOrWhiteSpace(request.DTO.EmergencyContactNumber))
                    staffMember.EmergencyContactNumber = request.DTO.EmergencyContactNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.MobileNumber))
                    staffMember.MobileNumber = request.DTO.MobileNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.BloodGroup))
                    staffMember.BloodGroup = request.DTO.BloodGroup;

                if (request.DTO.IsMarried.HasValue)
                    staffMember.IsMarried = request.DTO.IsMarried.Value;

                // ===============================
                // 7️⃣ AUDIT
                // ===============================
                staffMember.UpdatedById = validation.UserStaffMemberId;
                staffMember.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 8️⃣ SAVE
                // ===============================
                await _unitOfWork.StaffMembers.UpdateStaffMemberAsync(
                    staffMember,
                    validation.InstitutionId);

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateStaffMember success");

                return ApiResponse<bool>.Success(
                    true,
                    "StaffMember updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error updating staffMember");

                throw; //  MUST
            }
        }


    }















    //public class UpdateStaffMemberCommand : IRequest<ApiResponse<bool>>
    //{
    //    public GenricUpdateRequestDTO updateBaseStaffMemberRequestDTO { get; set; }

    //    public UpdateStaffMemberCommand(GenricUpdateRequestDTO dto)
    //    {
    //        updateBaseStaffMemberRequestDTO = dto;
    //    }

    //}
    //public class UpdateBaseStaffMemberCommandHandler : IRequestHandler<UpdateStaffMemberCommand, ApiResponse<bool>>
    //{
    //    private readonly IBaseStaffMemberRepository _staffMemberRepository;
    //    private readonly IUnitOfWork _unitOfWork;
    //    private readonly ILogger<UpdateBaseStaffMemberCommandHandler> _logger;
    //    private readonly IMapper _mapper;
    //    private readonly ITokenService _tokenService;
    //    private readonly IPermissionService _permissionService;
    //    private readonly IConfiguration _config;
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    private readonly IEncryptionService _encryptionService;


    //    public UpdateBaseStaffMemberCommandHandler(
    //        IBaseStaffMemberRepository staffMemberRepository,
    //        IUnitOfWork unitOfWork,
    //        ILogger<UpdateBaseStaffMemberCommandHandler> logger,
    //        IMapper mapper,
    //        ITokenService tokenService,
    //        IPermissionService permissionRepository,
    //        IConfiguration configuration,
    //        IHttpContextAccessor httpContextAccessor, IEncryptionService encryptionService)
    //    {
    //        _staffMemberRepository = staffMemberRepository;
    //        _unitOfWork = unitOfWork;
    //        _logger = logger;
    //        _mapper = mapper;
    //        _tokenService = tokenService;
    //        _permissionService = permissionRepository;
    //        _config = configuration;
    //        _httpContextAccessor = httpContextAccessor;
    //        _encryptionService = encryptionService;
    //    }

    //    public async Task<ApiResponse<bool>> Handle(UpdateStaffMemberCommand request, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            //  Step 1: Validate JWT Token
    //            var bearerToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
    //            if (string.IsNullOrEmpty(bearerToken))
    //                return ApiResponse<bool>.Fail("Unauthorized: Token not found.");

    //            var secretKey = _config["Jwt:Key"];
    //            var tokenClaims = TokenClaimHelper.ExtractClaims(bearerToken, secretKey);

    //            if (tokenClaims == null || tokenClaims.IsExpired)
    //                return ApiResponse<bool>.Fail("Invalid or expired token.");

    //            long empId = await _unitOfWork.CommonRepository.ValidateActiveUserLoginOnlyAsync(tokenClaims.UserId);
    //            if (empId < 1)
    //            {
    //                _logger.LogWarning("User validation failed for LoginId: {LoginId}", tokenClaims.UserId);
    //                await _unitOfWork.RollbackTransactionAsync();
    //                return ApiResponse<bool>.Fail("User is not authorized to perform this action.");
    //            }

    //            var DTO = request.updateBaseStaffMemberRequestDTO;
    //            var institutionKey = tokenClaims.InstitutionEncriptionKey;


    //            //  Step 4: Validate DTO Input
    //            if (DTO == null)
    //                return ApiResponse<bool>.Fail("Invalid request: DTO cannot be null.");

    //            if (!string.IsNullOrEmpty(DTO.StaffMemberId))
    //                DTO._StaffMemberId = EncryptionHelper1.DecryptId(_encryptionService, DTO.StaffMemberId, institutionKey);



    //            if (string.IsNullOrWhiteSpace(DTO.FieldName))
    //                return ApiResponse<bool>.Fail("Field name cannot be empty.");

    //            if (!string.IsNullOrEmpty(DTO.UserStaffMemberId))
    //                DTO._StaffMemberId = EncryptionHelper1.DecryptId(_encryptionService, DTO.UserStaffMemberId, institutionKey);


    //            if (string.IsNullOrWhiteSpace(DTO.FieldName))
    //                return ApiResponse<bool>.Fail("Field name is required.");

    //            //  Step 3: Fetch StaffMember Record
    //            var staffMember = await _staffMemberRepository.GetSingleRecordAsync(DTO._StaffMemberId, true);
    //            if (staffMember == null)
    //                return ApiResponse<bool>.Fail("StaffMember not found.");

    //            //  Step 4: Generate FieldWithAccess DTO (for read-only check)
    //            var editableDto = _mapper.Map<StaffMemberInfoEditableFieldsDTO>(staffMember);
    //            var accessDto = StaffMemberBasicInfoMapperHelper.ConvertToAccessResponseDTO(editableDto);

    //            //  Step 5: Reflection to find requested field
    //            var accessProp = typeof(GetBaseAccessStaffMemberResponseDTO)
    //                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                .FirstOrDefault(p => string.Equals(p.Name, DTO.FieldName, StringComparison.OrdinalIgnoreCase));

    //            if (accessProp == null)
    //                return ApiResponse<bool>.Fail($"Field '{DTO.FieldName}' does not exist.");

    //            var fieldWithAccess = accessProp.GetValue(accessDto);
    //            var isReadOnlyProp = fieldWithAccess?.GetType().GetProperty("IsReadOnly");
    //            bool isReadOnly = (bool?)isReadOnlyProp?.GetValue(fieldWithAccess) ?? false;

    //            if (isReadOnly)
    //                return ApiResponse<bool>.Fail($"Field '{DTO.FieldName}' is read-only and cannot be updated.");

    //            //  Step 6: Find actual property in StaffMember entity
    //            var staffMemberProp = typeof(StaffMember)
    //                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                .FirstOrDefault(p => string.Equals(p.Name, DTO.FieldName, StringComparison.OrdinalIgnoreCase));

    //            if (staffMemberProp == null || !staffMemberProp.CanWrite)
    //                return ApiResponse<bool>.Fail($"Property '{DTO.FieldName}' is not valid or not writable.");

    //            //  Step 7: Safe type conversion
    //            if (!TryConvertObjectToValue.TryConvertValue(DTO.FieldValue, staffMemberProp.PropertyType, out object? convertedValue))
    //            {
    //                _logger.LogWarning("Conversion failed for property '{Field}' with value '{Value}'", DTO.FieldName, DTO.FieldValue);
    //                return ApiResponse<bool>.Fail($"Value conversion failed for property '{DTO.FieldName}'.");
    //            }

    //            //  Step 8: Apply update
    //            staffMemberProp.SetValue(staffMember, convertedValue);
    //          //  staffMember.UpdatedById = DTO.StaffMemberId;
    //          //  staffMember.UpdatedDateTime = DateTime.UtcNow;

    //            //  Step 9: Commit update
    //            var updateStatus = await _unitOfWork.StaffMembers.UpdateStaffMemberFieldAsync(
    //                staffMember.Id, DTO.EntityName, DTO.FieldName, convertedValue, DTO._StaffMemberId);

    //            if (!updateStatus)
    //                return ApiResponse<bool>.Fail("Failed to update staffMember record.");

    //            return ApiResponse<bool>.Success(true, $"Field '{DTO.FieldName}' updated successfully.");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Unexpected error while updating staffMember field.");
    //            return ApiResponse<bool>.Fail("An unexpected error occurred.", new List<string> { ex.Message });
    //        }
    //    }
    //}

    #endregion
}
