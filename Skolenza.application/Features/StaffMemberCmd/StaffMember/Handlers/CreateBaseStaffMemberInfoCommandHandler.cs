// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Base StaffMember Info Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Constants;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;

using skolenza.domain.Entity; using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateBaseStaffMemberInfoCommand application component.
    /// </summary>
    public class CreateBaseStaffMemberInfoCommand : IRequest<ApiResponse<GetBaseStaffMemberResponseDTO>>
    {
        public CreateBaseStaffMemberRequestDTO DTO { get; set; }

        public CreateBaseStaffMemberInfoCommand(CreateBaseStaffMemberRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles CreateBaseStaffMemberInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateBaseStaffMemberInfoCommandHandler
   : IRequestHandler<CreateBaseStaffMemberInfoCommand, ApiResponse<GetBaseStaffMemberResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateBaseStaffMemberInfoCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;


        public CreateBaseStaffMemberInfoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateBaseStaffMemberInfoCommandHandler> logger,
            ICommonRequestService commonRequestService,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService, ITokenService tokenService, IEmailService emailService, IConfiguration configuration
             )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;

        }

        public async Task<ApiResponse<GetBaseStaffMemberResponseDTO>> Handle(
      CreateBaseStaffMemberInfoCommand request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("CreateStaffMember started");

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
                // 4️⃣ CHECK DUPLICATE USER
                // ===============================
                var existingUser =
                    await _unitOfWork.UserLoginRepository
                        .GetStaffMemberIdByUserLogin(request.DTO.OfficialEmail);

                if (existingUser != null)
                    throw new ApiException("User already exists.", 409);

                // ===============================
                // 5️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 6️⃣ MAP STAFF_MEMBER
                // ===============================
                var staffMember = _mapper.Map<StaffMember>(request.DTO);

                var staffEmploymentType = await _unitOfWork.StaffEmploymentTypeRepository.GetStaffEmploymentTypeByIdAsync(
                    validation.InstitutionId, request.DTO.StaffEmploymentTypeId, cancellationToken);
                if (staffEmploymentType?.IsActive != true)
                {
                    throw new ValidationErrorException("StaffEmploymentType must be active and belong to this institution.");
                }

                staffMember.InstitutionId = validation.InstitutionId;
                staffMember.AddedById = validation.UserStaffMemberId;
                staffMember.AddedDateTime = DateTime.UtcNow;
                staffMember.DateOfOnBoarding = request.DTO.DateOfOnBoarding ?? DateTime.UtcNow;
                staffMember.IsActive = true;
                staffMember.IsInfoVerified = false;
                staffMember.IsEditAllowed = true;
                staffMember.IsSoftDeleted = false;

                staffMember.StaffMemberContact.Add(
                    StaffMemberContactInfoMapperHelper.CreateInitialContact(
                        staffMember,
                        validation.UserStaffMemberId,
                        DateTime.UtcNow));

                // ===============================
                // 7️⃣ GENERATE STAFF_MEMBER CODE
                // ===============================
                staffMember.EmployementCode =
                    await _unitOfWork.InstitutionStaffCodePatternRepository
                        .GenerateStaffMemberCodeAsync(
                            validation.InstitutionId,
                            staffMember.DepartmentId,
                            staffMember.DateOfOnBoarding);

                // ===============================
                // 8️⃣ DEFAULT ROLE
                // ===============================
                if (request.DTO.RoleId <= 0)
                {
                    var roles = await _unitOfWork.RoleRepository
                        .GetRoleAsync(validation.InstitutionId,
                                      ConstantValues.RoleTypeStaffMember,
                                      true);

                    var defaultRole = roles.FirstOrDefault();

                    if (defaultRole == null)
                        throw new ValidationErrorException("Default staffMember role not configured.");

                    request.DTO.RoleId = defaultRole.Id;
                }

                // ===============================
                // 9️⃣ CREATE RELATED ENTITIES
                // ===============================
                var loginCredential = new LoginCredential
                {
                    InstitutionId = staffMember.InstitutionId,
                    LoginId = request.DTO.OfficialEmail,
                    HasFirstLogin = true,
                    IsPasswordChangeRequired = true,
                    IsActive = true,
                    StaffMember = staffMember
                };

                var userRole = new UserRole
                {
                    StaffMember = staffMember,
                    RoleId = request.DTO.RoleId,
                    RoleStartDate = DateTime.UtcNow,
                    IsActive =true,
                    IsPrimaryRole = true,
                    AddedById = validation.UserStaffMemberId,
                    AddedDateTime = DateTime.UtcNow
                };

                // ===============================
                //  SAVE
                // ===============================
                var savedStaffMember =
                    await _unitOfWork.StaffMembers
                        .CreateStaffMemberAsync(staffMember, loginCredential, userRole);

                if (savedStaffMember == null)
                    throw new ApiException("StaffMember creation failed.", 500);

                // ===============================
                // 1️⃣1️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // 1️⃣2️⃣ RESPONSE
                // ===============================
                var responseDto =
                    ProjectionHelper.ToGetBaseInfoResponseDTO(
                        savedStaffMember,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey);

                // ===============================
                // 1️⃣3️⃣ EMAIL (NON-BLOCKING)
                // ===============================
                try
                {
                    var token = await _tokenService.GenerateInstitutionToken(new GetTokenInfoDTO
                    {
                        StaffMemberId = responseDto.Id,
                        Email = savedStaffMember.OfficialEmail!,
                        FullName = $"{savedStaffMember.FirstName} {savedStaffMember.LastName}",
                        TokenPurpose = _idEncoderService.EncodeId_int(ConstantValues.SetPassword, ""),
                        IssuedAt = DateTime.UtcNow,
                        Expiry = DateTime.UtcNow.AddMinutes(30),
                        IsFirstLogin = true,
                        ClientType = "Web"
                    });

                    string baseUrl = _configuration["FrontEndWebURL:BaseUrl"] ?? "";

                    await _emailService.SendTemplatedEmailAsync(
                        ConstantValues.WelcomeEmail,
                        savedStaffMember.OfficialEmail!,
                        validation.InstitutionId,
                        new Dictionary<string, string>
                        {
                            ["UserName"] = savedStaffMember.FirstName,
                            ["VerificationUrl"] = $"{baseUrl}/auth/set-password?token={token}",
                            ["LinkExpiryMinutes"] = "30"
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email send failed (non-blocking)");
                }

                _logger.LogInformation("CreateStaffMember success");

                return ApiResponse<GetBaseStaffMemberResponseDTO>
                    .Success(responseDto, "StaffMember created successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "StaffMember creation failed");

                throw; //  MUST
            }
        }
    }


    #endregion
}

