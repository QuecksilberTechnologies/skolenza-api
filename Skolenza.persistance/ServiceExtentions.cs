using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IContext;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IRealTimeNotification;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;

using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using skolenza.persistance.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance
{
    /// <summary>
    /// Registers persistence-layer database services and repositories.
    /// </summary>
    public static class ServiceExtentions
    {
        /// <summary>
        /// Adds persistence services, including the read-only real-time connection membership resolver.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Connection String is null or empty!");

            // common options
            Action<DbContextOptionsBuilder> dbOptions = options =>
                options.UseNpgsql(connectionString)
                       .EnableDetailedErrors()
                       .EnableSensitiveDataLogging()
                       .LogTo(Console.WriteLine, LogLevel.Information);

            // ✔ Scoped DbContext
            services.AddDbContext<SkolenzaDbContext>(dbOptions);


            // ✔ Interface mapping
            services.AddScoped<ISkolenzaDbContext>(provider =>
                provider.GetRequiredService<SkolenzaDbContext>());



            //// ✅ Connection string check
            //var connectionString = configuration.GetConnectionString("DefaultConnection");
            //if (string.IsNullOrEmpty(connectionString))
            //    throw new Exception("Connection String is null or empty!");

            //// ✅ Register DbContextFactory for safe multi-threading
            //services.AddDbContextFactory<SkolenzaDbContext>(options =>
            //    options.UseNpgsql(connectionString)
            //           .EnableSensitiveDataLogging()
            //           .EnableDetailedErrors()
            //           .LogTo(Console.WriteLine, LogLevel.Information));

            //// ✅ Register DbContext using AddDbContextPool instead of AddDbContext
            //// DbContextPool is safe for root-level access and prevents "Cannot resolve scoped service" errors
            //// This solves the issue where Swagger/singleton middleware tries to access DbContext
            //services.AddDbContextPool<SkolenzaDbContext>(options =>
            //    options.UseNpgsql(connectionString)
            //           .EnableSensitiveDataLogging()
            //           .EnableDetailedErrors()
            //           .LogTo(Console.WriteLine, LogLevel.Information),
            //    poolSize: 128);



            // ✅ Register ISkolenzaDbContext (Interface based usage)
            // Use direct implementation registration to avoid resolving scoped DbContext from the root provider
            // which can cause "Cannot resolve scoped service ... from root provider" errors when a singleton
            // tries to capture the interface during startup.
            services.AddScoped<ISkolenzaDbContext, SkolenzaDbContext>();

            #region Real-time connection membership

            services.AddScoped<IRealTimeConnectionMembershipResolver, RealTimeConnectionMembershipResolver>();

            #endregion

            // ✅ Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBulkImportRepository, BulkImportRepository>();

            // ✅ All Repositories as Scoped
            services.AddScoped<IStoreProcedureRepository, StoreProcedureRepository>();
            services.AddScoped<IInstitutionEncryptionKeyRepository, InstitutionEncryptionKeyRepository>();

            
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IBaseStaffMemberRepository, BaseStaffMemberRepository>();
 
            services.AddScoped<IUserLoginReopsitory, UserLoginReopsitory>();
            services.AddScoped<ILeaveRepository, LeaveRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IStaffEmploymentTypeRepository, StaffEmploymentTypeRepository>();
            services.AddScoped<IStaffCategoryRepository, StaffCategoryRepository>();
            services.AddScoped<IStaffProfessionalStructureRepository, StaffProfessionalStructureRepository>();
            services.AddScoped<IStaffAcademicStructureRepository, StaffAcademicStructureRepository>();
            services.AddScoped<IStudentDocumentRequirementRepository, StudentDocumentRequirementRepository>();
            services.AddScoped<IStudentFeeMasterRepository, StudentFeeMasterRepository>();
            services.AddScoped<IStudentBenefitSchemeRepository, StudentBenefitSchemeRepository>();
            services.AddScoped<IStudentEducationIdentifierMasterRepository, StudentEducationIdentifierMasterRepository>();
            services.AddScoped<IInstitutionPortalDomainMasterRepository, InstitutionPortalDomainMasterRepository>();
            services.AddScoped<IStudentEducationIdentifierRepository, StudentEducationIdentifierRepository>();
            services.AddScoped<IStudentRegistryRepository, StudentRegistryRepository>();
            services.AddScoped<IStudentPortalAuthRepository, StudentPortalAuthRepository>();
            services.AddScoped<IStudentPortalDocumentRepository, StudentPortalDocumentRepository>();
            services.AddScoped<IStudentAdmissionDocumentRepository, StudentAdmissionDocumentRepository>();
            services.AddScoped<IStaffEmploymentTypeBasicMenuRepository, StaffEmploymentTypeBasicMenuRepository>();
            services.AddScoped<IUserRolesPermissionOnModuleRepository, UserRolesPermissionOnModuleRepository>();
            services.AddScoped<IStaffMemberExperienceRepository, StaffMemberExpereinceRepository>();
            services.AddScoped<IStaffMemberExperienceDetailRepository, StaffMemberExperienceDetailRepository>();
            services.AddScoped<IStaffMemberExperienceDocumentRepository, StaffMemberExperienceDocumentRepository>();
            services.AddScoped<ICandidateRegistrationRepository, CandidateRegistrationRepository>();
            services.AddScoped<IReportingTypeRepository, ReportingTypeRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<ITravelRepository, TravelRepository>();
            services.AddScoped<IOperationRepository, OperationRepository>();
            services.AddScoped<ICandidateCategorySkillRepository, CandidateCategorySkillRepository>();
            services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
            services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
            services.AddScoped<IInstitutionEmailTemplateRepository, InstitutionEmailTemplateRepository>();
            services.AddScoped<IInstitutionRepository, InstitutionRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IInstitutionEmailConfigRepository, InstitutionEmailConfigRepository>();
            services.AddScoped<IDefaultEmailConfigRepository, DefaultEmailConfigRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IHostBillingConfigurationRepository, HostBillingConfigurationRepository>();
            services.AddScoped<IHostCountryCurrencyMasterRepository, HostCountryCurrencyMasterRepository>();
            services.AddScoped<IStudentFeeLedgerRepository, StudentFeeLedgerRepository>();
            services.AddScoped<IStudentPaymentGatewayConfigurationRepository, StudentPaymentGatewayConfigurationRepository>();
            services.AddScoped<IHostBillingAdministrationRepository, HostBillingAdministrationRepository>();
            services.AddScoped<IInstitutionSubscriptionRepository, InstitutionSubscriptionRepository>();
            services.AddScoped<IPlanModuleMappingRepository, PlanModuleMappingRepository>();
            services.AddScoped<IModuleRepository, ModuleRepository>();
            
            services.AddScoped<IInstitutionModuleConfigurationRepository, InstitutionModuleConfigurationRepository>();
          
            services.AddScoped<ICommonServiceSyncRepository, CommonServiceSyncRepository>();
            services.AddScoped<IInstitutionTypeRepository, InstitutionTypeRepository>();
            services.AddScoped<IGenericPolicyRepository, GenericPolicyRepository>();
            services.AddScoped<ISuperAdminDashboardRepository, SuperAdminDashboardRepository>();
            services.AddScoped<IGenderRepository, GenderRepository>();
            services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
            services.AddScoped<IWorkflowStagesRepository, WorkflowStageRepository>();
             services.AddScoped<IReportingTypeRepository, ReportingTypeRepository>();
             services.AddScoped<ITicketClassificationRepository, TicketClassificationRepository>();
             services.AddScoped<ITicketHeaderRepository, TicketHeaderRepository>();
             services.AddScoped<IInstitutionStaffCodePatternRepository, InstitutionStaffCodePatternRepository>();
             services.AddScoped<ITicketGenrationRepository, TicketGenrationRepository>();
             services.AddScoped<ITicketThreadRepository, TicketThreadRepository>();
             services.AddScoped<IThreadMessageRepository, ThreadMessageRepository>();
             services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
             services.AddScoped<IHostRolePermissionRepository, HostRolePermissionRepository>();
             services.AddScoped<IHostRoleRepository, HostRoleRepository>();
             services.AddScoped<IHostUserRepository, HostUserRepository>();
            services.AddScoped<ICampusRepository, CampusRepository>();
            services.AddScoped<IAttendancePolicyRepository, AttendancePolicyRepository>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IStaffMemberLocationAssignmentRepository, StaffMemberLocationAssignmentRepository>();
            services.AddScoped<IStaffMemberDeviceEnrollmentRepository, StaffMemberDeviceEnrollmentRepository>();
            services.AddScoped<IStaffMemberMonitoringRepository, StaffMemberMonitoringRepository>();
            services.AddScoped<IStaffMemberEnrollmentAtDeviceRepository, StaffMemberEnrollmentAtDeviceRepository>();
            services.AddScoped<IStaffMemberWorkArrangementRepository, StaffMemberWorkArrangementRepository>();
            services.AddScoped<IStaffMemberWorkPatternRepository, StaffMemberWorkPatternRepository>();
            services.AddScoped<IStaffMemberWorkModeOverrideRequestRepository, StaffMemberWorkModeOverrideRequestRepository>();
            services.AddScoped<IDeviceMasterRepository, DeviceMasterRepository>();
            services.AddScoped<IInstitutionDeviceRepository, InstitutionDeviceRepository>();
            services.AddScoped<IDeviceCommandSubmissionService, DeviceCommandRepository>();
            services.AddScoped<IDeviceCommandDispatchStore, DeviceCommandRepository>();
            services.AddScoped<IDeviceHttpsPollingService, DeviceCommandRepository>();
            services.AddScoped<IDeviceInitialProvisioningService, DeviceInitialProvisioningRepository>();
            services.AddScoped<IStaffMemberBankRepository, StaffMemberBankRepository>();
            services.AddScoped<IStaffMemberContactRepository, StaffMemberContactRepository>();
            services.AddScoped<IStaffMemberIdentityRepository, StaffMemberIdentityRepository>();
           
            services.AddScoped<IStaffMemberEducationRepository, StaffMemberEducationRepository>();
            services.AddScoped<IBaseStaffMemberRepository, BaseStaffMemberRepository>();
            services.AddScoped<IStaffMemberDependentRepository, StaffMemberDependentRepository>();
            services.AddScoped<InstitutionEncryptionKeyRepository, InstitutionEncryptionKeyRepository>();
            services.AddScoped<IStaffMemberCompletionRepository, StaffMemberCompletionRepository>();           




            // ✅ Log
            Console.WriteLine("✅ Persistence Layer Configured Successfully.");
        }
    }
}
