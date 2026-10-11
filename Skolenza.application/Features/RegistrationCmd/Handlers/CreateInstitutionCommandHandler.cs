// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution registrations and validates their selected subscription plans.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.SeedData;
using skolenza.application.Constants;
using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.Token;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace skolenza.application.Features.RegistrationCmd.Handlers
{
    /// <summary>
    /// Represents a request to create a institution and its initial subscription.
    /// </summary>
    public class CreateInstitutionCommand : IRequest<ApiResponse<InstitutionCreateResponseDTO>>
    {
        public InstitutionCreationRequestDTO InstitutionCreateRequestDTO { get; set; }

        public CreateInstitutionCommand(InstitutionCreationRequestDTO createRequestDTO)
        {
            InstitutionCreateRequestDTO = createRequestDTO;
        }
    }

    /// <summary>
    /// Creates a institution registration after validating its selected subscription plan.
    /// </summary>
    public class CreateInstitutionCommandHandler : IRequestHandler<CreateInstitutionCommand, ApiResponse<InstitutionCreateResponseDTO>>
    {
        private const int InstitutionOnboardingLinkExpiryMinutes = 30;

        private readonly IEmailService _emailService;
        private readonly IStoreProcedureRepository _commonRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateInstitutionCommandHandler> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IConfiguration _configuration;
        private readonly ICommonRequestService _commonRequestService;

        public CreateInstitutionCommandHandler(
            ITokenService tokenService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<CreateInstitutionCommandHandler> logger,
            IEmailService emailService,
            IStoreProcedureRepository commonRepository,
            IPasswordService passwordService,
            IEncryptionService encryptionService,
            IIdEncoderService idEncoderService,
            IConfiguration configuration,
            ICommonRequestService commonRequestService)
        {
            _tokenService = tokenService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailService = emailService;
            _commonRepository = commonRepository;
            _passwordService = passwordService;
            _encryptionService = encryptionService;
            _idEncoderService = idEncoderService;
            _configuration = configuration;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<InstitutionCreateResponseDTO>> Handle(CreateInstitutionCommand request, CancellationToken cancellationToken)
        {
            long newInstitutionId = 0;
            string failureStage = "validating the registration request";
            var dto = request?.InstitutionCreateRequestDTO
                ?? throw new ArgumentNullException(nameof(request.InstitutionCreateRequestDTO));
            var onboardingRequest = dto as INewInstitutionOnboardingConfiguration;

            // The public registration endpoint has no authenticated request context. Only the
            // Host-side onboarding bridge carries the extended configuration and must validate
            // the caller's current Host module-operation permission.
            if (onboardingRequest is not null)
            {
                await HostRuntimePermissionValidator.ValidateAsync(
                    _commonRequestService,
                    _commonRepository,
                    dto.ModuleId,
                    dto.OperationId,
                    cancellationToken);
            }

            try
            {
                // =====================================================
                // STEP 1 : Validate request
                // =====================================================
                string prefix = dto.Prefix?.Trim().ToUpper() ?? string.Empty;
                string separator = dto.Separator?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(prefix))
                    return Fail("Prefix is required.");

                if (prefix.Length > 10)
                    return Fail("Prefix maximum length is 10 characters.");

                if (!Regex.IsMatch(prefix, "^[A-Z]+$"))
                    return Fail("Prefix must contain capital letters only. Example: QT, MSPL, BHEL.");

                if (string.IsNullOrWhiteSpace(separator) || !new[] { "_", "/", "-" }.Contains(separator))
                    return Fail("Separator must be one of these values: _, /, -");

                if (!int.TryParse(dto.RunningNumberLength, out int runningNumberLength))
                    return Fail("RunningNumberLength must be a valid number.");

                if (!new[] { 3, 4, 5, 6, 7 }.Contains(runningNumberLength))
                    return Fail("RunningNumberLength must be one of these values: 3, 4, 5, 6, 7.");

                if (string.IsNullOrWhiteSpace(dto.InstitutionEmail))
                    return Fail("Institution email is required.");

                // =====================================================
                // STEP 2 : Duplicate checks
                // =====================================================
                failureStage = "checking whether the email is already registered";
                bool isInstitutionEmailExists = await _unitOfWork.InstitutionRepository
                    .CheckInstitutionByEmailAsync(dto.InstitutionEmail);

                if (isInstitutionEmailExists)
                    return Fail("An institution with this email already exists.", AppConstants.ErrorCodes.Conflict);

                var existingUser = await _unitOfWork.UserLoginRepository
                    .GetStaffMemberIdByUserLogin(dto.InstitutionEmail);

                if (existingUser != null)
                    return Fail("This institution email is already assigned to a staff member.", AppConstants.ErrorCodes.Conflict);

                #region Subscription Plan Validation

                // Prevent a new institution from being assigned to a soft-deleted subscription plan.
                failureStage = "validating the selected subscription plan";
                var subscriptionPlan = await _unitOfWork.SubscriptionRepository
                    .GetNonDeletedSubscriptionPlanByIdAsync(dto.SubscriptionPlanId, cancellationToken);

                if (subscriptionPlan is null)
                    return Fail(AppConstants.ErrorMessages.SubscriptionPlanNotFound, AppConstants.ErrorCodes.NotFound);

                #endregion

                failureStage = "checking the email configuration";
                var defaultEmailConfiguration = await GetActiveHostEmailConfigurationAsync(cancellationToken);

                if (defaultEmailConfiguration is null)
                {
                    return Fail(
                        "An active default email configuration is required before institution registration.",
                        AppConstants.ErrorCodes.SmtpConfigurationUnavailable);
                }

                // =====================================================
                // STEP 3 : Prepare root entity
                // =====================================================
                var institutionEntity = _mapper.Map<Institution>(dto);
                string? hashedPassword = null;

                // =====================================================
                // STEP 4 : Start transaction
                // =====================================================
                failureStage = "saving the institution account";
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // =====================================================
                // STEP 5 : Create institution
                // =====================================================
                await _unitOfWork.InstitutionRepository.AddInstitutionAsync(institutionEntity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                newInstitutionId = institutionEntity.Id;

                if (newInstitutionId <= 0)
                {
                    await SafeRollbackAsync();
                    return Fail("Institution creation failed.");
                }

                _logger.LogInformation("Institution created successfully with InstitutionId: {InstitutionId}", newInstitutionId);

                failureStage = "saving the initial campus";
                var location = onboardingRequest?.InitialLocation;
                if (location is not null &&
                    (!AppConstants.IsSupportedInstitutionCampusType(location.LocationType) ||
                     string.IsNullOrWhiteSpace(location.LocationCode) ||
                     string.IsNullOrWhiteSpace(location.TimeZoneId)))
                {
                    await SafeRollbackAsync();
                    return Fail("A valid initial campus is required.");
                }

                var initialLocation = new Campus
                {
                    InstitutionId = newInstitutionId,
                    LocationCode = location?.LocationCode.Trim() ?? ConstantValues.InitialCampusCode,
                    LocationName = string.IsNullOrWhiteSpace(location?.LocationName)
                        ? institutionEntity.CompanyName
                        : location.LocationName.Trim(),
                    LocationType = location?.LocationType ?? (short)CampusType.HeadOffice,
                    CountryId = dto.CountryId,
                    StateId = location?.StateId,
                    DistrictId = location?.DistrictId,
                    LocalityId = location?.LocalityId,
                    Address = location?.Address,
                    Landmark = location?.Landmark,
                    PostalCode = location?.PostalCode,
                    Latitude = location?.Latitude,
                    Longitude = location?.Longitude,
                    GeoFenceRadiusMeters = location?.GeoFenceRadiusMeters,
                    TimeZoneId = location?.TimeZoneId.Trim() ?? ConstantValues.InitialCampusTimeZoneId,
                    IsHeadOffice = location is null || location.LocationType == (short)CampusType.HeadOffice,
                    IsGeoFenceEnabled = location?.IsGeoFenceEnabled ?? false,
                    IsAttendanceAllowed = location?.IsAttendanceAllowed ?? false,
                    IsBiometricEnabled = location?.IsBiometricEnabled ?? false,
                    IsActive = true,
                    IsSoftDeleted = false,
                    AddedById = newInstitutionId,
                    AddedDateTime = DateTime.UtcNow
                };

                if (!await _unitOfWork.CampusRepository.IsValidGeographyAsync(
                        initialLocation.CountryId,
                        initialLocation.StateId,
                        initialLocation.DistrictId,
                        initialLocation.LocalityId,
                        cancellationToken))
                {
                    await SafeRollbackAsync();
                    return Fail("The initial campus geography is invalid.");
                }

                await _unitOfWork.CampusRepository.AddAsync(initialLocation, cancellationToken);

                // =====================================================
                // STEP 6 : Create institution subscription
                // =====================================================
                failureStage = "saving the institution subscription";
                var subscription = new InstitutionSubscription
                {
                    InstitutionId = newInstitutionId,
                    SubscriptionPlanId = dto.SubscriptionPlanId,
                    SubscriptionStartDate = DateTime.UtcNow,
                    SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    IsTrial = true
                };

                var savedSub = await _unitOfWork.InstitutionSubscriptionRepository.AddInstitutionSubscriptionAsync(subscription);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                if (savedSub == null)
                {
                    await SafeRollbackAsync();
                    return Fail("Institution subscription creation failed.");
                }

                // =====================================================
                // STEP 7 : Fetch subscription plan info
                // =====================================================
                failureStage = "loading the selected subscription plan";
                var institutionSubscriptionPlan = await _unitOfWork.InstitutionSubscriptionRepository
                    .GetInstitutionSubscriptionPlanInfoAsync(new InstitutionSubscriptionPlanRequestDTO
                    {
                        InstitutionId = newInstitutionId,
                        SubscriptionPlanId = dto.SubscriptionPlanId,
                        IsTrial = true,
                        IsActive = true
                    });

                if (institutionSubscriptionPlan == null || !institutionSubscriptionPlan.Any())
                {
                    await SafeRollbackAsync();
                    return Fail("Institution subscription plan details not found.");
                }

                // =====================================================
                // STEP 8 : Load subscribed modules
                // =====================================================
                failureStage = "loading the selected plan modules";
                List<Module> subscriptionModules = await _unitOfWork.PlanModuleMappingRepository
                    .GetAllSubscribedModuleAsync(dto.SubscriptionPlanId);

                if (subscriptionModules == null || !subscriptionModules.Any())
                {
                    await SafeRollbackAsync();
                    return Fail("No modules found for selected subscription plan.");
                }

                var leafNodeModules = subscriptionModules
                    .Where(m => m.IsLeafNode == true)
                    .ToList();

                // =====================================================
                // STEP 9 : Prepare institution enabled modules
                // =====================================================
                List<InstitutionEnabledModule> institutionEnabledModules = subscriptionModules
                    .Select(m => new InstitutionEnabledModule
                    {
                        InstitutionId = newInstitutionId,
                        ModuleId = m.Id,
                        ParentModuleId = m.ParentModuleId,
                        IsLeafNode = m.IsLeafNode,
                        IsEnabled = true,
                        AddedById = newInstitutionId,
                        AddedDateTime = DateTime.UtcNow
                    })
                    .ToList();

                // =====================================================
                // STEP 10 : Prepare institution enabled operations
                // =====================================================
                failureStage = "configuring the selected plan access";
                List<ModuleOperationMapping> allModuleOperations =
                    await _unitOfWork.UserRolesPermissionOnModuleRepository
                        .GetModuleOperationMappings(leafNodeModules);

                var institutionEnabledOperations = _mapper.Map<List<InstitutionEnabledOperation>>(allModuleOperations);

                institutionEnabledOperations.ForEach(x =>
                {
                    x.InstitutionId = newInstitutionId;
                });

                await _unitOfWork.InstitutionModuleConfigurationRepository.CreateByDefaultEnabledModulesAsync(
                    newInstitutionId,
                    institutionEnabledModules,
                    institutionEnabledOperations);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                // =====================================================
                // STEP 11 : Create institution encryption key
                // =====================================================
                failureStage = "creating institution security data";
                string encryptedInstitutionKey = _encryptionService.GenerateKey();

                var institutionEncryptionKey = new InstitutionEncryptionKeys
                {
                    InstitutionId = newInstitutionId,
                    EncryptionKey = encryptedInstitutionKey,
                    IsActive = true
                };

                await _unitOfWork.InstitutionEncryptionKeyRepository.AddAsync(institutionEncryptionKey);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                // =====================================================
                // STEP 12 : Seed departments
                // =====================================================
                failureStage = "creating default departments";
                var departmentList = DepartmentSeedHelper.GetRuntimeDepartmentsToSeeds(
                    new Dictionary<int, string>(),
                    newInstitutionId,
                    dto.InstitutionTypeId,
                    newInstitutionId);
                bool isDepartmentSeeded = await _unitOfWork.DepartmentRepository
                    .AutoCreateDepartmentSeedAsync(departmentList, cancellationToken);

                if (!isDepartmentSeeded)
                {
                    await SafeRollbackAsync();
                    return Fail("Department creation failed.");
                }
 
                // Save required here so seeded departments get real IDs
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                Dictionary<string, int> deptMap =
                    await _unitOfWork.DepartmentRepository.GetDepartmentNameIdMapAsync(newInstitutionId);

                if (deptMap == null || !deptMap.Any())
                {
                    await SafeRollbackAsync();
                    return Fail("Seeded departments not found.");
                }

                string executiveOfficeDepartmentName = departmentList
                    .FirstOrDefault(x => x.IsExecutiveOffice == true)?.DepartmentName ?? "Executive Office";

                if (!deptMap.TryGetValue(executiveOfficeDepartmentName, out int insertedAdminDepartment) || insertedAdminDepartment <= 0)
                {
                    await SafeRollbackAsync();
                    return Fail("Executive Office department not found.");
                }

                // =====================================================
                // STEP 13 : Seed designations
                // =====================================================
                failureStage = "creating default designations";
                List<Designation> designations =
                    DesignationsSeedHelper.GetRuntimeDesignationsToSeed(
                        newInstitutionId,
                        newInstitutionId,
                        deptMap);

                int adminDesignationId =
                    await _unitOfWork.DesignationRepository.AutoCreateDesignationAsync(
                        designations,
                        insertedAdminDepartment);

                if (adminDesignationId <= 0)
                {
                    await SafeRollbackAsync();
                    return Fail("Designation creation failed.");
                }

                // =====================================================
                // STEP 14 : Create staffMember code pattern
                // =====================================================
                failureStage = "creating the staffMember code pattern";
                var staffMemberCodePattern = new StaffMemberCodePattern
                {
                    InstitutionId = newInstitutionId,
                    Prefix = prefix,
                    IncludeYear = dto.IncludeYear,
                    IncludeMonth = dto.IncludeMonth,
                    IncludeDepartment = dto.IncludeDepartment,
                    Separator = separator,
                    RunningNumberLength = runningNumberLength,
                    LastUsedNumber = 0,
                    IsActive = true,
                    AddedById = newInstitutionId,
                    AddedDateTime = DateTime.UtcNow
                };

                bool isEmpCodePatternCreated =
                    await _unitOfWork.InstitutionStaffCodePatternRepository.CreatePatternAsync(staffMemberCodePattern);

                if (!isEmpCodePatternCreated)
                {
                    await SafeRollbackAsync();
                    return Fail("StaffMember code pattern creation failed.");
                }

                // Save required here because next code generation may read active pattern from DB
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // =====================================================
                // STEP 15 : Generate staffMember code
                // =====================================================
                string? employmentCodeGenerated =
                    await _unitOfWork.InstitutionStaffCodePatternRepository
                        .GenerateStaffMemberCodeAsync(newInstitutionId, insertedAdminDepartment);

                if (string.IsNullOrWhiteSpace(employmentCodeGenerated))
                {
                    await SafeRollbackAsync();
                    return Fail("StaffMember code generation failed.");
                }

                // =====================================================
                // STEP 16 : Create default roles
                // =====================================================
                failureStage = "creating default roles";
                var rolesToCreate = new List<Role>();

                foreach (var roleName in new[]
                {
                    ConstantValues.InstitutionAdminRoleName,
                    ConstantValues.InstitutionManagerRoleName,
                    ConstantValues.InstitutionStaffMemberRoleName,
                    ConstantValues.InstitutionExternalRoleName
                })
                {
                    int roleType = roleName switch
                    {
                        var r when r == ConstantValues.InstitutionAdminRoleName => ConstantValues.RoleTypeAdmin,
                        var r when r == ConstantValues.InstitutionManagerRoleName => ConstantValues.RoleTypeManager,
                        var r when r == ConstantValues.InstitutionStaffMemberRoleName => ConstantValues.RoleTypeStaffMember,
                        var r when r == ConstantValues.InstitutionExternalRoleName => ConstantValues.RoleTypeClient,
                        _ => 0
                    };

                    rolesToCreate.Add(new Role
                    {
                        InstitutionId = newInstitutionId,
                        RoleName = roleName,
                        RoleType = roleType,
                        Remark = ConstantValues.GetRoleTypeRemark(roleType),
                        IsActive = true,
                        IsSoftDeleted = false,
                        IsSystemDefault = false,
                        AddedDateTime = DateTime.UtcNow,
                        AddedById = newInstitutionId
                    });
                }

                bool isRolesCreated = await _unitOfWork.RoleRepository.AutoCreatedForInstitutionRoleAsync(rolesToCreate);

                if (!isRolesCreated)
                {
                    await SafeRollbackAsync();
                    return Fail("Default role creation failed.");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                // =====================================================
                // STEP 16.1 : Create Role Permissions 🔥
                // =====================================================
                failureStage = "assigning administrator permissions";
               

                // 🔥 IMPORTANT: rolesToCreate me Id reliable nahi hota → DB se fetch karo
                var adminRole = await _unitOfWork.RoleRepository.GetInstitutionAdminRoleAsync(newInstitutionId);

                if (adminRole == null || adminRole.Id <= 0)
                {
                    await SafeRollbackAsync();
                    return Fail("Admin role not found for permission setup.");
                }
 
                // Get enabled modules + operations (institution specific)
                InstitutionEnabledOperation institutionEnabledOperation = new InstitutionEnabledOperation
                {
                    InstitutionId = newInstitutionId
                };

                var moduleOperations = await _unitOfWork.UserRolesPermissionOnModuleRepository.GetAllInstitutionModuleWithOperation(institutionEnabledOperation);

                // Prepare permissions (ONLY ADMIN)
                var rolePermissions = new List<RoleModuleAndPermission>();

                if (moduleOperations?.Modules != null)
                {
                    foreach (var module in moduleOperations.Modules)
                    {
                        if (module.Operations == null) continue;

                        foreach (var op in module.Operations)
                        {
                            rolePermissions.Add(new RoleModuleAndPermission
                            {
                                RoleId = adminRole.Id, // 🔥 ONLY ADMIN
                                ModuleId = module.Id,
                                OperationId = op.Id,
                                HasAccess = true,      // 🔥 FULL ACCESS
                                IsActive = true,
                                AddedById = newInstitutionId,
                                AddedDateTime = DateTime.UtcNow,
                                Remark = "Auto-assigned during institution creation for administrator role"


                            });
                        }
                    }
                }

                // 4️⃣ Bulk insert
                int insertedCount = await _unitOfWork.UserRolesPermissionOnModuleRepository.BulkInsertAsync(rolePermissions);
                // 5️⃣ Save
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                        "Institution administrator permissions assigned | InstitutionId={InstitutionId}, InsertedCount={InsertedCount}",
                        newInstitutionId, insertedCount);



                int createdAdminRoleId = adminRole.Id;

                // =====================================================
                // STEP 17 : Create staffMember (institution admin)
                // =====================================================
                failureStage = "creating the administrator account";
                var onboardingTypeId = await _unitOfWork.StaffEmploymentTypeRepository.EnsureOnboardingTypeAsync(
                    newInstitutionId, newInstitutionId, cancellationToken);
                var staffMember = new StaffMember
                {
                    InstitutionId = newInstitutionId,
                    FirstName = dto.ContactPersonName?.Trim(),
                    DepartmentId = insertedAdminDepartment,
                    DesignationId = adminDesignationId,
                    CountryId = dto.CountryId,
                    OfficialEmail = institutionEntity.InstitutionEmail,
                    EmployementCode = employmentCodeGenerated,
                    IsActive = true,
                    IsSoftDeleted = false,
                    IsEditAllowed = true,
                    StaffEmploymentTypeId = onboardingTypeId,
                    StaffCategoryId = AppConstants.StaffCategories.Administrative,
                    AddedById = newInstitutionId,
                    AddedDateTime = DateTime.UtcNow
                };

                var loginCredential = new LoginCredential
                {
                    InstitutionId = newInstitutionId,
                    LoginId = institutionEntity.InstitutionEmail,
                    StaffMember = staffMember,
                    Password = hashedPassword,
                    IsActive = true,
                    IsSoftDeleted = false,
                    IsPasswordChangeRequired = true,
                    HasFirstLogin = true,
                    Remark = "System Generated Account",
                    IsOnboard = false,
                    AddedById = newInstitutionId,
                    AddedDateTime = DateTime.UtcNow
                };

                var userRole = new UserRole
                {
                    StaffMember = staffMember,
                    RoleId = createdAdminRoleId,
                    IsPrimaryRole = true,
                    IsActive = true,
                    Remark = "Initial role assignment during staffMember creation",
                    AssignedDateTime = DateTime.UtcNow,
                    RoleStartDate = DateTime.UtcNow,
                    ApprovalRequired = false,
                    AddedDateTime = DateTime.UtcNow,
                    ApprovalStatus = ConstantValues.IsByDefaultTrue ? "Approved" : "Pending",
                    IsSoftDeleted = false,
                    AddedById = newInstitutionId
                };

                await _unitOfWork.StaffMembers.AddStaffMemberAggregateAsync(
                    staffMember,
                    loginCredential,
                    userRole,
                    cancellationToken);

                // Save required here because staffMemberId is needed
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                long staffMemberId = staffMember.Id;

                if (staffMemberId <= 0)
                {
                    await SafeRollbackAsync();
                    return Fail("StaffMember creation failed.");
                }

                var createdStaffMember = await _unitOfWork.StaffMembers.GetCreatedStaffMemberResponseAsync(staffMemberId, cancellationToken);

                if (createdStaffMember == null || string.IsNullOrWhiteSpace(createdStaffMember.Id))
                {
                    await SafeRollbackAsync();
                    return Fail("StaffMember creation failed.");
                }

                // =====================================================
                // STEP 18 : Create institution profile
                // =====================================================
                failureStage = "saving the institution profile";
                if (onboardingRequest?.Profile is not null &&
                    !string.IsNullOrWhiteSpace(onboardingRequest.Profile.BusinessType) &&
                    !AppConstants.IsSupportedInstitutionCategory(onboardingRequest.Profile.BusinessType))
                {
                    await SafeRollbackAsync();
                    return Fail("Select a valid Institution ownership category.");
                }

                var institutionProfile = new InstitutionProfile
                {
                    InstitutionId = newInstitutionId,
                    LogoUrl = onboardingRequest?.Profile.LogoUrl,
                    ThemeColor = onboardingRequest?.Profile.ThemeColor,
                    BusinessType = onboardingRequest?.Profile.BusinessType,
                    Industry = onboardingRequest?.Profile.Industry,
                    TotalStaffMembers = onboardingRequest?.Profile.TotalStaffMembers,
                    TotalCampuses = onboardingRequest?.Profile.TotalCampuses,
                    FoundedYear = onboardingRequest?.Profile.FoundedYear,
                    WebsiteUrl = onboardingRequest?.Profile.WebsiteUrl
                };

                await _unitOfWork.InstitutionRepository.AddInstitutionProfileAsync(institutionProfile);

                // Admin permissions were inserted in step 16.1 and the staffMember's
                // UserRole was saved with the aggregate. Do not insert them twice.

                // =====================================================
                // STEP 21 : Final save before commit
                // =====================================================
                await _unitOfWork.SaveChangesAsync(cancellationToken);


           

                // =====================================================
                // STEP 23 : Prepare token
                // =====================================================
                failureStage = "generating the onboarding link";
                string encryptedStaffMemberId = _idEncoderService.EncodeId_long(staffMemberId, null);
                string encryptedInstitutionId = _idEncoderService.EncodeId_long(newInstitutionId, null);

                var getTokenInfoDTO = new GetTokenInfoDTO
                {
                    StaffMemberId = encryptedStaffMemberId,
                    InstitutionId = encryptedInstitutionId,
                    Email = staffMember.OfficialEmail!,
                    FullName = staffMember.FirstName,
                    TokenPurpose = _idEncoderService.EncodeId_int(ConstantValues.SetPassword, ""),
                    IssuedAt = DateTime.UtcNow,
                    Expiry = DateTime.UtcNow.AddMinutes(InstitutionOnboardingLinkExpiryMinutes),
                    IsFirstLogin = true,
                    ClientType = "Web"
                };

                // =====================================================
                // STEP 24 : Generate a valid onboarding token before committing.
                // =====================================================
                string token = await _tokenService.GenerateInstitutionToken(getTokenInfoDTO);
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError(
                        "Institution onboarding token generation failed | InstitutionId={InstitutionId}",
                        newInstitutionId);
                    await SafeRollbackAsync();
                    return Fail("Institution onboarding link could not be generated.");
                }

                // =====================================================
                // STEP 25 : Commit the Institution aggregate before attempting external SMTP work.
                // =====================================================
                failureStage = "finalizing institution registration";
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // =====================================================
                // STEP 26 : Send email after commit. A mail failure must not undo a valid Institution.
                // =====================================================
                var emailSent = await SendInstitutionWelcomeEmailAsync(
                    newInstitutionId,
                    staffMember.OfficialEmail!,
                    staffMember.FirstName,
                    token);

                return CreateSuccessResponse(emailSent);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "Institution registration was cancelled | InstitutionId={InstitutionId}, Stage={Stage}",
                    newInstitutionId,
                    failureStage);

                await SafeRollbackAsync();

                return Fail(
                    "Institution registration was cancelled before it completed. No account was finalized.",
                    AppConstants.ErrorCodes.InstitutionRegistrationFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while creating institution | InstitutionId={InstitutionId}, Stage={Stage}",
                    newInstitutionId,
                    failureStage);

                await SafeRollbackAsync();

                return CreateUnexpectedFailureResponse(ex, failureStage);
            }
        }

        private ApiResponse<InstitutionCreateResponseDTO> CreateUnexpectedFailureResponse(
            Exception exception,
            string failureStage)
        {
            var databaseException = FindDatabaseException(exception);

            return databaseException?.SqlState switch
            {
                "23505" => Fail(
                    $"Institution registration conflicts with existing setup data while {failureStage}. " +
                    "No account was finalized. Contact support before retrying.",
                    AppConstants.ErrorCodes.Conflict),
                "23503" => Fail(
                    $"A selected registration option is no longer available while {failureStage}. " +
                    "Refresh the page, select the current options, and try again.",
                    AppConstants.ErrorCodes.Conflict),
                "23502" => Fail(
                    $"Required institution setup data is missing while {failureStage}. " +
                    "No account was finalized. Contact support.",
                    AppConstants.ErrorCodes.InstitutionRegistrationFailed),
                "40001" or "40P01" => Fail(
                    $"Institution registration was interrupted by another database operation while {failureStage}. " +
                    "No account was finalized. Please try again.",
                    AppConstants.ErrorCodes.Conflict),
                _ => Fail(
                    $"Institution registration could not complete while {failureStage}. " +
                    "Please try again. If the problem continues, contact support.",
                    AppConstants.ErrorCodes.InstitutionRegistrationFailed)
            };
        }

        private static DbException? FindDatabaseException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is DbException databaseException)
                {
                    return databaseException;
                }
            }

            return null;
        }

        private ApiResponse<InstitutionCreateResponseDTO> Fail(
            string message,
            string errorCode = AppConstants.ErrorCodes.InstitutionRegistrationFailed)
        {
            return new ApiResponse<InstitutionCreateResponseDTO>
            {
                IsSucceeded = false,
                Message = message,
                ErrorCode = errorCode,
                Data = new InstitutionCreateResponseDTO
                {
                    Success = false,
                    EmailSent = false
                }
            };
        }

        /// <summary>
        /// Resolves the Host SMTP configuration selected for registration.
        /// The repository applies both IsActive and IsDefault filters, so a
        /// self-registration flow never chooses an arbitrary Host record.
        /// </summary>
        private Task<DefaultEmailConfig?> GetActiveHostEmailConfigurationAsync(
            CancellationToken cancellationToken) =>
            _unitOfWork.DefaultEmailConfigRepository
                .GetActiveDefaultEmailConfigAsync(cancellationToken);

        private static string? GetConfiguredValue(string? suppliedValue, string? fallbackValue)
        {
            return string.IsNullOrWhiteSpace(suppliedValue)
                ? fallbackValue
                : suppliedValue.Trim();
        }

        private async Task<bool> SendInstitutionWelcomeEmailAsync(
            long institutionId,
            string recipientEmail,
            string? recipientName,
            string token)
        {
            var baseUrl = _configuration["FrontEndWebURL:BaseUrl"]?.Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                _logger.LogError(
                    "Institution welcome email was not sent because FrontEndWebURL:BaseUrl is invalid | InstitutionId={InstitutionId}",
                    institutionId);
                return false;
            }

            var verificationUrl = $"{baseUrl.TrimEnd('/')}/auth/set-password?token={Uri.EscapeDataString(token)}";

            try
            {
                var emailSent = await _emailService.SendTemplatedEmailUsingHostConfigAsync(
                    ConstantValues.WelcomeEmail,
                    recipientEmail,
                    institutionId,
                    new Dictionary<string, string>
                    {
                        ["UserName"] = recipientName ?? string.Empty,
                        ["VerificationUrl"] = verificationUrl,
                        ["LinkExpiryMinutes"] = InstitutionOnboardingLinkExpiryMinutes.ToString()
                    });

                if (emailSent)
                {
                    _logger.LogInformation(
                        "Institution welcome email accepted by SMTP | InstitutionId={InstitutionId}",
                        institutionId);
                }
                else
                {
                    _logger.LogWarning(
                        "Institution welcome email was not accepted by SMTP | InstitutionId={InstitutionId}",
                        institutionId);
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Institution welcome email dispatch failed after institution creation | InstitutionId={InstitutionId}",
                    institutionId);
                return false;
            }
        }

        private static ApiResponse<InstitutionCreateResponseDTO> CreateSuccessResponse(bool emailSent)
        {
            return new ApiResponse<InstitutionCreateResponseDTO>
            {
                IsSucceeded = true,
                Message = emailSent
                    ? "Institution created successfully. Please check your email and set password."
                    : "Institution created successfully, but welcome email could not be sent.",
                Data = new InstitutionCreateResponseDTO
                {
                    Success = true,
                    EmailSent = emailSent
                }
            };
        }

        private async Task SafeRollbackAsync()
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback transaction failed.");
            }
        }
    }
}
