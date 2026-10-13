// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates repositories and transactions over the Skolenza persistence context.
// ================================================================

using AutoMapper;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICacheService;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IQRService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides lazy repository construction and transaction coordination for application handlers.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SkolenzaDbContext _context;
    private readonly IQRService _qrService;
    private readonly IMapper _mapper;
    private readonly IEncryptionService _encriptionService;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPermissionService _permissionService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _config;
    private readonly ICacheService _cacheService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UnitOfWork> _logger;


    private IDbContextTransaction? _currentTransaction;
    private ICompilanceRuleRepository? _compilanceRuleRepository;
    private bool _disposed;
    private   ITicketGenrationRepository _ticketRepository;
    private ITicketThreadRepository _ticketThreadRepository;
    private IThreadMessageRepository _threadMessageRepository;

    private  ITicketAttachmentRepository _ticketAttachmentRepository;
    private ITicketHistoryRepository _ticketHistoryRepository;
    private IHostUserRepository _hostUserRepository;
    private IHostRoleRepository _hostRoleRepository;
    private IHostRolePermissionRepository _hostRolePermissionRepository;

    private IStaffMemberManagerMappingRepository? _staffMemberManagerMappingRepository    ;
    private IForgotPasswordOtpRepository? _forgotPasswordOtpRepository;
    private IInstitutionEmailConfigRepository? _institutionEmailConfigRepository;
    private IDefaultEmailConfigRepository? _defaultEmailConfigRepository;
    private IHolidayRepository? _holidayRepository;
    private IInstitutionModuleConfigurationRepository? _institutionModuleConfigurationRepository;
    private IInstitutionParentModuleRepository? _institutionParentModuleRepository;
    private IPlanModuleMappingRepository? _planModuleMappingRepository;
    private ILeaveRepository? _leaveRepository;
    private ITicketTypeRepository? _ticketTypeRepository;
    private IReportingTypeRepository? _reportingTypeRepository;
    private IAssetStatusRepository? _assetStatusRepository;
    private IAssetTypeRepository? _assetTypeRepository;
    private IAssetCategoryRepository? _assetCategoryRepository;
    private ITicketClassificationRepository? _ticketClassificationRepository;
    private ITicketHeaderRepository? _ticketHeaderRepository;
    private IBaseStaffMemberRepository? _baseStaffMemberRepository;
    private IStaffMemberContactRepository? _staffMemberContactRepository;
    private IStaffMemberBankRepository? _staffMemberBankRepository;
    private IStaffMemberEducationRepository? _staffMemberEducationRepository;
    private IStaffMemberExperienceRepository? _staffMemberExpereinceRepository;
    private IStaffMemberExperienceDetailRepository? _staffMemberExperienceDetailRepository;
    private IStaffMemberExperienceDocumentRepository? _staffMemberExpereinceDocumentRepository;
    private IStaffMemberIdentityRepository? _staffMemberIdentityRepository;
    private IStaffMemberInsuranceRepository? _staffMemberInsuranceRepository;
    private IStaffMemberDependentRepository? _staffMemberDependentRepository;
    private IWorkflowStagesRepository? _workflowStagesRepository;
    private IInstitutionRepository? _institutionRepository;
    private IUserRoleRepository? _userRoleRepository;
    private IRoleRepository? _roleRepository;
    private IStaffEmploymentTypeRepository? _staffEmploymentTypeRepository;
    private IStaffCategoryRepository? _staffCategoryRepository;
    private IStaffProfessionalStructureRepository? _staffProfessionalStructureRepository;
    private IStaffAcademicStructureRepository? _staffAcademicStructureRepository;
    private IInstitutionStaffCodePatternRepository? _institutionStaffMemberCodePatternRepository;
    private ICategoryRepository? _categoryRepository;
    private IStaffEmploymentTypeBasicMenuRepository? _staffEmploymentTypeBasicMenuRepository;
    private IUserRolesPermissionOnModuleRepository? _userRolesPermissionOnModuleRepository;
    private ICandidateRegistrationRepository? _candidateRegistrationRepository;
    private ICandidateCategorySkillRepository? _candidateCategorySkillRepository;
    private ILocationRepository? _locationRepository;
    private IAssetRepository? _assetRepository;
    private IRefreshTokenRepository? _refreshTokenRepository;
    private IStoreProcedureRepository? _commonRepository;
    private IUserLoginReopsitory? _userLoginRepository;
    private IEmailTemplateRepository? _emailTemplateRepository;
    private IInstitutionEmailTemplateRepository? _institutionEmailTemplateRepository;
    private ISubscriptionRepository? _subscriptionRepository;
    private IInstitutionSubscriptionRepository? _institutionSubscriptionRepository;
    private IModuleRepository? _moduleRepository;
    private IGenderRepository? _genderRepository;
    private IInstitutionEncryptionKeyRepository? _institutionEncryptionKeyRepository;
    private IPermissionRepository? _permissionRepository;
    private IDepartmentRepository? _departmentRepository;
    private IDesignationRepository? _designationRepository;
    private IOperationRepository? _operationRepository;
    private ITravelRepository? _travelRepository;
    private IClientRepository? _clientRepository;
    private ICampusRepository? _campusRepository;
    private IAttendancePolicyRepository? _attendancePolicyRepository;
    private IStaffMemberLocationAssignmentRepository? _staffMemberLocationAssignmentRepository;
    private IStaffMemberDeviceEnrollmentRepository? _staffMemberDeviceEnrollmentRepository;
    private IInstitutionCardMasterRepository? _institutionCardMasterRepository;
    private IStaffMemberWorkArrangementRepository? _staffMemberWorkArrangementRepository;
    private IStaffMemberWorkPatternRepository? _staffMemberWorkPatternRepository;
    private IStaffMemberWorkModeOverrideRequestRepository? _staffMemberWorkModeOverrideRequestRepository;
    private IDeviceMasterRepository? _deviceMasterRepository;
    private IInstitutionDeviceRepository? _institutionDeviceRepository;
    private IInstitutionDeviceConfigurationRepository? _institutionDeviceConfigurationRepository;

    public UnitOfWork(
        SkolenzaDbContext context,
        ILoggerFactory loggerFactory,
        IQRService qrService,
        IFileStorageService fileStorageService,
        IMapper mapper,
        IPasswordService passwordService,
        IConfiguration configuration,
        IPermissionService permissionService,
        ICacheService cacheService,
        IEncryptionService encryptionService,
        IIdEncoderService idEncoderService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<UnitOfWork>();
        _qrService = qrService ?? throw new ArgumentNullException(nameof(qrService));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _encriptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _idEncoderService = idEncoderService ?? throw new ArgumentNullException(nameof(idEncoderService));
    }

   
    public IUserLoginReopsitory UserLoginRepository =>
        _userLoginRepository ??= new UserLoginReopsitory(_context, _loggerFactory.CreateLogger<UserLoginReopsitory>(), _mapper, _passwordService, _config);
    //public ICompilanceRuleRepository CompilanceRuleRepository =>
    //    _compilanceRuleRepository ??= new CompilanceRuleRepository(_context, _loggerFactory.CreateLogger<CompilanceRuleRepository>(), _mapper, _config);

    public IPermissionRepository PermissionRepository =>
        _permissionRepository ??= new PermissionRepository(_context, _loggerFactory.CreateLogger<PermissionRepository>(), _cacheService);

    public ITicketTypeRepository TicketTypeRepository =>
        _ticketTypeRepository ??= new TicketTypeRepository(_context, _loggerFactory.CreateLogger<TicketTypeRepository>(), _mapper);

    public IBaseStaffMemberRepository StaffMembers =>
        _baseStaffMemberRepository ??= new BaseStaffMemberRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<BaseStaffMemberRepository>(),
            _passwordService,
            _encriptionService);

    public IInstitutionStaffCodePatternRepository InstitutionStaffCodePatternRepository =>
        _institutionStaffMemberCodePatternRepository ??= new InstitutionStaffCodePatternRepository(
            _context,
            _loggerFactory.CreateLogger<InstitutionStaffCodePatternRepository>());

    public IStaffMemberContactRepository StaffMemberContactRepository =>
        _staffMemberContactRepository ??= new StaffMemberContactRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberContactRepository>(),
            _passwordService,
            _encriptionService);

    public IStaffMemberEducationRepository StaffMemberEducationRepository =>
        _staffMemberEducationRepository ??= new StaffMemberEducationRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberEducationRepository>(),
            _passwordService,
            _encriptionService, _fileStorageService);
    public IStaffMemberExperienceRepository StaffMemberExperienceRepository =>      
        _staffMemberExpereinceRepository ??= new StaffMemberExpereinceRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberExpereinceRepository>(),
            _passwordService,
            _encriptionService,_fileStorageService);
    
    public IStaffMemberExperienceDocumentRepository StaffMemberExperienceDocumentRepository =>
        _staffMemberExpereinceDocumentRepository ??= new StaffMemberExperienceDocumentRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberExperienceDocumentRepository>(),
            _passwordService,
            _encriptionService);

    
    public IStaffMemberIdentityRepository StaffMemberIdentityRepository =>
        _staffMemberIdentityRepository ??= new StaffMemberIdentityRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberIdentityRepository>(),
            _passwordService,
            _encriptionService);
    public IStaffMemberManagerMappingRepository StaffMemberManagerMappingRepository =>
        _staffMemberManagerMappingRepository ??= new StaffMemberManagerMappingRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberManagerMappingRepository>()
            );

    

    public IStaffMemberBankRepository StaffMemberBankRepository =>
        _staffMemberBankRepository ??= new StaffMemberBankRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberBankRepository>(),
            _passwordService,
            _encriptionService, _fileStorageService);

    public IHostUserRepository HostUserRepository =>
        _hostUserRepository ??= new HostUserRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<HostUserRepository>(),
            _passwordService,
            _encriptionService);
    public IHostRoleRepository HostRoleRepository =>
        _hostRoleRepository ??= new HostRoleRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<HostRoleRepository>(),
            _passwordService,
            _encriptionService);
    public IHostRolePermissionRepository HostRolePermissionRepository =>
        _hostRolePermissionRepository ??= new HostRolePermissionRepository (
            _context);

   

    public IGenderRepository GenderRepository =>
        _genderRepository ??= new GenderRepository(_context, _loggerFactory.CreateLogger<GenderRepository>());

    public ILocationRepository LocationRepository =>
        _locationRepository ??= new LocationRepository(_context, _loggerFactory.CreateLogger<LocationRepository>());

    public IHolidayRepository HolidayRepository =>
        _holidayRepository ??= new HolidayRepository(_context, _loggerFactory.CreateLogger<HolidayRepository>());

    public IInstitutionRepository InstitutionRepository =>
        _institutionRepository ??= new InstitutionRepository(_context, _loggerFactory.CreateLogger<InstitutionRepository>());

    public IForgotPasswordOtpRepository ForgotPasswordOtpRepository =>
        _forgotPasswordOtpRepository ??= new ForgotPasswordOtpRepository(_context, _loggerFactory.CreateLogger<ForgotPasswordOtpRepository>());

    public IPlanModuleMappingRepository PlanModuleMappingRepository =>
        _planModuleMappingRepository ??= new PlanModuleMappingRepository(_context, _loggerFactory.CreateLogger<PlanModuleMappingRepository>());

    public IEmailTemplateRepository EmailTemplateRepository =>
        _emailTemplateRepository ??= new EmailTemplateRepository(_context);

    public IInstitutionEmailTemplateRepository InstitutionEmailTemplateRepository =>
        _institutionEmailTemplateRepository ??= new InstitutionEmailTemplateRepository(_context);

    public ICandidateRegistrationRepository CandidatesRegistrationRepository =>
        _candidateRegistrationRepository ??= new CandidateRegistrationRepository(_context, _loggerFactory.CreateLogger<CandidateRegistrationRepository>());

    public ICandidateCategorySkillRepository CandidateCategorySkillRepository =>
        _candidateCategorySkillRepository ??= new CandidateCategorySkillRepository(_context, _loggerFactory.CreateLogger<CandidateCategorySkillRepository>());

    public IStaffEmploymentTypeRepository StaffEmploymentTypeRepository =>
        _staffEmploymentTypeRepository ??= new StaffEmploymentTypeRepository(_context, _mapper);

    public IStaffCategoryRepository StaffCategoryRepository =>
        _staffCategoryRepository ??= new StaffCategoryRepository(_context);

    public IStaffProfessionalStructureRepository StaffProfessionalStructureRepository =>
        _staffProfessionalStructureRepository ??= new StaffProfessionalStructureRepository(_context);

    public IStaffAcademicStructureRepository StaffAcademicStructureRepository =>
        _staffAcademicStructureRepository ??= new StaffAcademicStructureRepository(_context);

    public IClientRepository ClientsRepository =>
        _clientRepository ??= new ClientRepository(_context, _loggerFactory.CreateLogger<ClientRepository>());

    public IInstitutionSubscriptionRepository InstitutionSubscriptionRepository =>
        _institutionSubscriptionRepository ??= new InstitutionSubscriptionRepository(_context, _loggerFactory.CreateLogger<InstitutionSubscriptionRepository>(), _mapper);

    public IRefreshTokenRepository RefreshTokenRepository =>
        _refreshTokenRepository ??= new RefreshTokenRepository(_context, _loggerFactory.CreateLogger<RefreshTokenRepository>());

    public ISubscriptionRepository SubscriptionRepository =>
        _subscriptionRepository ??= new SubscriptionRepository(_context, _loggerFactory.CreateLogger<SubscriptionRepository>(), _mapper);

    public IStaffEmploymentTypeBasicMenuRepository StaffEmploymentTypeBasicMenuRepository =>
        _staffEmploymentTypeBasicMenuRepository ??= new StaffEmploymentTypeBasicMenuRepository(_context, _loggerFactory.CreateLogger<StaffEmploymentTypeBasicMenuRepository>(), _mapper);

    public IStoreProcedureRepository StoreProcedureRepository =>
        _commonRepository ??= new StoreProcedureRepository(_context, _loggerFactory.CreateLogger<StoreProcedureRepository>(), _mapper);

    public IModuleRepository ModuleRepository =>
        _moduleRepository ??= new ModuleRepository(_context, _loggerFactory.CreateLogger<ModuleRepository>(), _mapper);

    public IAssetRepository AssetRepository =>
        _assetRepository ??= new AssetRepository(_context, _loggerFactory.CreateLogger<AssetRepository>(), _qrService, _fileStorageService, _mapper);

    public ITravelRepository TravelRepository =>
        _travelRepository ??= new TravelRepository(_context, _loggerFactory.CreateLogger<TravelRepository>());

    public IOperationRepository OperationRepository =>
        _operationRepository ??= new OperationRepository(_context, _loggerFactory.CreateLogger<OperationRepository>());

    public ICategoryRepository CategoryRepository =>
        _categoryRepository ??= new CategoryRepository(_context, _loggerFactory.CreateLogger<CategoryRepository>());

    public IUserRoleRepository UserRoleRepository =>
        _userRoleRepository ??= new UserRoleRepository(_context, _loggerFactory.CreateLogger<UserRoleRepository>());

    public IUserRolesPermissionOnModuleRepository UserRolesPermissionOnModuleRepository =>
        _userRolesPermissionOnModuleRepository ??= new UserRolesPermissionOnModuleRepository(_context, _loggerFactory.CreateLogger<UserRolesPermissionOnModuleRepository>());

    public ILeaveRepository LeaveRepository =>
        _leaveRepository ??= new LeaveRepository(_context, _loggerFactory.CreateLogger<LeaveRepository>());

    public IWorkflowStagesRepository WorkflowStagesRepository =>
        _workflowStagesRepository ??= new WorkflowStageRepository(_context, _loggerFactory.CreateLogger<WorkflowStageRepository>(), _mapper);

    public IInstitutionEncryptionKeyRepository InstitutionEncryptionKeyRepository =>
        _institutionEncryptionKeyRepository ??= new InstitutionEncryptionKeyRepository(_context, _loggerFactory.CreateLogger<InstitutionEncryptionKeyRepository>());

    public IInstitutionModuleConfigurationRepository InstitutionModuleConfigurationRepository =>
        _institutionModuleConfigurationRepository ??= new InstitutionModuleConfigurationRepository(_context, _loggerFactory.CreateLogger<InstitutionModuleConfigurationRepository>(), _mapper);

    public IInstitutionParentModuleRepository InstitutionParentModuleRepository =>
        _institutionParentModuleRepository ??= new InstitutionParentModuleRepository(_context);

    public IReportingTypeRepository ReportingTypeRepository =>
        _reportingTypeRepository ??= new ReportingTypeRepository(_context, _loggerFactory.CreateLogger<ReportingTypeRepository>(), _mapper);

    public IAssetStatusRepository AssetStatusRepository =>
        _assetStatusRepository ??= new AssetStatusRepository(_context, _loggerFactory.CreateLogger<AssetStatusRepository>());

    public IAssetCategoryRepository AssetCategoryRepository =>
        _assetCategoryRepository ??= new AssetCategoryRepository(_context, _loggerFactory.CreateLogger<AssetCategoryRepository>());

    public IInstitutionEmailConfigRepository InstitutionEmailConfigRepository =>
        _institutionEmailConfigRepository ??= new InstitutionEmailConfigRepository(_context, _loggerFactory.CreateLogger<InstitutionEmailConfigRepository>());

    public IDefaultEmailConfigRepository DefaultEmailConfigRepository =>
        _defaultEmailConfigRepository ??= new DefaultEmailConfigRepository(
            _context,
            _loggerFactory.CreateLogger<DefaultEmailConfigRepository>());

    public IDesignationRepository DesignationRepository =>
        _designationRepository ??= new DesignationRepository(_context, _loggerFactory.CreateLogger<DesignationRepository>(), _mapper);

    public IDepartmentRepository DepartmentRepository =>
        _departmentRepository ??= new DepartmentRepository(_context, _loggerFactory.CreateLogger<DepartmentRepository>(), _mapper, _encriptionService);

    public IRoleRepository RoleRepository =>
        _roleRepository ??= new RoleRepository(_context, _loggerFactory.CreateLogger<RoleRepository>(), _mapper, _encriptionService);

    public ITicketClassificationRepository TicketClassificationRepository =>
        _ticketClassificationRepository ??= new TicketClassificationRepository(_context, _loggerFactory.CreateLogger<TicketClassificationRepository>(), _mapper);
    public IThreadMessageRepository ThreadMessageRepository =>
    _threadMessageRepository ??= new ThreadMessageRepository(_context, _loggerFactory.CreateLogger<ThreadMessageRepository>(), _mapper);
   
    public ITicketHistoryRepository TicketHistoryRepository =>
    _ticketHistoryRepository ??= new TicketHistoryRepository(_context, _loggerFactory.CreateLogger<TicketHistoryRepository>(), _mapper);
   
    public ITicketGenrationRepository TicketGenrationRepository =>
    _ticketRepository ??= new TicketGenrationRepository(_context, _loggerFactory.CreateLogger<TicketGenrationRepository>(), _mapper);
   
    public IAssetTypeRepository AssetTypeRepository =>
        _assetTypeRepository ??= new AssetTypeRepository(_context, _loggerFactory.CreateLogger<AssetTypeRepository>());

    public ITicketHeaderRepository TicketHeaderRepository =>
        _ticketHeaderRepository ??= new TicketHeaderRepository(_context, _loggerFactory.CreateLogger<TicketHeaderRepository>(), _mapper);

    public IStaffMemberDependentRepository StaffMemberDependentRepository => 
        _staffMemberDependentRepository ??= new StaffMemberDependentRepository(
            _context,
            _mapper,
            _loggerFactory.CreateLogger<StaffMemberDependentRepository>(),
            _passwordService,
            _encriptionService,_fileStorageService);
    public ITicketThreadRepository TicketThreadRepository =>
   _ticketThreadRepository ??= new TicketThreadRepository(_context, _loggerFactory.CreateLogger<TicketThreadRepository >(), _mapper);
    public ITicketAttachmentRepository TicketAttachmentRepository =>
   _ticketAttachmentRepository ??= new TicketAttachmentRepository(_context, _loggerFactory.CreateLogger<TicketAttachmentRepository>(), _mapper);

    #region Institution Configuration

    public ICampusRepository CampusRepository =>
        _campusRepository ??= new CampusRepository(_context);

    public IAttendancePolicyRepository AttendancePolicyRepository =>
        _attendancePolicyRepository ??= new AttendancePolicyRepository(_context);

    public IStaffMemberLocationAssignmentRepository StaffMemberLocationAssignmentRepository =>
        _staffMemberLocationAssignmentRepository ??= new StaffMemberLocationAssignmentRepository(_context);

    public IStaffMemberDeviceEnrollmentRepository StaffMemberDeviceEnrollmentRepository =>
        _staffMemberDeviceEnrollmentRepository ??= new StaffMemberDeviceEnrollmentRepository(_context);

    public IInstitutionCardMasterRepository InstitutionCardMasterRepository =>
        _institutionCardMasterRepository ??= new InstitutionCardMasterRepository(_context);

    public IStaffMemberWorkArrangementRepository StaffMemberWorkArrangementRepository =>
        _staffMemberWorkArrangementRepository ??= new StaffMemberWorkArrangementRepository(_context);

    public IStaffMemberWorkPatternRepository StaffMemberWorkPatternRepository =>
        _staffMemberWorkPatternRepository ??= new StaffMemberWorkPatternRepository(_context);

    public IStaffMemberWorkModeOverrideRequestRepository StaffMemberWorkModeOverrideRequestRepository =>
        _staffMemberWorkModeOverrideRequestRepository ??= new StaffMemberWorkModeOverrideRequestRepository(_context);

    #endregion

    #region Host Device Management

    /// <summary>Gets the Host-managed DeviceMaster catalog repository.</summary>
    public IDeviceMasterRepository DeviceMasterRepository =>
        _deviceMasterRepository ??= new DeviceMasterRepository(_context);

    /// <summary>Gets the Host-managed physical InstitutionDevice repository.</summary>
    public IInstitutionDeviceRepository InstitutionDeviceRepository =>
        _institutionDeviceRepository ??= new InstitutionDeviceRepository(_context);

    /// <summary>Gets the Institution device connection-configuration repository.</summary>
    public IInstitutionDeviceConfigurationRepository InstitutionDeviceConfigurationRepository =>
        _institutionDeviceConfigurationRepository ??= new InstitutionDeviceConfigurationRepository(_context);

    #endregion

    public ICompilanceRuleRepository CompilanceRuleRepository =>
        _compilanceRuleRepository ??= new skolenza.infrastructure.Repositories.CompilanceRuleRepository(
            _context,
            _loggerFactory.CreateLogger<skolenza.infrastructure.Repositories.CompilanceRuleRepository>());

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _currentTransaction?.Dispose();
        _context.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task SafeRollbackInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while rolling back transaction.");
            throw;
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
   
 

}
