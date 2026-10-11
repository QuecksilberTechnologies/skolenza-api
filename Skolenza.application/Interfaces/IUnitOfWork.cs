using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.domain.Entity;

// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository and transaction boundary for application operations.
// ================================================================

namespace skolenza.application.Interfaces
{
    /// <summary>
    /// Defines the repositories and transaction operations available to application handlers.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IModuleRepository ModuleRepository { get; }
        IInstitutionEmailConfigRepository InstitutionEmailConfigRepository { get; }
        IDefaultEmailConfigRepository DefaultEmailConfigRepository { get; }
        IInstitutionStaffCodePatternRepository InstitutionStaffCodePatternRepository { get; }
        IPermissionRepository PermissionRepository { get; }
        ITicketThreadRepository TicketThreadRepository { get; }
        ITicketHistoryRepository TicketHistoryRepository { get; }
        IThreadMessageRepository ThreadMessageRepository { get; }
        IReportingTypeRepository ReportingTypeRepository { get; }
        IAssetCategoryRepository AssetCategoryRepository { get; }
        ITicketClassificationRepository TicketClassificationRepository { get; }
        IStaffMemberManagerMappingRepository StaffMemberManagerMappingRepository { get; }    
        IAssetTypeRepository AssetTypeRepository { get; }
        ITicketHeaderRepository TicketHeaderRepository { get; }
        IWorkflowStagesRepository WorkflowStagesRepository { get; }
        IAssetStatusRepository AssetStatusRepository { get; }
        ITicketTypeRepository TicketTypeRepository { get; }
        ITicketAttachmentRepository TicketAttachmentRepository { get; }
        IUserLoginReopsitory UserLoginRepository { get; }
        IForgotPasswordOtpRepository ForgotPasswordOtpRepository { get; }
        IInstitutionModuleConfigurationRepository InstitutionModuleConfigurationRepository { get; }
        IInstitutionParentModuleRepository InstitutionParentModuleRepository { get; }
        IInstitutionEncryptionKeyRepository InstitutionEncryptionKeyRepository { get; }
        IHolidayRepository HolidayRepository { get; }
        IHostUserRepository HostUserRepository { get; }
        IHostRolePermissionRepository HostRolePermissionRepository { get; }
        IHostRoleRepository HostRoleRepository { get; }
        IDepartmentRepository DepartmentRepository { get; }
        IStoreProcedureRepository StoreProcedureRepository { get; }
        ILocationRepository LocationRepository { get; }
        ISubscriptionRepository SubscriptionRepository { get; }
        IPlanModuleMappingRepository PlanModuleMappingRepository { get; }
        IInstitutionRepository InstitutionRepository { get; }
        ICompilanceRuleRepository CompilanceRuleRepository { get; }
        IInstitutionSubscriptionRepository InstitutionSubscriptionRepository { get; }
        IRefreshTokenRepository RefreshTokenRepository { get; }
        IAssetRepository AssetRepository { get; }
        IOperationRepository OperationRepository { get; }
        IDesignationRepository DesignationRepository { get; }
        IBaseStaffMemberRepository StaffMembers { get; }
        IStaffMemberContactRepository StaffMemberContactRepository { get; }
        IStaffMemberBankRepository StaffMemberBankRepository { get; }
        IStaffMemberEducationRepository StaffMemberEducationRepository { get; }

        IStaffMemberExperienceRepository StaffMemberExperienceRepository { get; }
 
        IStaffMemberExperienceDocumentRepository StaffMemberExperienceDocumentRepository { get; }
        IStaffMemberIdentityRepository StaffMemberIdentityRepository { get; }
       
       
        IStaffMemberDependentRepository StaffMemberDependentRepository { get; }
        ITravelRepository TravelRepository { get; }
        IClientRepository ClientsRepository { get; }
        ICandidateRegistrationRepository CandidatesRegistrationRepository { get; }
        ICandidateCategorySkillRepository CandidateCategorySkillRepository { get; }
        IStaffEmploymentTypeRepository StaffEmploymentTypeRepository { get; }
        IEmailTemplateRepository EmailTemplateRepository { get; }
        IInstitutionEmailTemplateRepository InstitutionEmailTemplateRepository { get; }
        IUserRoleRepository UserRoleRepository { get; }
        ICategoryRepository CategoryRepository { get; }
        IRoleRepository RoleRepository { get; }
        ILeaveRepository LeaveRepository { get; }
        IGenderRepository GenderRepository { get; }
        IStaffEmploymentTypeBasicMenuRepository StaffEmploymentTypeBasicMenuRepository { get; }
        IUserRolesPermissionOnModuleRepository UserRolesPermissionOnModuleRepository { get; }
         ITicketGenrationRepository TicketGenrationRepository { get; }

        #region Institution Configuration

        /// <summary>Gets Institution-location persistence operations.</summary>
        ICampusRepository CampusRepository { get; }
        /// <summary>Gets AttendancePolicy persistence operations.</summary>
        IAttendancePolicyRepository AttendancePolicyRepository { get; }
        /// <summary>Gets staffMember-location-assignment persistence operations.</summary>
        IStaffMemberLocationAssignmentRepository StaffMemberLocationAssignmentRepository { get; }
        /// <summary>Gets staffMember-device-enrollment persistence operations.</summary>
        IStaffMemberDeviceEnrollmentRepository StaffMemberDeviceEnrollmentRepository { get; }
        /// <summary>Gets Host-only Institution card-inventory persistence operations.</summary>
        IInstitutionCardMasterRepository InstitutionCardMasterRepository { get; }
        /// <summary>Gets staffMember-work-arrangement persistence operations.</summary>
        IStaffMemberWorkArrangementRepository StaffMemberWorkArrangementRepository { get; }
        /// <summary>Gets staffMember-work-pattern persistence operations.</summary>
        IStaffMemberWorkPatternRepository StaffMemberWorkPatternRepository { get; }
        /// <summary>Gets staffMember-work-mode-override persistence operations.</summary>
        IStaffMemberWorkModeOverrideRequestRepository StaffMemberWorkModeOverrideRequestRepository { get; }

        #endregion

        #region Host Device Management

        /// <summary>Gets Host-managed DeviceMaster catalog persistence operations.</summary>
        IDeviceMasterRepository DeviceMasterRepository { get; }
        /// <summary>Gets Host-managed physical InstitutionDevice persistence operations.</summary>
        IInstitutionDeviceRepository InstitutionDeviceRepository { get; }
        /// <summary>Gets Institution device connection-configuration persistence operations.</summary>
        IInstitutionDeviceConfigurationRepository InstitutionDeviceConfigurationRepository { get; }

        #endregion
 
 
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
       

          
 

    }
}
