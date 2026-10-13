// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Configures the Skolenza PostgreSQL persistence model.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.DTOS.StoreProcedures.DashboardSummeries;
using skolenza.application.DTOS.Institution;
using skolenza.application.Interfaces.IContext;
using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace skolenza.persistance.Data.Context
{
    /// <summary>
    /// Represents the Skolenza database context and its entity mappings.
    /// </summary>
    public partial class SkolenzaDbContext : DbContext, ISkolenzaDbContext
    {


        public SkolenzaDbContext(DbContextOptions<SkolenzaDbContext> options) : base(options)
        {

        }


        public DbSet<RoleModuleOperationResponseDTO> RoleModuleOperationResponse { get; set; }


        public virtual DbSet<StaffMemberContact> StaffMemberContacts { get; set; }
        public virtual DbSet<District> Districts { get; set; }
        public DbSet<StaffMemberCountResponseStatsSp> StaffMemberCountResponseStatsSp { get; set; }

        public virtual DbSet<Asset> Assets { get; set; }

        public virtual DbSet<AssetAssignment> AssetAssignments { get; set; }

        public virtual DbSet<AssetCategory> AssetCategories { get; set; }

        public virtual DbSet<AssetImage> AssetImages { get; set; }

        public virtual DbSet<AssetStatus> AssetStatuses { get; set; }
        public virtual DbSet<AssetType> AssetTypes { get; set; }

        public virtual DbSet<AssignmentStatus> AssignmentStatuses { get; set; }

        public virtual DbSet<Attendance> Attendances { get; set; }

        public virtual DbSet<AttendanceDeviceType> AttendanceDeviceTypes { get; set; }
        public virtual DbSet<AttendanceRequest> AttendanceRequests { get; set; }

        public virtual DbSet<AttendancePolicy> AttendancePolicies { get; set; }

        public virtual DbSet<BasicMenu> BasicMenus { get; set; }

        public virtual DbSet<Candidate> Candidates { get; set; }

        public virtual DbSet<CandidateCategorySkill> CandidateCategorySkills { get; set; }
        public virtual DbSet<Category> Categories { get; set; }

        public virtual DbSet<Locality> Localities { get; set; }
        public virtual DbSet<LocalityType> LocalityTypes { get; set; }

        public virtual DbSet<Client> Clients { get; set; }

        public virtual DbSet<ClientType> ClientTypes { get; set; }

        public virtual DbSet<Country> Countries { get; set; }

        public virtual DbSet<DataViewStructure> DataViewStructures { get; set; }


        public virtual DbSet<Department> Departments { get; set; }

        public virtual DbSet<DeviceCommand> DeviceCommands { get; set; }

        public virtual DbSet<DeviceCommandResponse> DeviceCommandResponses { get; set; }

        public virtual DbSet<DeviceMessageLog> DeviceMessageLogs { get; set; }

        public virtual DbSet<DeviceCredential> DeviceCredentials { get; set; }

        public virtual DbSet<DeviceInitialProvisioning> DeviceInitialProvisionings { get; set; }

        public virtual DbSet<DeviceMaster> DeviceMasters { get; set; }

        public virtual DbSet<StaffMemberTaxProfile> StaffMemberTaxProfile { get; set; }
        public virtual DbSet<ComplianceRule> ComplianceRule { get; set; }

        public virtual DbSet<ComplianceTypeMaster> ComplianceTypeMaster { get; set; }
        public virtual DbSet<PayrollStaffMember> PayrollStaffMember { get; set; }

        public virtual DbSet<PayrollStaffMemberDetail> PayrollStaffMemberDetail { get; set; }

        public virtual DbSet<PayrollRun> PayrollRun { get; set; }
        public virtual DbSet<SalaryComponentMaster> SalaryComponentMaster { get; set; }

        public virtual DbSet<SalaryStructure> SalaryStructure { get; set; }

        public virtual DbSet<SalaryStructureDetail> SalaryStructureDetail { get; set; }

        public virtual DbSet<TaxRegimeMaster> TaxRegimeMaster { get; set; }

        public virtual DbSet<TaxRule> TaxRule { get; set; }

        public virtual DbSet<TaxSlab> TaxSlab { get; set; }

        public virtual DbSet<TaxSystemMaster> TaxSystemMaster { get; set; }
        public virtual DbSet<Designation> Designations { get; set; }

        public virtual DbSet<EmailQueue> EmailQueues { get; set; }

        public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }
        public virtual DbSet<InstitutionEmailTemplate> InstitutionEmailTemplates { get; set; }
        public virtual DbSet<StaffMember> StaffMembers { get; set; }

        public virtual DbSet<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; }

        public virtual DbSet<StaffMemberMonitoringPolicy> StaffMemberMonitoringPolicies { get; set; }

        public virtual DbSet<StaffMemberMonitoringAgent> StaffMemberMonitoringAgents { get; set; }

        public virtual DbSet<StaffMemberScreenCapture> StaffMemberScreenCaptures { get; set; }

        public virtual DbSet<StaffMemberDeviceAccessWindow> StaffMemberDeviceAccessWindows { get; set; }

        public virtual DbSet<InstitutionCardMaster> InstitutionCardMasters { get; set; }

        public virtual DbSet<StaffMemberLocationAssignment> StaffMemberLocationAssignments { get; set; }

        public virtual DbSet<StaffMemberWorkArrangement> StaffMemberWorkArrangements { get; set; }

        public virtual DbSet<StaffMemberWorkPattern> StaffMemberWorkPatterns { get; set; }

        public virtual DbSet<StaffMemberWorkModeOverrideRequest> StaffMemberWorkModeOverrideRequests { get; set; }

        public virtual DbSet<StaffMemberBankDetail> StaffMemberBankDetails { get; set; }

        public virtual DbSet<StaffMemberCategorySkill> StaffMemberCategorySkills { get; set; }

        public virtual DbSet<StaffMemberDailyAttendance> StaffMemberDailyAttendances { get; set; }
        public virtual DbSet<StaffMemberAttendancePunch> StaffMemberAttendancePunches { get; set; }

        public virtual DbSet<StaffMemberDependent> StaffMemberDependents { get; set; }

        public virtual DbSet<StaffMemberEducation> StaffMemberEducations { get; set; }

        public virtual DbSet<StaffMemberExperience> StaffMemberExperiences { get; set; }
        
        public virtual DbSet<StaffMemberExperience> StaffMemberExperienceDetails { get; set; }

        public virtual DbSet<StaffMemberExperienceDocument> StaffMemberExperienceDocuments { get; set; }

        public virtual DbSet<StaffMemberImage> StaffMemberImages { get; set; }

     //   public virtual DbSet<StaffMemberInsuranceMapping> StaffMemberInsuranceMappings { get; set; }




        public virtual DbSet<StaffMemberManagerMapping> StaffMemberManagerMappings { get; set; }

        public virtual DbSet<StaffMemberPersonalDetail> StaffMemberPersonalDetails { get; set; }

        public virtual DbSet<StaffEmploymentType> StaffEmploymentTypes { get; set; }

        public virtual DbSet<StaffCategory> StaffCategories { get; set; }

        public virtual DbSet<StaffJobFamilyMaster> StaffJobFamilyMasters { get; set; }

        public virtual DbSet<InstitutionStaffCategoryMaster> InstitutionStaffCategoryMasters { get; set; }

        public virtual DbSet<StaffCategoryJobFamily> StaffCategoryJobFamilies { get; set; }

        public virtual DbSet<StaffPositionMaster> StaffPositionMasters { get; set; }

        public virtual DbSet<StaffCapabilityMaster> StaffCapabilityMasters { get; set; }

        public virtual DbSet<StaffDutyDefinitionMaster> StaffDutyDefinitionMasters { get; set; }

        public virtual DbSet<StaffPositionCapability> StaffPositionCapabilities { get; set; }

        public virtual DbSet<StaffPositionDuty> StaffPositionDuties { get; set; }

        public virtual DbSet<StaffPositionAssignment> StaffPositionAssignments { get; set; }

        public virtual DbSet<StaffCapabilityAssignment> StaffCapabilityAssignments { get; set; }

        public virtual DbSet<StaffDutyAssignment> StaffDutyAssignments { get; set; }

        public virtual DbSet<AcademicSession> AcademicSessions { get; set; }
        public virtual DbSet<AcademicProgramme> AcademicProgrammes { get; set; }
        public virtual DbSet<AcademicGroup> AcademicGroups { get; set; }
        public virtual DbSet<ClassSection> ClassSections { get; set; }
        public virtual DbSet<Subject> Subjects { get; set; }
        public virtual DbSet<CourseOffering> CourseOfferings { get; set; }
        public virtual DbSet<TeachingAssignmentTypeMaster> TeachingAssignmentTypes { get; set; }
        public virtual DbSet<StaffTeachingAssignment> StaffTeachingAssignments { get; set; }
        public virtual DbSet<ClassTeacherAssignment> ClassTeacherAssignments { get; set; }
        public virtual DbSet<AcademicWorkflowStatusMaster> AcademicWorkflowStatuses { get; set; }
        public virtual DbSet<CurriculumFrameworkMaster> CurriculumFrameworks { get; set; }
        public virtual DbSet<CurriculumUnit> CurriculumUnits { get; set; }
        public virtual DbSet<CurriculumTopic> CurriculumTopics { get; set; }
        public virtual DbSet<LessonPlan> LessonPlans { get; set; }
        public virtual DbSet<LessonPlanCurriculumTopic> LessonPlanCurriculumTopics { get; set; }
        public virtual DbSet<LessonDeliveryLog> LessonDeliveryLogs { get; set; }
        public virtual DbSet<LessonDeliveryCoverage> LessonDeliveryCoverages { get; set; }
        public virtual DbSet<TimetablePeriodMaster> TimetablePeriodMasters { get; set; }
        public virtual DbSet<TimetableEntry> TimetableEntries { get; set; }
        public virtual DbSet<TimetableWorkflowMaster> TimetableWorkflowMasters { get; set; }
        public virtual DbSet<TimetableWorkflowStep> TimetableWorkflowSteps { get; set; }
        public virtual DbSet<TimetableApprovalHistory> TimetableApprovalHistories { get; set; }
        public virtual DbSet<SubstituteWorkflowMaster> SubstituteWorkflowMasters { get; set; }
        public virtual DbSet<SubstituteWorkflowStep> SubstituteWorkflowSteps { get; set; }
        public virtual DbSet<StaffAbsenceImpact> StaffAbsenceImpacts { get; set; }
        public virtual DbSet<SubstituteAssignment> SubstituteAssignments { get; set; }
        public virtual DbSet<SubstituteApprovalHistory> SubstituteApprovalHistories { get; set; }
        public virtual DbSet<InstitutionCommunicationSetting> InstitutionCommunicationSettings { get; set; }
        public virtual DbSet<StaffQueryCategoryMaster> StaffQueryCategoryMasters { get; set; }
        public virtual DbSet<StaffConversation> StaffConversations { get; set; }
        public virtual DbSet<StaffConversationParticipant> StaffConversationParticipants { get; set; }
        public virtual DbSet<StaffConversationMessage> StaffConversationMessages { get; set; }
        public virtual DbSet<StaffMessageAttachment> StaffMessageAttachments { get; set; }
        public virtual DbSet<StaffBroadcastAudience> StaffBroadcastAudiences { get; set; }

        public virtual DbSet<StaffEmploymentTypeBasicMenu> StaffEmploymentTypeBasicMenus { get; set; }

        public virtual DbSet<StaffMembersChangedTypeHistory> StaffMembersChangedTypeHistories { get; set; }

        public virtual DbSet<ForgotPasswordOtpdetail> ForgotPasswordOtpdetails   { get; set; }

        public virtual DbSet<Gender> Genders { get; set; }
        public virtual DbSet<HostRole> HostRoles { get; set; }

        public virtual DbSet<HostRoleModuleAndPermission> HostRoleModuleAndPermissions { get; set; }

        public virtual DbSet<HostUser> HostUsers { get; set; }

        public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }



        public virtual DbSet<LeaveType> LeaveTypes { get; set; }

        //public virtual DbSet<License> Licenses { get; set; }

        public virtual DbSet<LoginCredential> LoginCredentials { get; set; }


        public virtual DbSet<Module> Modules { get; set; }

        public virtual DbSet<ModuleOperationMapping> ModuleOperationMappings { get; set; }

        public virtual DbSet<Operation> Operations { get; set; }

        public virtual DbSet<Holiday> Holidays { get; set; }

        public virtual DbSet<PageTypeEnum> PageTypeEnums { get; set; }

        public virtual DbSet<PlanModuleMapping> PlanModuleMappings { get; set; }

        public virtual DbSet<StaffMemberIdentity> StaffMemberIdentities { get; set; }

        public virtual DbSet<PolicyType> PolicyTypes { get; set; }

        public virtual DbSet<PolicyCategory> PolicyCategories { get; set; }
        public virtual DbSet<PolicyStatus> PolicyStatuses { get; set; }
        public virtual DbSet<PolicyRuleType> PolicyRuleTypes { get; set; }
        public virtual DbSet<PolicyCategoryRuleType> PolicyCategoryRuleTypes { get; set; }
        public virtual DbSet<PolicyRuleSettingDefinition> PolicyRuleSettingDefinitions { get; set; }
        public virtual DbSet<PolicyRuleSettingOption> PolicyRuleSettingOptions { get; set; }
        public virtual DbSet<PolicyRuleSettingDependency> PolicyRuleSettingDependencies { get; set; }
        public virtual DbSet<PolicyDocumentType> PolicyDocumentTypes { get; set; }
        public virtual DbSet<Policy> Policies { get; set; }
        public virtual DbSet<PolicyVersion> PolicyVersions { get; set; }
        public virtual DbSet<AttendancePolicyVersionConfiguration> AttendancePolicyVersionConfigurations { get; set; }
        public virtual DbSet<PolicyRule> PolicyRules { get; set; }
        public virtual DbSet<PolicyVersionLeaveType> PolicyVersionLeaveTypes { get; set; }
        public virtual DbSet<PolicyRuleLeaveType> PolicyRuleLeaveTypes { get; set; }
        public virtual DbSet<PolicyApplicabilityLeaveType> PolicyApplicabilityLeaveTypes { get; set; }
        public virtual DbSet<PolicyApplicability> PolicyApplicabilities { get; set; }
        public virtual DbSet<PolicyAssignment> PolicyAssignments { get; set; }
        public virtual DbSet<PolicyException> PolicyExceptions { get; set; }
        public virtual DbSet<PolicyDocument> PolicyDocuments { get; set; }
        public virtual DbSet<PolicyApprovalStage> PolicyApprovalStages { get; set; }
        public virtual DbSet<PolicyApprovalHistory> PolicyApprovalHistories { get; set; }
        public virtual DbSet<PolicyAcknowledgement> PolicyAcknowledgements { get; set; }
        public virtual DbSet<PolicyChangeAudit> PolicyChangeAudits { get; set; }

        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        public virtual DbSet<ReportingType> ReportingTypes { get; set; }

        public virtual DbSet<RequestType> RequestTypes { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<RoleModuleAndPermission> RoleModuleAndPermissions { get; set; }

        public virtual DbSet<ServiceProvider> ServiceProviders { get; set; }

        public virtual DbSet<State> States { get; set; }

        public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public virtual DbSet<CountryIdentityRule> CountryIdentityRules { get; set; }
        public virtual DbSet<IdentityCategory> IdentityCategories { get; set; }
        public virtual DbSet<IdentityCategoryDocument> IdentityCategoryDocuments { get; set; }
        public virtual DbSet<Institution> Institutions { get; set; }

        public virtual DbSet<DefaultEmailConfig> DefaultEmailConfigs { get; set; }

        public virtual DbSet<InstitutionDevice> InstitutionDevices { get; set; }

        public virtual DbSet<InstitutionDeviceConfiguration> InstitutionDeviceConfigurations { get; set; }

        public virtual DbSet<Campus> Campuss { get; set; }

        public virtual DbSet<InstitutionEmailConfig> InstitutionEmailConfigs { get; set; }
        public virtual DbSet<TicketHistory> TicketHistories { get; set; }

        public virtual DbSet<InstitutionEnabledModule> InstitutionEnabledModules { get; set; }

        public virtual DbSet<InstitutionEnabledOperation> InstitutionEnabledOperations { get; set; }

        public virtual DbSet<InstitutionType> InstitutionTypes { get; set; }

        public virtual DbSet<InstitutionProfile> InstitutionProfiles { get; set; }

        public virtual DbSet<InstitutionSubscription> InstitutionSubscriptions { get; set; }

        public virtual DbSet<Tender> Tenders { get; set; }
        public virtual DbSet<TenderStatus> TenderStatuses { get; set; }

        public virtual DbSet<TicketClassification> TicketClassifications { get; set; }
        public virtual DbSet<InstitutionEncryptionKeys> InstitutionEncryptionKeys { get; set; }
      

        public virtual DbSet<ThreadMessage> ThreadMessage { get; set; }
        public virtual DbSet<TicketThread> TicketThreads { get; set; }

        public virtual DbSet<Ticket> Ticket { get; set; }

        public virtual DbSet<TicketAttachment> TicketAttachment { get; set; }
        public virtual DbSet<TicketHeader> TicketHeaders { get; set; }

        public virtual DbSet<TicketType> TicketTypes { get; set; }


        public virtual DbSet<TravelMode> TravelModes { get; set; }

        public virtual DbSet<UserAttendanceSetting> UserAttendanceSettings { get; set; }

        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<StaffMemberCodePattern> StaffMemberCodePatterns { get; set; }

        public virtual DbSet<WorkflowStage> WorkflowStages { get; set; }
        public virtual DbSet<WorkstationType> WorkstationTypes { get; set; }
        public virtual DbSet<SubscribedModuleResponseDTO> SubscribedModuleResponseDTOs { get; set; }
        public virtual DbSet<FlatModuleOperationDto> InstitutionModulesConfigurations { get; set; }
        public virtual DbSet<GetStaffMemberIdentitySp> GetStaffMemberIdentitySps { get; set; }
        public virtual DbSet<GetStaffMemberCodePatternResponseDTO> GetStaffMemberCodePatternResponseDTOs { get; set; }
 
        
        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
      

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        //    => optionsBuilder.UseSqlServer("Server=acer;Database=workforcedb-dev-pre;User Id=sa;Password=123;Encrypt=True;TrustServerCertificate=True;Command Timeout=300");

      protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region Compliance Mapping

        modelBuilder.Entity<InstitutionStaffMemberSectionDefault>(entity =>
        {
            entity.ToTable("InstitutionStaffSectionDefault", "skolenza");
            entity.HasKey(item => new { item.InstitutionId, item.ModuleCode });
            entity.Property(item => item.ModuleCode).HasMaxLength(64);
        });

        modelBuilder.Entity<ComplianceTypeMaster>(entity =>
        {
            entity.ToTable("ComplianceTypeMaster", "skolenza");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ComplianceRule>(entity =>
        {
            entity.ToTable("ComplianceRule", "skolenza");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RuleJson).HasColumnType("jsonb");
            entity.HasOne(item => item.ComplianceType)
                .WithMany(item => item.ComplianceRule)
                .HasForeignKey(item => item.ComplianceTypeId);
        });

        #endregion

        modelBuilder.Entity<BulkImportJob>(entity =>
        {
            entity.ToTable("BulkImportJob", "skolenza");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.PreviewJson).HasColumnType("jsonb");
            entity.Property(job => job.InputHash).HasMaxLength(64);
        });


            modelBuilder.Entity<GetStaffMemberCodePatternResponseDTO>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });

            modelBuilder.Entity<HostUserPermissionCheckResponseDTO>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });

            // Keyless stored-function result used by the central Institution
            // runtime module-operation permission validation flow.
            modelBuilder.Entity<InstitutionsUserPermissionCheckResponseDTO>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });


            modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Asset__3214EC076178ABAE");

            entity.ToTable("Asset", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.AssetName).HasMaxLength(100);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAssigned).HasDefaultValue(false);
            entity.Property(e => e.IsRepairable).HasDefaultValue(true);
            entity.Property(e => e.ModelNo).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Qrcode)
                .HasMaxLength(500)
                .HasColumnName("qrcode");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.SoftDeletedById).HasDefaultValue(0L);
            entity.Property(e => e.Weight).HasMaxLength(50);

            entity.HasOne(d => d.AssetStatus).WithMany(p => p.Asset)
                .HasForeignKey(d => d.AssetStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asset_AssetStatus");

            entity.HasOne(d => d.AssetType).WithMany(p => p.Asset)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asset_AssetType");
        });

        modelBuilder.Entity<AssetAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AssetAssignment_pkey");

            entity.ToTable("AssetAssignment", "skolenza");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"AssetAssignment_Id_seq\"'::regclass)");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.AssetConditionAtAssign).HasMaxLength(255);
            entity.Property(e => e.AssetConditionAtReturn).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetAssignment)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK_AssetAssignment_Asset");

            entity.HasOne(d => d.Request).WithMany(p => p.AssetAssignment)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetAssignment_Request");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetCat__3214EC07E9FE2792");

            entity.ToTable("AssetCategory", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CategoryName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasOne(d => d.Institution).WithMany(p => p.AssetCategory)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetCategory_Institution");
        });

        modelBuilder.Entity<AssetImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetIma__3214EC0752335BEC");

            entity.ToTable("AssetImage", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.AssetImagePath).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetImage)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetImage_Asset");
        });

        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AssetRequest_pkey");

            entity.ToTable("AssetRequest", "skolenza");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"AssetRequest_Id_seq\"'::regclass)");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.RequestDate).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AssetStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetSta__3214EC0722A59445");

            entity.ToTable("AssetStatus", "skolenza");

            entity.HasIndex(e => e.StatusName, "UQ__AssetSta__05E7698A7AA7A28E").IsUnique();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.ColorKey).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.SoftDeletedById).HasDefaultValue(0L);
            entity.Property(e => e.StatusName).HasMaxLength(50);
            entity.Property(e => e.UpdatedById).HasDefaultValue(0L);
        });

            modelBuilder.Entity<AssetType>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__AssetTyp__3214EC077375A9AA");

                entity.ToTable("AssetType", "skolenza");

                entity.HasIndex(e => e.TypeName, "UQ__AssetTyp__D4E7DFA8692FD5DF").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.TypeName).HasMaxLength(100);

                entity.HasOne(d => d.AssetCategory).WithMany(p => p.AssetType)
                    .HasForeignKey(d => d.AssetCategoryId)
                    .HasConstraintName("FK_AssetType_AssetCategory");
            });

            modelBuilder.Entity<AssignmentStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assignme__3214EC07BCFD76FA");

            entity.ToTable("AssignmentStatus", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC079EEE2ABB");

            entity.ToTable("Attendance", "skolenza");

            entity.Property(e => e.AttendanceImagePath).HasMaxLength(200);
            entity.Property(e => e.AttendanceImageUrl)
                .HasMaxLength(200)
                .HasColumnName("AttendanceImageURL");
        });

        modelBuilder.Entity<AttendanceDeviceType>(entity =>
        {
            entity.ToTable("AttendanceDeviceType", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.DeviceTypeCode).HasMaxLength(30);
            entity.HasIndex(e => e.DeviceTypeCode).IsUnique();
            entity.Property(e => e.IsDeviceRegister).HasDefaultValue(false);
            entity.Property(e => e.Remark).HasMaxLength(255);
        });

        modelBuilder.Entity<AttendanceRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC07DA4D2CA6");

            entity.ToTable("AttendanceRequest", "skolenza");

            entity.Property(e => e.AttendanceImagePath).HasMaxLength(200);
            entity.Property(e => e.AttendanceImageUrl)
                .HasMaxLength(200)
                .HasColumnName("AttendanceImageURL");
            entity.Property(e => e.Remark).HasMaxLength(255);
        });

        modelBuilder.Entity<BasicMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BasicMenu_Id");

            entity.ToTable("BasicMenu", "skolenza");

            entity.Property(e => e.ImageIcon).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MenuName).HasMaxLength(100);
            entity.Property(e => e.MenuUrl)
                .HasMaxLength(255)
                .HasColumnName("MenuURL");
            entity.Property(e => e.Remark).HasMaxLength(200);

            entity.HasOne(d => d.ParentMenu).WithMany(p => p.InverseParentMenu)
                .HasForeignKey(d => d.ParentMenuId)
                .HasConstraintName("FK_BasicMenu_ParentMenu");
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Candidat__3214EC078AAEC326");

            entity.ToTable("Candidate", "skolenza");

            entity.HasIndex(e => e.PhoneNumber, "UQ__Candidat__85FB4E384EF74606").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Candidat__A9D10534B67C36A0").IsUnique();

            entity.HasIndex(e => e.Aadhaar, "UQ__Candidat__C4B3336970636589").IsUnique();

            entity.HasIndex(e => e.Pan, "UQ__Candidat__C577943D11665D42").IsUnique();

            entity.HasIndex(e => e.CandidateReferenceCode, "UQ__Candidat__CF22B81A936408F4").IsUnique();

            entity.Property(e => e.Aadhaar).HasMaxLength(12);
            entity.Property(e => e.ActionStatus).HasMaxLength(20);
            entity.Property(e => e.AppliedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CandidateReferenceCode).HasMaxLength(20);
            entity.Property(e => e.CurrentCompany).HasMaxLength(200);
            entity.Property(e => e.CurrentLocation).HasMaxLength(200);
            entity.Property(e => e.Education).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.ExpectedSalary).HasPrecision(18, 2);
            entity.Property(e => e.ExperienceYears).HasPrecision(4, 1);
            entity.Property(e => e.FewWords).HasMaxLength(1000);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.LastUpdatedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.Pan)
                .HasMaxLength(10)
                .HasColumnName("PAN");
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.ResumePath).HasMaxLength(200);
            entity.Property(e => e.ResumeUrl)
                .HasMaxLength(200)
                .HasColumnName("ResumeURL");
        });

        modelBuilder.Entity<CandidateCategorySkill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Candidat__3214EC07853F4240");

            entity.ToTable("CandidateCategorySkill", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateCategorySkill)
                .HasForeignKey(d => d.CandidateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Candidate__Candi__15DA3E5D");

            entity.HasOne(d => d.Category).WithMany(p => p.CandidateCategorySkill)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Candidate__Categ__16CE6296");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3214EC070D4D0C80");

            entity.ToTable("Category", "skolenza");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.Remark).HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(255);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Category_Parent");
        });

        modelBuilder.Entity<Locality>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Locality");

            entity.ToTable("Locality", "skolenza");

            entity.Property(e => e.LocalityName).HasMaxLength(100);
            entity.Property(e => e.LocalityCode).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.HasIndex(e => new { e.DistrictId, e.LocalityCode }).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.State).WithMany(p => p.Localities)
                .HasForeignKey(d => d.StateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Locality_State");
            entity.HasOne(d => d.District).WithMany(p => p.Localities)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Locality_District");
            entity.HasOne(d => d.LocalityType).WithMany(p => p.Localities)
                .HasForeignKey(d => d.LocalityTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Locality_LocalityType");
        });

        modelBuilder.Entity<LocalityType>(entity =>
        {
            entity.ToTable("LocalityType", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_LocalityType");
            entity.HasIndex(e => e.TypeName).IsUnique();
            entity.Property(e => e.TypeName).HasMaxLength(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_TenderClient");

            entity.ToTable("Client", "skolenza");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.ClientName).HasMaxLength(255);
            entity.Property(e => e.ContactPerson).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Remark).HasMaxLength(255);

            entity.HasOne(d => d.ClientType).WithMany(p => p.Client)
                .HasForeignKey(d => d.ClientTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Client_ClientType");
        });

        modelBuilder.Entity<ClientType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClientTy__3214EC078D984A16");

            entity.ToTable("ClientType", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Country__3214EC070584DCC0");

            entity.ToTable("Country", "skolenza");

            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.CountryName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Stdcode)
                .HasMaxLength(10)
                .HasColumnName("STDCode");
        });

        modelBuilder.Entity<CountryIdentityRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CountryI__3214EC0748E4A649");

            entity.ToTable("CountryIdentityRule", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsMandatory).HasDefaultValue(true);

            entity.HasOne(d => d.Country).WithMany(p => p.CountryIdentityRule)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Country");

            entity.HasOne(d => d.IdentityCategoryDocument).WithMany(p => p.CountryIdentityRule)
                .HasForeignKey(d => d.IdentityCategoryDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Country_Document");
        });

        modelBuilder.Entity<CountryStatutoryRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CountryS__3214EC07068D8E4F");

            entity.ToTable("CountryStatutoryRule", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SalaryThreshold).HasPrecision(18, 2);

            entity.HasOne(d => d.Country).WithMany(p => p.CountryStatutoryRule)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CountryStatutoryRule_Country");

            entity.HasOne(d => d.StatutoryType).WithMany(p => p.CountryStatutoryRule)
                .HasForeignKey(d => d.StatutoryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CountryStatutoryRule_Statutory");
        });

        modelBuilder.Entity<DataViewStructure>(entity =>
        {
            entity.ToTable("DataViewStructure", "skolenza");

            entity.Property(e => e.Discription).HasMaxLength(150);
            entity.Property(e => e.DisplayOn).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(150);
        });


        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC071E7B0B0B");

            entity.ToTable("Department", "skolenza");

            entity.Property(e => e.DepartmentName).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(200);

            entity.HasOne(d => d.Institution).WithMany(p => p.Department)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_Department_Institution");
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Designat__3214EC07F9FF4C75");

            entity.ToTable("Designation", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DesignationName).HasMaxLength(255);

            entity.HasOne(d => d.Department).WithMany(p => p.Designation)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Designation_Department");

            entity.HasOne(d => d.Institution).WithMany(p => p.Designation)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Designation_Institution");
        });

        modelBuilder.Entity<DeviceCommand>(entity =>
        {
            entity.ToTable("DeviceCommand", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_DeviceCommand");
            entity.HasIndex(e => e.InternalTrackingId, "UX_DeviceCommand_InternalTrackingId").IsUnique();
            entity.HasIndex(e => new { e.Status, e.NextAttemptDateTime, e.AddedDateTime }, "IX_DeviceCommand_Dispatch");
            entity.HasIndex(e => new { e.DeviceSerialNumber, e.AddedDateTime }, "IX_DeviceCommand_DeviceSerial_Order");
            entity.HasIndex(e => e.InstitutionId, "IX_DeviceCommand_InstitutionId");
            entity.HasIndex(e => e.CampusId, "IX_DeviceCommand_CampusId");
            entity.HasIndex(e => e.DeviceSerialNumber, "UX_DeviceCommand_OneOutstandingSerial")
                .IsUnique()
                .HasFilter("\"Status\" IN (2, 3, 6)");
            entity.Property(e => e.DeviceSerialNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CommandName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RequestPayload).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.MatchCriteria).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasDefaultValue((short)DeviceCommandStatus.Queued);
            entity.Property(e => e.ResponseMode).HasDefaultValue((short)DeviceCommandResponseMode.Required);
            entity.Property(e => e.AccessLevel).HasDefaultValue((short)DeviceCommandAccessLevel.HostOnly);
            entity.Property(e => e.AttemptCount).HasDefaultValue(0);
            entity.Property(e => e.MaxAttempts).HasDefaultValue(3);
            entity.Property(e => e.FailureReason).HasMaxLength(2000);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommand_Institution");
            entity.HasOne(e => e.InstitutionDevice).WithMany().HasForeignKey(e => e.InstitutionDeviceId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommand_InstitutionDevice");
            entity.HasOne(e => e.Campus).WithMany().HasForeignKey(e => e.CampusId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommand_Campus");
        });

        modelBuilder.Entity<DeviceCommandResponse>(entity =>
        {
            entity.ToTable("DeviceCommandResponse", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_DeviceCommandResponse");
            entity.HasIndex(e => e.DeviceCommandId, "UX_DeviceCommandResponse_DeviceCommand")
                .IsUnique().HasFilter("\"DeviceCommandId\" IS NOT NULL");
            entity.HasIndex(e => new { e.InstitutionDeviceId, e.ReceivedDateTime }, "IX_DeviceCommandResponse_InstitutionDevice_Received");
            entity.Property(e => e.DeviceSerialNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResponseCommandName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(2000);
            entity.Property(e => e.ResponsePayload).HasColumnType("text").IsRequired();
            entity.Property(e => e.ReceivedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.DeviceCommand).WithMany(e => e.DeviceCommandResponses).HasForeignKey(e => e.DeviceCommandId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommandResponse_DeviceCommand");
            entity.HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommandResponse_Institution");
            entity.HasOne(e => e.InstitutionDevice).WithMany().HasForeignKey(e => e.InstitutionDeviceId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCommandResponse_InstitutionDevice");
        });

        modelBuilder.Entity<DeviceMessageLog>(entity =>
        {
            entity.ToTable("DeviceMessageLog", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_DeviceMessageLog");
            entity.HasIndex(e => new { e.InstitutionDeviceId, e.OccurredDateTime }, "IX_DeviceMessageLog_InstitutionDevice_Occurred");
            entity.HasIndex(e => new { e.Topic, e.PayloadHash }, "IX_DeviceMessageLog_Topic_PayloadHash");
            entity.Property(e => e.DeviceSerialNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Topic).HasMaxLength(300).IsRequired();
            entity.Property(e => e.PayloadHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.RawPayload).HasColumnType("text").IsRequired();
            entity.Property(e => e.Direction).HasDefaultValue((short)DeviceMessageDirection.Inbound);
            entity.Property(e => e.QualityOfService).HasDefaultValue(1);
            entity.Property(e => e.IsDuplicateDelivery).HasDefaultValue(false);
            entity.Property(e => e.OccurredDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceMessageLog_Institution");
            entity.HasOne(e => e.InstitutionDevice).WithMany().HasForeignKey(e => e.InstitutionDeviceId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceMessageLog_InstitutionDevice");
        });

        modelBuilder.Entity<DeviceCredential>(entity =>
        {
            entity.ToTable("DeviceCredential", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_DeviceCredential");
            entity.HasIndex(e => new { e.InstitutionDeviceId, e.CredentialType }, "UX_DeviceCredential_ActiveType")
                .IsUnique().HasFilter("\"IsActive\" = true");
            entity.Property(e => e.UserName).HasMaxLength(300);
            entity.Property(e => e.SecretEncrypted).HasColumnType("text");
            entity.Property(e => e.CertificateReference).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.InstitutionDevice).WithMany().HasForeignKey(e => e.InstitutionDeviceId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceCredential_InstitutionDevice");
        });

        modelBuilder.Entity<DeviceInitialProvisioning>(entity =>
        {
            entity.ToTable("DeviceInitialProvisioning", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_DeviceInitialProvisioning");
            entity.HasIndex(e => e.IngressTokenHash, "UX_DeviceInitialProvisioning_IngressTokenHash")
                .IsUnique();
            entity.HasIndex(e => new { e.DeviceMasterId, e.RevokedDateTime, e.ExpiresDateTime }, "IX_DeviceInitialProvisioning_DeviceMaster_Active");
            entity.Property(e => e.IngressTokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.HeartbeatIntervalSeconds).HasDefaultValue(20);
            entity.Property(e => e.IssuedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.DeviceMaster).WithMany().HasForeignKey(e => e.DeviceMasterId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_DeviceInitialProvisioning_DeviceMaster");
        });

        modelBuilder.Entity<DeviceMaster>(entity =>
        {
            entity.ToTable("DeviceMaster", "skolenza");
            entity.HasIndex(e => e.CompanyName, "IX_DeviceMaster_CompanyName");
            entity.HasIndex(e => e.DeviceType, "IX_DeviceMaster_DeviceType");
            entity.HasIndex(e => new { e.IsActive, e.IsSoftDeleted }, "IX_DeviceMaster_IsActive_IsSoftDeleted");
            entity.HasIndex(e => e.IsAttendanceDevice, "IX_DeviceMaster_IsAttendanceDevice");
            entity.HasIndex(e => e.IsIntegrationSupported, "IX_DeviceMaster_IsIntegrationSupported");
            entity.HasIndex(e => e.ModelNo, "IX_DeviceMaster_ModelNo");
            entity.Property(e => e.SNo).HasColumnName("SNo").HasMaxLength(100).IsRequired();
            entity.Property(e => e.DeviceCode).HasMaxLength(50);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.ModelNo).HasMaxLength(100);
            entity.Property(e => e.ProductCode).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(150);
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.CountryOfOrigin).HasMaxLength(100);
            entity.Property(e => e.ManufacturerWebsite).HasMaxLength(300);
            entity.Property(e => e.SupportEmail).HasMaxLength(200);
            entity.Property(e => e.SupportContactNumber).HasMaxLength(30);
            entity.Property(e => e.SupportedTlsVersions).HasMaxLength(100);
            entity.Property(e => e.SupportedProtocols).HasMaxLength(500);
            entity.Property(e => e.FirmwareVersion).HasMaxLength(100);
            entity.Property(e => e.SoftwareVersion).HasMaxLength(100);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.ProcessorInfo).HasMaxLength(255);
            entity.Property(e => e.MemoryInfo).HasMaxLength(255);
            entity.Property(e => e.StorageInfo).HasMaxLength(255);
            entity.Property(e => e.DisplayInfo).HasMaxLength(255);
            entity.Property(e => e.CameraInfo).HasMaxLength(255);
            entity.Property(e => e.SensorInfo).HasMaxLength(255);
            entity.Property(e => e.PowerRequirement).HasMaxLength(150);
            entity.Property(e => e.OperatingTemperature).HasMaxLength(100);
            entity.Property(e => e.Dimensions).HasMaxLength(150);
            entity.Property(e => e.Weight).HasMaxLength(100);
            entity.Property(e => e.Features).HasColumnType("jsonb");
            entity.Property(e => e.TechnicalSpecifications).HasColumnType("jsonb");
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.CurrencyKey).HasMaxLength(10);
            entity.Property(e => e.WarrantyDescription).HasMaxLength(500);
            entity.Property(e => e.IntegrationType).HasMaxLength(100);
            entity.Property(e => e.ApiDocumentationUrl).HasMaxLength(500);
            entity.Property(e => e.SdkDocumentationUrl).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.BrochureUrl).HasMaxLength(500);
            entity.Property(e => e.ManualUrl).HasMaxLength(500);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(1000);
            entity.Property(e => e.IsAttendanceDevice).HasDefaultValue(true);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsOccupied).HasDefaultValue(false);
            entity.Property(e => e.SupportsMqtt).HasDefaultValue(false);
            entity.Property(e => e.SupportsMqtts).HasDefaultValue(false);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__District__3214EC07CB6A47D0");

            entity.ToTable("District", "skolenza");

            entity.HasIndex(e => e.DistrictName, "IX_District_DistrictName");

            entity.HasIndex(e => e.StateId, "IX_District_StateId");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DistrictName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DistrictCode).HasMaxLength(100);
            entity.HasIndex(e => new { e.StateId, e.DistrictCode }).IsUnique();
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasOne(d => d.State).WithMany(p => p.District)
                .HasForeignKey(d => d.StateId)
                .HasConstraintName("FK_District_State");
        });

        modelBuilder.Entity<EmailQueue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailQue__3214EC0705218438");

            entity.ToTable("EmailQueue", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsSent).HasDefaultValue(false);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.IsProcessing).HasDefaultValue(false);
            entity.Property(e => e.TemplateCode).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.ToEmail).HasMaxLength(250);

            entity.HasOne(d => d.Template).WithMany(p => p.EmailQueue)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmailQueu__Templ__44B528D7");
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailTem__3214EC072FE49A64");

            entity.ToTable("EmailTemplate", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.AddedFromIp)
                .HasMaxLength(50)
                .HasColumnName("AddedFromIP");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.FromEmail).HasMaxLength(150);
            entity.Property(e => e.FromName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LanguageCode).HasMaxLength(10);
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.TemplateCode).HasMaxLength(100);
            entity.Property(e => e.TemplateName).HasMaxLength(150);
            entity.Property(e => e.UpdatedFromIp)
                .HasMaxLength(50)
                .HasColumnName("UpdatedFromIP");
        });

        modelBuilder.Entity<InstitutionEmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_InstitutionEmailTemplate");
            entity.ToTable("InstitutionEmailTemplate", "skolenza");
            entity.HasIndex(e => new { e.InstitutionId, e.TemplateCode })
                .IsUnique()
                .HasDatabaseName("UX_InstitutionEmailTemplate_Institution_TemplateCode");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.AddedFromIp).HasMaxLength(50).HasColumnName("AddedFromIP");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.FromEmail).HasMaxLength(150);
            entity.Property(e => e.FromName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LanguageCode).HasMaxLength(10);
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.TemplateCode).HasMaxLength(100);
            entity.Property(e => e.TemplateName).HasMaxLength(150);
            entity.Property(e => e.UpdatedFromIp).HasMaxLength(50).HasColumnName("UpdatedFromIP");
            entity.HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InstitutionEmailTemplate_Institution");
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07E3264254");

            entity.ToTable("StaffMember", "skolenza");

            entity.HasIndex(e => e.OfficialEmail, "UX_StaffMember_SystemUser_OnlyOnce").IsUnique();

            entity.HasIndex(e => e.Id, "UX_StaffMember_InstitutionIdNullOnce").IsUnique();

            entity.HasIndex(e => e.Id, "UX_StaffMember_InstitutionId_Null_OnlyOnce").IsUnique();

            entity.HasIndex(e => e.InstitutionStaffCategoryMasterId);

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.BloodGroup).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EmergencyContactNumber).HasMaxLength(50);
            entity.Property(e => e.EmergencyContactPerson).HasMaxLength(100);
            entity.Property(e => e.EmployementCode).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.MobileNumber).HasMaxLength(50);
            entity.Property(e => e.OfficialEmail).HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(200);

            entity.HasOne(d => d.Country).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMember_Country");

            entity.HasOne(d => d.Designation).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.DesignationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMember_Designation");

            entity.HasOne(d => d.Department).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMember_Department");

            entity.HasOne(d => d.StaffEmploymentType).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.StaffEmploymentTypeId)
                .HasConstraintName("FK_StaffMember_StaffEmploymentType");

            entity.HasOne(d => d.StaffCategory).WithMany(p => p.StaffMembers)
                .HasForeignKey(d => d.StaffCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMember_StaffCategory");

            entity.HasOne<InstitutionStaffCategoryMaster>().WithMany()
                .HasForeignKey(d => d.InstitutionStaffCategoryMasterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMember_InstitutionCategory");

            entity.HasOne(d => d.Gender).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.GenderId)
                .HasConstraintName("FK_StaffMember_Gender");

            entity.HasOne(d => d.Institution).WithMany(p => p.StaffMember)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StaffMember_Institution");
        });

        modelBuilder.Entity<StaffMemberBankDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC072E9F930F");

            entity.ToTable("StaffMemberBankDetail", "skolenza");

            entity.Property(e => e.AccountNumber).HasMaxLength(128);
            entity.Property(e => e.AccountType).HasMaxLength(50);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(100);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.Ifsccode)
                .HasMaxLength(128)
                .HasColumnName("IFSCCode");
            entity.Property(e => e.IsPrimaryAccount).HasDefaultValue(true);
            entity.Property(e => e.Upiid)
                .HasMaxLength(512)
                .HasColumnName("UPIId");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberBankDetail)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberBank_StaffMember");
        });

        modelBuilder.Entity<StaffMemberCategorySkill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07BB3C49C9");

            entity.ToTable("StaffMemberCategorySkill", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.StaffMemberCategorySkill)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_StaffMemberCategorySkill_Category");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberCategorySkill)
                .HasForeignKey(d => d.StaffMemberId)
                .HasConstraintName("FK_StaffMemberCategorySkill_StaffMember");
        });

        modelBuilder.Entity<StaffMemberCodePattern>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC070ECDA69B");

            entity.ToTable("StaffMemberCodePattern", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Prefix).HasMaxLength(20);
            entity.Property(e => e.RunningNumberLength).HasDefaultValue(4);
            entity.Property(e => e.Separator)
                .HasMaxLength(5)
                .HasDefaultValueSql("'/'::character varying");

            entity.HasOne(d => d.Institution).WithMany(p => p.StaffMemberCodePattern)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberCodePattern_Institution");
        });

        modelBuilder.Entity<StaffMemberContact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07371CFD87");

            entity.ToTable("StaffMemberContact", "skolenza");

            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.AlternateNumber).HasMaxLength(20);
            entity.Property(e => e.ContactName).HasMaxLength(302);
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsRequired(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HouseNo).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEditAllowed).HasDefaultValue(true);
            entity.Property(e => e.IsInfoVerified).HasDefaultValue(false);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(true);
            entity.Property(e => e.LandMark).HasMaxLength(250);
            entity.Property(e => e.Remark).HasMaxLength(250);
            entity.Property(e => e.Street).HasMaxLength(250);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberContact)
                .HasForeignKey(d => d.StaffMemberId)
                .HasConstraintName("FK_StaffMemberContact_StaffMember");

            entity.HasOne(d => d.Locality).WithMany()
                .HasForeignKey(d => d.LocalityId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_StaffMemberContact_Locality");
        });

        modelBuilder.Entity<StaffMemberDailyAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC078C36F2DB");

            entity.ToTable("StaffMemberDailyAttendance", "skolenza");

            entity.Property(e => e.IsLate).HasDefaultValue(false);

            entity.HasOne(d => d.AttendanceDeviceType).WithMany(p => p.StaffMemberDailyAttendance)
                .HasForeignKey(d => d.AttendanceDeviceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDeviceType");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberDailyAttendance)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMember");

            entity.HasOne(d => d.WorkstationType).WithMany(p => p.StaffMemberDailyAttendance)
                .HasForeignKey(d => d.WorkstationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkstationType");
        });

        modelBuilder.Entity<StaffMemberAttendancePunch>(entity =>
        {
            entity.ToTable("StaffMemberAttendancePunch", "skolenza");
            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.Latitude).HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasPrecision(10, 7);
            entity.Property(e => e.AccuracyMeters).HasPrecision(10, 2);
            entity.Property(e => e.DistanceFromLocationMeters).HasPrecision(12, 2);
            entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.IdempotencyKey }).IsUnique();
            entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.WorkDate, e.OccurredAtUtc });
            entity.HasOne(e => e.AttendanceDeviceType)
                .WithMany(e => e.StaffMemberAttendancePunches)
                .HasForeignKey(e => e.AttendanceDeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMemberAttendancePunch_AttendanceDeviceType");
        });

        modelBuilder.Entity<StaffMemberDependent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC074B6A13E2");

            entity.ToTable("StaffMemberDependent", "skolenza");

            entity.Property(e => e.DependentName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.FileName).HasMaxLength(100);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.IsCoveredInPolicy).HasDefaultValue(false);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.UpdatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberDependent)
                .HasForeignKey(d => d.StaffMemberId)
                .HasConstraintName("FK_StaffMemberDependents_StaffMember");
        });

        modelBuilder.Entity<StaffMemberEducation>(entity =>
        {
            entity.ToTable("StaffMemberEducation", "skolenza");

            entity.Property(e => e.Degree).HasMaxLength(50);
            entity.Property(e => e.EducationGap).HasDefaultValue(false);
            entity.Property(e => e.FileName).HasMaxLength(100);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.GradeDivision).HasMaxLength(10);
            entity.Property(e => e.InstituteName).HasMaxLength(100);
            entity.Property(e => e.ReasonOfEducationGap).HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(100);
            entity.Property(e => e.ScoreValue).HasMaxLength(50);
        });

        modelBuilder.Entity<StaffMemberExperience>(entity =>
        {
            entity.ToTable("StaffMemberExperience", "skolenza");

            entity.Property(e => e.ColleagueContactNumber).HasMaxLength(20);
            entity.Property(e => e.ColleagueDesignation).HasMaxLength(100);
            entity.Property(e => e.ColleagueName).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Ctc)
                .HasPrecision(18, 2)
                .HasColumnName("CTC");
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.StaffMemberIdOfCompany).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAnyGap).HasDefaultValue(false);
            entity.Property(e => e.IsForeignExperience).HasDefaultValue(false);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsWfh)
                .HasDefaultValue(false)
                .HasColumnName("IsWFH");
            entity.Property(e => e.ReasonForLeaving).HasMaxLength(200);
            entity.Property(e => e.ReasonOfGap).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(200);
            entity.Property(e => e.ReportingManagerName).HasMaxLength(100);
            entity.Property(e => e.ReportingManagerNumber).HasMaxLength(20);
            entity.Property(e => e.VerificationEmail).HasMaxLength(100);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberExperience)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StaffMemberExperience_StaffMember");
        });

        modelBuilder.Entity<StaffMemberExperienceDocument>(entity =>
        {
            entity.ToTable("StaffMemberExperienceDocument", "skolenza");

            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.HasExperienceDocUploaded).HasDefaultValue(true);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.Remark).HasMaxLength(200);

            entity.HasOne(d => d.StaffMemberExperience).WithMany(p => p.StaffMemberExperienceDocument)
                .HasForeignKey(d => d.StaffMemberExperienceId)
                .HasConstraintName("FK_StaffMemberExperienceDocument_Experience");
        });

        modelBuilder.Entity<StaffMemberIdentity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC074FF9BCC1");

            entity.ToTable("StaffMemberIdentity", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.DocumentFileName).HasMaxLength(255);
            entity.Property(e => e.DocumentFilePath).HasMaxLength(500);
            entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IdentityValue).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEditAllowed).HasDefaultValue(true);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberIdentity)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberIdentity_StaffMember");

            entity.HasOne(d => d.IdentityCategoryDocument).WithMany(p => p.StaffMemberIdentity)
                .HasForeignKey(d => d.IdentityCategoryDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberIdentity_Document");
        });

        modelBuilder.Entity<StaffMemberImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07C583D80F");

            entity.ToTable("StaffMemberImage", "skolenza");

            entity.Property(e => e.FileName).HasMaxLength(50);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberImage)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StaffMemberImages_StaffMember");
        });



        modelBuilder.Entity<StaffMemberManagerMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC075BB6D3FC");

            entity.ToTable("StaffMemberManagerMapping", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(250);

            entity.HasOne(d => d.Department).WithMany(p => p.StaffMemberManagerMapping)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_StaffMemberManagerMapping_Department");

            entity.HasOne(d => d.Designation).WithMany(p => p.StaffMemberManagerMapping)
                .HasForeignKey(d => d.DesignationId)
                .HasConstraintName("FK_StaffMemberManagerMapping_Designation");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberManagerMappingStaffMember)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberManagerMapping_StaffMember");

            entity.HasOne(d => d.Manager).WithMany(p => p.StaffMemberManagerMappingManager)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberManagerMapping_Manager");

            entity.HasOne(d => d.ReportingType).WithMany(p => p.StaffMemberManagerMapping)
                .HasForeignKey(d => d.ReportingTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberManagerMapping_ReportingType");

            entity.HasOne(d => d.Institution).WithMany(p => p.StaffMemberManagerMapping)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberManagerMapping_Institution");
        });

        modelBuilder.Entity<StaffMemberPersonalDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC074796302D");

            entity.ToTable("StaffMemberPersonalDetail", "skolenza");

            entity.Property(e => e.AadhaarDocName).HasMaxLength(100);
            entity.Property(e => e.AadhaarDocPath).HasMaxLength(500);
            entity.Property(e => e.AadhaarNumber).HasMaxLength(20);
            entity.Property(e => e.BloodGroup).HasMaxLength(10);
            entity.Property(e => e.DrivingLicenseNumber).HasMaxLength(20);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactNumber).HasMaxLength(15);
            entity.Property(e => e.EmergencyContactRelation).HasMaxLength(50);
            entity.Property(e => e.HasEpfaccount).HasColumnName("HasEPFAccount");
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.PanDocName).HasMaxLength(100);
            entity.Property(e => e.PanDocPath).HasMaxLength(500);
            entity.Property(e => e.PanNumber).HasMaxLength(20);
            entity.Property(e => e.PassportDocName).HasMaxLength(100);
            entity.Property(e => e.PassportDocPath).HasMaxLength(500);
            entity.Property(e => e.PassportNumber).HasMaxLength(20);
            entity.Property(e => e.Uannumber)
                .HasMaxLength(20)
                .HasColumnName("UANNumber");
            entity.Property(e => e.VoterId).HasMaxLength(20);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberPersonalDetail)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberPersonalDetail_StaffMember");
        });



        modelBuilder.Entity<StaffMemberStatutoryAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07ACDD4E51");

            entity.ToTable("StaffMemberStatutoryAccount", "skolenza");

            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.EmployerCode).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.StatutoryType).WithMany(p => p.StaffMemberStatutoryAccount)
                .HasForeignKey(d => d.StatutoryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberStatutoryAccount_Statutory");
        });

        modelBuilder.Entity<StaffEmploymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC0760C5ED38");

            entity.ToTable("StaffEmploymentType", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.TypeName).HasMaxLength(255);
        });

        modelBuilder.Entity<StaffCategory>(entity =>
        {
            entity.ToTable("StaffCategory", "skolenza");
            entity.HasKey(e => e.Id).HasName("PK_StaffCategory");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_StaffCategory_Code");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSystemDefined).HasDefaultValue(true);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<StaffEmploymentTypeBasicMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07A773CB3F");

            entity.ToTable("StaffEmploymentTypeBasicMenu", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsMenuDisplayInUi).HasColumnName("IsMenuDisplayInUI");

            entity.HasOne(d => d.BasicMenu).WithMany(p => p.StaffEmploymentTypeBasicMenu)
                .HasForeignKey(d => d.BasicMenuId)
                .HasConstraintName("FK_StaffEmploymentTypeBasicMenu_BasicMenu");

            entity.HasOne(d => d.StaffEmploymentType).WithMany(p => p.StaffEmploymentTypeBasicMenu)
                .HasForeignKey(d => d.StaffEmploymentTypeId)
                .HasConstraintName("FK_StaffEmploymentTypeBasicMenu_StaffEmploymentType");
        });

        modelBuilder.Entity<StaffMemberWorkDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07BDFE60B6");

            entity.ToTable("StaffMemberWorkDocument", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.FileName).HasMaxLength(250);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.StaffMemberWorkHistory).WithMany(p => p.StaffMemberWorkDocument)
                .HasForeignKey(d => d.StaffMemberWorkHistoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberWorkDocument_WorkHistory");

            entity.HasOne(d => d.WorkDocumentType).WithMany(p => p.StaffMemberWorkDocument)
                .HasForeignKey(d => d.WorkDocumentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberWorkDocument_DocumentType");
        });

        modelBuilder.Entity<StaffMemberWorkHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07994D46FA");

            entity.ToTable("StaffMemberWorkHistory", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.Ctc)
                .HasPrecision(18, 2)
                .HasColumnName("CTC");
            entity.Property(e => e.Designation).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEditAllowed).HasDefaultValue(true);
            entity.Property(e => e.IsWfh).HasColumnName("IsWFH");
            entity.Property(e => e.ReasonForLeaving).HasMaxLength(500);
            entity.Property(e => e.ReportingManagerName).HasMaxLength(150);
            entity.Property(e => e.ReportingManagerNumber).HasMaxLength(20);
            entity.Property(e => e.VerificationEmail).HasMaxLength(150);
            entity.Property(e => e.VerificationMode).HasMaxLength(50);

            entity.HasOne(d => d.StaffMemberWorkProfile).WithMany(p => p.StaffMemberWorkHistory)
                .HasForeignKey(d => d.StaffMemberWorkProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberWorkHistory_Profile");
        });

        modelBuilder.Entity<StaffMemberWorkProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC07605A7EB3");

            entity.ToTable("StaffMemberWorkProfile", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEditAllowed).HasDefaultValue(true);
        });

        modelBuilder.Entity<StaffMembersChangedTypeHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC0779153EAD");

            entity.ToTable("StaffMembersChangedTypeHistory", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ChangeDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(200);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMembersChangedTypeHistory)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberStatusHistory_StaffMember");

            entity.HasOne(d => d.NewStaffEmploymentType).WithMany(p => p.StaffMembersChangedTypeHistoryNewStaffEmploymentType)
                .HasForeignKey(d => d.NewStaffEmploymentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberStatusHistory_NewStaffEmploymentType");

            entity.HasOne(d => d.OldStaffEmploymentType).WithMany(p => p.StaffMembersChangedTypeHistoryOldStaffEmploymentType)
                .HasForeignKey(d => d.OldStaffEmploymentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMemberStatusHistory_OldStaffEmploymentType");
        });

        modelBuilder.Entity<ForgotPasswordOtpdetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ForgotPa__3214EC071E853072");

            entity.ToTable("ForgotPasswordOTPDetail", "skolenza");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.Otp)
                .HasMaxLength(10)
                .HasColumnName("OTP");
            entity.Property(e => e.OtpexpireDateTime).HasColumnName("OTPExpireDateTime");
        });

        modelBuilder.Entity<Gender>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Gender__3214EC07EF3CD03D");

            entity.ToTable("Gender", "skolenza");

            entity.HasIndex(e => e.GenderName, "UQ__Gender__F7C177153AF55502").IsUnique();

            entity.Property(e => e.GenderName).HasMaxLength(50);
        });
            modelBuilder.Entity<HostRole>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("HostRole_pkey");

                entity.ToTable("HostRole", "skolenza");

                entity.HasIndex(e => e.Name, "UQ_HostRole_Name").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<HostRoleModuleAndPermission>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("HostRoleModuleAndPermission_pkey");

                entity.ToTable("HostRoleModuleAndPermission", "skolenza");

                entity.HasIndex(e => e.HostRoleId, "IX_HRMP_HostRoleId");

                entity.HasIndex(e => e.ModuleId, "IX_HRMP_ModuleId");

                entity.HasIndex(e => e.OperationId, "IX_HRMP_OperationId");

                entity.HasIndex(e => new { e.HostRoleId, e.ModuleId, e.OperationId }, "UQ_HostRole_Module_Operation").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(d => d.HostRole).WithMany(p => p.HostRoleModuleAndPermission)
                    .HasForeignKey(d => d.HostRoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HRMP_HostRole");

                entity.HasOne(d => d.Module).WithMany(p => p.HostRoleModuleAndPermission)
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HRMP_Module");

                entity.HasOne(d => d.Operation).WithMany(p => p.HostRoleModuleAndPermission)
                    .HasForeignKey(d => d.OperationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HRMP_Operation");
            });

            modelBuilder.Entity<HostUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("HostUser_pkey");

                entity.ToTable("HostUser", "skolenza");

                entity.HasIndex(e => e.LoginId, "IX_HostUser_LoginId");

                entity.HasIndex(e => e.LoginId, "UQ_HostUser_LoginId").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.LoginId).HasMaxLength(255);
                entity.Property(e => e.MobileNumber).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(150);

                entity.HasOne(d => d.HostRole).WithMany(p => p.HostUser)
                    .HasForeignKey(d => d.HostRoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HostUser_HostRole");
            });

        modelBuilder.Entity<IdentityCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Identity__3214EC07393AF557");

            entity.ToTable("IdentityCategory", "skolenza");

            entity.HasIndex(e => e.Code, "UQ__Identity__A25C5AA7EC142E9A").IsUnique();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<IdentityCategoryDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Identity__3214EC07AA61914F");

            entity.ToTable("IdentityCategoryDocument", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.DocumentName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsUnique).HasDefaultValue(true);

            entity.HasOne(d => d.IdentityCategory).WithMany(p => p.IdentityCategoryDocument)
                .HasForeignKey(d => d.IdentityCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IdentityDocument_Category");
        });



        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveReq__3214EC07D95D9BAD");

            entity.ToTable("LeaveRequest", "skolenza");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsHalfDay).HasDefaultValue(false);
            entity.Property(e => e.IsSandwich).HasDefaultValue(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(100);
            entity.Property(e => e.TotalLeaveDays).HasPrecision(5, 2);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.LeaveRequest)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LeaveRequ__Emplo__05EEBAAE");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveRequest)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LeaveRequ__Leave__06E2DEE7");

            entity.HasOne(d => d.Institution).WithMany(p => p.LeaveRequest)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LeaveRequ__Tenan__04FA9675");
        });




        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveTyp__3214EC0788A5EF9C");

            entity.ToTable("LeaveType", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LeaveName).HasMaxLength(50);

            entity.HasOne(d => d.Institution).WithMany(p => p.LeaveType)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_LeaveType_InstitutionId");
        });

        //modelBuilder.Entity<License>(entity =>
        //{
        //    entity.HasKey(e => e.Id).HasName("PK__License__3214EC07FF575687");

        //    entity.ToTable("License", "skolenza");

        //    entity.Property(e => e.IsActive).HasDefaultValue(true);
        //});

        modelBuilder.Entity<LoginCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LoginCre__3214EC0750429DFA");

            entity.ToTable("LoginCredential", "skolenza");

            entity.HasIndex(e => e.LoginId, "UQ_LoginId").IsUnique();

            entity.Property(e => e.AddedById).HasDefaultValue(0L);
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.HasFirstLogin).HasDefaultValue(true);
            entity.Property(e => e.IpAddressLocal).HasMaxLength(50);
            entity.Property(e => e.IpAddressPublic).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.Latitude).HasDefaultValue(0.0);
            entity.Property(e => e.LoginDevice).HasDefaultValue(0);
            entity.Property(e => e.LoginId).HasMaxLength(255);
            entity.Property(e => e.Longitude).HasDefaultValue(0.0);
            entity.Property(e => e.MacAddress).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(550);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.SoftDeletedById).HasDefaultValue(0L);
            entity.Property(e => e.UpdatedById).HasDefaultValue(0L);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.LoginCredential)
                .HasForeignKey(d => d.StaffMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoginCredential_StaffMember");
        });


        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Module__3214EC078891AB2D");

            entity.ToTable("Module", "skolenza");

            entity.Property(e => e.Id).UseIdentityByDefaultColumn();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");            
             
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.ImageIconMobile).HasMaxLength(255);
            entity.Property(e => e.ImageIconWeb).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsModuleDisplayInUI)
                .HasDefaultValue(true)
                .HasColumnName("IsModuleDisplayInUI");
            entity.Property(e => e.ModuleScope).HasDefaultValue((short)1);
            entity.Property(e => e.ItemPriority).HasDefaultValue(0);
            entity.Property(e => e.ModuleCode).HasMaxLength(50);
            entity.Property(e => e.ModuleName).HasMaxLength(100);
            entity.Property(e => e.PageName).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(200);
            entity.Property(e => e.Urlpath)
                .HasMaxLength(500)
                .HasColumnName("URLPath");

            entity.HasOne(d => d.ParentModule).WithMany(p => p.InverseParentModule)
                .HasForeignKey(d => d.ParentModuleId)
                .HasConstraintName("FK_Module_ParentModule");
        });

        modelBuilder.Entity<ModuleOperationMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ModuleOp__3214EC07BF86A196");

            entity.ToTable("ModuleOperationMapping", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IconUrl)
                .HasMaxLength(255)
                .HasColumnName("IconURL");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsCommonItem).HasDefaultValue(false);
            entity.Property(e => e.IsOperational).HasDefaultValue(true);
            entity.Property(e => e.PageUrl)
                .HasMaxLength(255)
                .HasColumnName("PageURL");
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.Remark).HasMaxLength(255);

            entity.HasOne(d => d.DataViewStructure).WithMany(p => p.ModuleOperationMapping)
                .HasForeignKey(d => d.DataViewStructureId)
                .HasConstraintName("FK_ModuleOperationMapping_DataViewStructure");

            entity.HasOne(d => d.Module).WithMany(p => p.ModuleOperationMapping)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("FK_ModuleOperationMapping_Module");

            entity.HasOne(d => d.Operation).WithMany(p => p.ModuleOperationMapping)
                .HasForeignKey(d => d.OperationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ModuleOperation_Operation");

            entity.HasOne(d => d.PageType).WithMany(p => p.ModuleOperationMapping)
                .HasForeignKey(d => d.PageTypeId)
                .HasConstraintName("FK_ModuleOperationMapping_PageTypeEnum");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Operatio__3214EC079C437610");

            entity.ToTable("Operation", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IconImage).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OperationName).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(200);
            entity.Property(e => e.UpdatedDateTime).HasColumnName("UpdatedDateTime");
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Holiday");

            entity.ToTable("Holiday", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.HolidayName).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.HolidayDate).HasColumnType("date");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.CampusId, e.HolidayDate })
                .HasDatabaseName("IX_Holiday_Location_Date");

            entity.HasOne(d => d.Institution).WithMany(p => p.Holidays)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_Holiday_Institution");

            entity.HasOne(d => d.Campus).WithMany(p => p.Holidays)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Holiday_Campus");
        });

        modelBuilder.Entity<PageTypeEnum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PageType__3214EC07D71C5D81");

            entity.ToTable("PageTypeEnum", "skolenza");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PageTypeName).HasMaxLength(20);
        });

        modelBuilder.Entity<PlanModuleMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PlanModu__3214EC0729948732");

            entity.ToTable("PlanModuleMapping", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.UpdatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Module).WithMany(p => p.PlanModuleMapping)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlanModuleMapping_Module");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.PlanModuleMapping)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PMM_SubscriptionPlan");
        });


        modelBuilder.Entity<PolicyType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PolicyTy__3214EC07AAE08A64");

            entity.ToTable("PolicyType", "skolenza");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDelete).HasDefaultValue(false);
            entity.Property(e => e.PolicyName).HasMaxLength(255);

            entity.HasOne(d => d.Institution).WithMany(p => p.PolicyType)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_PolicyType_Institution");
        });



            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC0731D02168");

                entity.ToTable("RefreshToken", "skolenza");

                entity.HasIndex(e => e.HostUserId, "IX_RefreshToken_HostUserId");

                entity.HasIndex(e => e.LoginCredentialId, "IX_RefreshToken_LoginCredentialId");

                entity.HasIndex(e => new { e.LoginId, e.UserType }, "IX_RefreshToken_LoginId_UserType");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedByIp).HasMaxLength(50);
                entity.Property(e => e.ExpiryDate).HasDefaultValueSql("(CURRENT_TIMESTAMP + '5 days'::interval)");
                entity.Property(e => e.IsRevoked).HasDefaultValue(false);
                entity.Property(e => e.LoginId).HasMaxLength(255);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
                entity.Property(e => e.RevokedByIp).HasMaxLength(50);
                entity.Property(e => e.Token).HasMaxLength(500);
                entity.Property(e => e.UserType).HasDefaultValue((short)LoginUserType.InstitutionStaffMember);

                entity.HasOne(d => d.HostUser).WithMany(p => p.RefreshToken)
                    .HasForeignKey(d => d.HostUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_RefreshToken_HostUser");

                entity.HasOne(d => d.LoginCredential).WithMany(p => p.RefreshToken)
                    .HasForeignKey(d => d.LoginCredentialId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_RefreshToken_LoginCredential");
            });

            modelBuilder.Entity<ReportingType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reportin__3214EC0773736C22");

            entity.ToTable("ReportingType", "skolenza");
 

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SoftDeletedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RequestT__3214EC07E53AD996");

            entity.ToTable("RequestType", "skolenza");

            entity.HasIndex(e => new { e.InstitutionId, e.RequestTypeName }, "UQ_RequestType_Institution").IsUnique();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.RequestTypeName).HasMaxLength(100);
            entity.Property(e => e.UpdatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Institution).WithMany(p => p.RequestType)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_RequestType_Institution");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Role__3214EC0723371271");

            entity.ToTable("Role", "skolenza");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsSystemDefault).HasDefaultValue(false);
            entity.Property(e => e.Remark)
                .HasMaxLength(200)
                .HasDefaultValueSql("NULL::character varying");
            entity.Property(e => e.RoleName).HasMaxLength(100);

            entity.HasOne(d => d.Institution).WithMany()
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Role_Institution");
        });

        modelBuilder.Entity<RoleModuleAndPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_RoleModuleAndPermission_Id");

            entity.ToTable("RoleModuleAndPermission", "skolenza");

            entity.HasIndex(e => e.OperationId, "IDX_RolesPermission_OperationId");

            entity.HasIndex(e => e.RoleId, "IDX_RolesPermission_RoleId");

            entity.Property(e => e.ImageIcon).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(255);

            entity.HasOne(d => d.Module).WithMany(p => p.RoleModuleAndPermission)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("FK_RoleModulePermission_Module");

            entity.HasOne(d => d.Operation).WithMany(p => p.RoleModuleAndPermission)
                .HasForeignKey(d => d.OperationId)
                .HasConstraintName("FK_RoleModulePermission_Operation");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleModuleAndPermission)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_RoleModulePermission_Role");
        });

        modelBuilder.Entity<ServiceProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ServiceP__3214EC07B0695346");

            entity.ToTable("ServiceProvider", "skolenza");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Ceoname)
                .HasMaxLength(255)
                .HasColumnName("CEOName");
            entity.Property(e => e.CompanyEmail).HasMaxLength(255);
            entity.Property(e => e.CompanyName).HasMaxLength(255);
            entity.Property(e => e.CompanyType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Fax).HasMaxLength(50);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(50)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.PinCode).HasMaxLength(10);
            entity.Property(e => e.Profile).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.WebsiteUrl)
                .HasMaxLength(255)
                .HasColumnName("WebsiteURL");
        });
            modelBuilder.Entity<SalaryComponentMaster>(entity =>
            {
                entity.ToTable("SalaryComponentMaster", "skolenza");

                entity.HasIndex(e => new { e.InstitutionId, e.ComponentName, e.EffectiveFrom }, "UQ_SalaryComponent_Institution_ComponentName_Effective").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.ComponentName).HasMaxLength(150);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsEditable).HasDefaultValue(true);
                entity.Property(e => e.IsProrated).HasDefaultValue(true);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
                entity.Property(e => e.IsVisibleInPayslip).HasDefaultValue(true);

                entity.HasOne(d => d.ComplianceType).WithMany(p => p.SalaryComponentMaster)
                    .HasForeignKey(d => d.ComplianceTypeId)
                    .HasConstraintName("FK_SalaryComponent_Compliance");

                entity.HasOne(d => d.Country).WithMany(p => p.SalaryComponentMaster)
                    .HasForeignKey(d => d.CountryId)
                    .HasConstraintName("FK_SalaryComponent_Country");

                entity.HasOne(d => d.DependsOnComponent).WithMany(p => p.InverseDependsOnComponent)
                    .HasForeignKey(d => d.DependsOnComponentId)
                    .HasConstraintName("FK_SalaryComponent_Dependency");

                entity.HasOne(d => d.State).WithMany(p => p.SalaryComponentMaster)
                    .HasForeignKey(d => d.StateId)
                    .HasConstraintName("FK_SalaryComponent_State");

                entity.HasOne(d => d.Institution).WithMany(p => p.SalaryComponentMaster)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SalaryComponent_Institution");
            });

            modelBuilder.Entity<SalaryStructure>(entity =>
            {
                entity.ToTable("SalaryStructure", "skolenza");

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
                entity.Property(e => e.StructureName).HasMaxLength(150);

                entity.HasOne(d => d.Institution).WithMany(p => p.SalaryStructure)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SalaryStructure_Institution");
            });

            modelBuilder.Entity<SalaryStructureDetail>(entity =>
            {
                entity.ToTable("SalaryStructureDetail", "skolenza");

                entity.HasIndex(e => new { e.SalaryStructureId, e.CalculationOrder }, "UQ_Structure_Order").IsUnique();

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.FormulaExpression).HasColumnType("jsonb");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
                entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.MinAmount).HasPrecision(18, 2);
                entity.Property(e => e.Value).HasPrecision(18, 2);

                entity.HasOne(d => d.Component).WithMany(p => p.SalaryStructureDetailComponent)
                    .HasForeignKey(d => d.ComponentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StructureDetail_Component");

                entity.HasOne(d => d.DependsOnComponent).WithMany(p => p.SalaryStructureDetailDependsOnComponent)
                    .HasForeignKey(d => d.DependsOnComponentId)
                    .HasConstraintName("FK_StructureDetail_Dependency");

                entity.HasOne(d => d.SalaryStructure).WithMany(p => p.SalaryStructureDetail)
                    .HasForeignKey(d => d.SalaryStructureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StructureDetail_Structure");
            });

            modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__State__3214EC0705AF0D74");

            entity.ToTable("State", "skolenza");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StateCode).HasMaxLength(50);
            entity.Property(e => e.StateName).HasMaxLength(100);
            entity.HasIndex(e => new { e.CountryId, e.StateCode }).IsUnique();

            entity.HasOne(d => d.Country).WithMany(p => p.State)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__State__CountryId__6B0FDBE9");
        });

        modelBuilder.Entity<StatutoryType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Statutor__3214EC078EED6601");

            entity.ToTable("StatutoryType", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Country).WithMany(p => p.StatutoryType)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatutoryType_Country");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subscrip__3214EC07BED0F0D0");

            entity.ToTable("SubscriptionPlan", "skolenza");

            entity.HasIndex(e => e.IsSoftDeleted, "IX_SubscriptionPlan_IsSoftDeleted");

            entity.Property(e => e.CurrencyKey).HasMaxLength(20);
            entity.Property(e => e.DeletedById).HasColumnType("integer");
            entity.Property(e => e.DeletedDateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
            entity.Property(e => e.PerDayPrice).HasPrecision(10, 2);
            entity.Property(e => e.PlanName).HasMaxLength(100);
            entity.Property(e => e.YearlyPrice).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Institution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Institution__3214EC0728DD7C6E");

            entity.ToTable("Institution", "skolenza");

            entity.HasIndex(e => e.InstitutionEmail, "UQ__Institution__F7C944DD7E3D53D9").IsUnique();

            entity.Property(e => e.CompanyEmailDomain).HasMaxLength(255);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.ContactNumber).HasMaxLength(20);
            entity.Property(e => e.ContactPersonName).HasMaxLength(100);
            entity.Property(e => e.InstitutionCode).HasMaxLength(100);
            entity.Property(e => e.InstitutionEmail).HasMaxLength(200);

            entity.HasOne(d => d.Gender).WithMany(p => p.Institution)
                .HasForeignKey(d => d.GenderId)
                .HasConstraintName("FK_Gender_Institution");

            entity.HasOne(d => d.InstitutionType).WithMany(p => p.Institutions)
                .HasForeignKey(d => d.InstitutionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Institution_InstitutionType");
        });

        modelBuilder.Entity<InstitutionEmailConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionEm__3214EC077A90B3A3");

            entity.ToTable("InstitutionEmailConfig", "skolenza");

            entity.Property(e => e.FromEmail).HasMaxLength(200);
            entity.Property(e => e.FromName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SecrateKey)
                .HasMaxLength(100)
                .HasDefaultValueSql("NULL::character varying");
            entity.Property(e => e.SmtpHost).HasMaxLength(200);
            entity.Property(e => e.SmtpPasswordEncrypted).HasMaxLength(500);
            entity.Property(e => e.SmtpUsername).HasMaxLength(200);

            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionEmailConfig)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_InstitutionEmailConfig_Institution");
        });

        modelBuilder.Entity<DefaultEmailConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_DefaultEmailConfig");

            entity.ToTable("DefaultEmailConfig", "skolenza");

            entity.HasIndex(e => e.ConfigName).IsUnique().HasDatabaseName("UX_DefaultEmailConfig_ConfigName");
            entity.HasIndex(e => e.IsDefault)
                .IsUnique()
                .HasFilter("\"IsDefault\" = true")
                .HasDatabaseName("UX_DefaultEmailConfig_OneDefault");

            entity.Property(e => e.ConfigName).HasMaxLength(100);
            entity.Property(e => e.SmtpHost).HasMaxLength(200);
            entity.Property(e => e.SmtpUsername).HasMaxLength(200);
            entity.Property(e => e.SmtpPasswordEncrypted).HasMaxLength(500);
            entity.Property(e => e.FromEmail).HasMaxLength(200);
            entity.Property(e => e.FromName).HasMaxLength(100);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.SecrateKey).HasMaxLength(500);
            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<InstitutionEnabledModule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionEn__3214EC07AC610092");

            entity.ToTable("InstitutionEnabledModule", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);

            entity.HasOne(d => d.Module).WithMany(p => p.InstitutionEnabledModule)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionEnabledModules_Module");

            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionEnabledModule)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionEnabledModules_Institution");
        });

        modelBuilder.Entity<InstitutionEnabledOperation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionEn__3214EC07047C5B46");

            entity.ToTable("InstitutionEnabledOperation", "skolenza");

            entity.HasIndex(e => new { e.InstitutionId, e.ModuleId, e.OperationId }, "UQ_Institution_Module_Operation").IsUnique();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);

            entity.HasOne(d => d.Module).WithMany(p => p.InstitutionEnabledOperation)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionEnabledOperations_Module");

            entity.HasOne(d => d.Operation).WithMany(p => p.InstitutionEnabledOperation)
                .HasForeignKey(d => d.OperationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionEnabledOperations_Operation");

            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionEnabledOperation)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionEnabledOperations_Institution");
        });

        modelBuilder.Entity<InstitutionEncryptionKeys>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionEn__3214EC070E3540EE");

            entity.ToTable("InstitutionEncryptionKeys", "skolenza");

            entity.HasIndex(e => e.InstitutionId, "UQ_InstitutionKey_InstitutionId").IsUnique();

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.EncryptionKey).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<InstitutionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionIn__3214EC0765258C5E");

            entity.ToTable("InstitutionType", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.InstitutionTypeName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(255);
        });

        modelBuilder.Entity<InstitutionProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionPr__3214EC0796A1B93D");

            entity.ToTable("InstitutionProfile", "skolenza");

            entity.Property(e => e.BusinessType).HasMaxLength(100);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(255);
            entity.Property(e => e.ThemeColor).HasMaxLength(50);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(200);

            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionProfile)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_InstitutionProfile_Institution");
        });

        modelBuilder.Entity<InstitutionSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InstitutionSu__3214EC07CF143048");

            entity.ToTable("InstitutionSubscription", "skolenza");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PaymentMode).HasMaxLength(50);
            entity.Property(e => e.PaymentTxnId).HasMaxLength(100);
            entity.Property(e => e.SubscriptionStartDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.InstitutionSubscription)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionSubscription_Plan");

            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionSubscription)
                .HasForeignKey(d => d.InstitutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstitutionSubscription_Institution");
        });

        modelBuilder.Entity<Tender>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tender__3214EC076E847502");

            entity.ToTable("Tender", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.TenderName).HasMaxLength(255);
            entity.Property(e => e.TenderValue).HasPrecision(18, 2);

            entity.HasOne(d => d.Client).WithMany(p => p.Tender)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tender_ClientId");

            entity.HasOne(d => d.TenderStatus).WithMany(p => p.Tender)
                .HasForeignKey(d => d.TenderStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tender__TenderSt__3EDC53F0");
        });

        modelBuilder.Entity<TenderStatus>(entity =>
        {
            entity.ToTable("TenderStatus", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.StatusName).HasMaxLength(100);
        });

            modelBuilder.Entity<TicketHistory>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("TicketHistory_pkey");

                entity.ToTable("TicketHistory", "skolenza");

                entity.Property(e => e.Action).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Remarks).HasMaxLength(500);

                entity.HasOne(d => d.DoneByUser).WithMany(p => p.TicketHistory)
                    .HasForeignKey(d => d.DoneByUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TicketHistory_User");

                entity.HasOne(d => d.Ticket).WithMany(p => p.TicketHistory)
                    .HasForeignKey(d => d.TicketId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TicketHistory_Ticket");
            });

            modelBuilder.Entity <skolenza.domain.Entity.TicketThread>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Thread_pkey");

            entity.ToTable("TicketThread", "skolenza");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"Thread_Id_seq\"'::regclass)");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<ThreadMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ThreadMessage_pkey");

            entity.ToTable("ThreadMessage", "skolenza");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"ThreadMessage_Id_seq\"'::regclass)");
            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.MessageType).HasDefaultValue(1);

            entity.HasOne(d => d.AddedBy).WithMany(p => p.ThreadMessage)
                .HasForeignKey(d => d.AddedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ThreadMessage_StaffMember");

            entity.HasOne(d => d.Thread).WithMany(p => p.ThreadMessage)
                .HasForeignKey(d => d.ThreadId)
                .HasConstraintName("FK_ThreadMessage_Thread");
        });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("Ticket_pkey");

                entity.ToTable("Ticket", "skolenza");

                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RejectedReason).HasMaxLength(500);
                entity.Property(e => e.TicketNumber).HasMaxLength(50);

                // ===============================
                // 🔥 STAFF_MEMBER RELATIONS (IMPORTANT)
                // ===============================

                entity.HasOne(d => d.RequestedByUser)
                    .WithMany(p => p.TicketRequestedByUser)
                    .HasForeignKey(d => d.RequestedByUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_RequestedBy");

                entity.HasOne(d => d.RequestedForUser)
                    .WithMany(p => p.TicketRequestedForUser)
                    .HasForeignKey(d => d.RequestedForUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_RequestedFor");

                entity.HasOne(d => d.RecommendedByUser)
                    .WithMany(p => p.TicketRecommendedByUser)
                    .HasForeignKey(d => d.RecommendedByUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_RecommendedBy");

                entity.HasOne(d => d.ApprovedByUser)
                    .WithMany(p => p.TicketApprovedByUser)
                    .HasForeignKey(d => d.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_ApprovedBy");

                entity.HasOne(d => d.AssignedToUser)
                    .WithMany(p => p.TicketAssignedToUser)
                    .HasForeignKey(d => d.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_AssignedUser");

               

                // ===============================
                // 🔥 ROLE
                // ===============================

                entity.HasOne(d => d.AssignedToRole)
                    .WithMany(p => p.Ticket)
                    .HasForeignKey(d => d.AssignedToRoleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_AssignedRole");

                // ===============================
                // 🔥 MASTER TABLES
                // ===============================

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.Ticket)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_Institution");

                entity.HasOne(d => d.TicketClassification)
                    .WithMany(p => p.Ticket)
                    .HasForeignKey(d => d.TicketClassificationId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_Classification");

                entity.HasOne(d => d.TicketHeader)
                    .WithMany(p => p.Ticket)
                    .HasForeignKey(d => d.TicketHeaderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_Header");

                entity.HasOne(d => d.TicketType)
                    .WithMany(p => p.Ticket)
                    .HasForeignKey(d => d.TicketTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Ticket_TicketType");
            });

            modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TicketAttachment_pkey");

            entity.ToTable("TicketAttachment", "skolenza");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"TicketAttachment_Id_seq\"'::regclass)");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.UploadedDateTime).HasDefaultValueSql("now()");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.TicketAttachment)
              .HasForeignKey(d => d.UploadedByUserId)
              .OnDelete(DeleteBehavior.SetNull)
              .HasConstraintName("FK_TicketAttachment_User");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.TicketAttachment)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_TicketAttachment_User");
        });

        modelBuilder.Entity<TicketClassification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketCl__3214EC07BA509F8F");

            entity.ToTable("TicketClassification", "skolenza");

            entity.Property(e => e.ClassificationName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Institution).WithMany(p => p.TicketClassification)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_TicketClassification_Institution");
        });

        modelBuilder.Entity<TicketHeader>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketHe__3214EC07EADE3F1B");

            entity.ToTable("TicketHeader", "skolenza");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.HeaderName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Institution).WithMany(p => p.TicketHeader)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("FK_TicketHeader_InstitutionId");

            entity.HasOne(d => d.TicketClassification).WithMany(p => p.TicketHeader)
                .HasForeignKey(d => d.TicketClassificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TicketHeaderType_TicketClassification");
        });

            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__TicketTy__3214EC07554DC568");

                entity.ToTable("TicketType", "skolenza");

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.AutoApproveIfSameRole).HasDefaultValue(false);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsActiveForAllUsers).HasDefaultValue(true);
                entity.Property(e => e.IsApprovalRequired).HasDefaultValue(false);
                entity.Property(e => e.IsAttachmentRequired).HasDefaultValue(false);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
                entity.Property(e => e.TicketTypeName).HasMaxLength(100);

                entity.HasOne(d => d.ApprovalRole)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovalRoleId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .HasConstraintName("FK_TicketType_ApprovalRole");

                entity.HasOne(d => d.ResponsibleRole)
                    .WithMany()
                    .HasForeignKey(d => d.ResponsibleRoleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_TicketType_ResponsibleRole");


                entity.HasOne(d => d.Institution).WithMany(p => p.TicketType)
                    .HasForeignKey(d => d.InstitutionId)
                    .HasConstraintName("FK_TicketType_Institution");

                entity.HasOne(d => d.TicketHeader).WithMany(p => p.TicketType)
                    .HasForeignKey(d => d.TicketHeaderId)
                    .HasConstraintName("FK_TicketType_Header");
            });


        modelBuilder.Entity<TravelMode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TravelMo__3214EC075A97AC69");

            entity.ToTable("TravelMode", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TravelModeName).HasMaxLength(255);
        });


        modelBuilder.Entity<UserAttendanceSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAtte__3214EC0750CE7646");

            entity.ToTable("UserAttendanceSetting", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.GeofenceLatitude).HasPrecision(10, 8);
            entity.Property(e => e.GeofenceLongitude).HasPrecision(10, 8);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAllowed).HasDefaultValue(true);
            entity.Property(e => e.Remark).HasMaxLength(255);

            entity.HasOne(d => d.AttendanceDeviceType).WithMany(p => p.UserAttendanceSetting)
                .HasForeignKey(d => d.AttendanceDeviceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAttendanceSetting_AttendanceDeviceType");

            entity.HasOne(d => d.StaffMember).WithMany(p => p.UserAttendanceSetting)
                .HasForeignKey(d => d.StaffMemberId)
                .HasConstraintName("FK_UserAttendanceSetting_StaffMember");

            entity.HasOne(d => d.WorkstationType).WithMany(p => p.UserAttendanceSetting)
                .HasForeignKey(d => d.WorkstationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAttendanceSetting_WorkstationType");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRole__3214EC07C9E25EB9");

            entity.ToTable("UserRole", "skolenza");

            entity.Property(e => e.ApprovalStatus)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);
            entity.Property(e => e.Remark).HasMaxLength(255);

            entity.HasOne(d => d.StaffMember).WithMany(p => p.UserRole)
                .HasForeignKey(d => d.StaffMemberId)
                .HasConstraintName("FK_Role_StaffMember");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRole)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_UserRole_Role");
        });

        modelBuilder.Entity<WorkDocumentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WorkDocu__3214EC0710449AA0");

            entity.ToTable("WorkDocumentType", "skolenza");

            entity.Property(e => e.DocumentTypeName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<WorkflowStage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Workflow__3214EC07428E99CC");

            entity.ToTable("WorkflowStage", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ColorKey).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StageName).HasMaxLength(100);
        });

        modelBuilder.Entity<WorkstationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_WorkstationMode");

            entity.ToTable("WorkstationType", "skolenza");

            entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Workstation)
                .HasMaxLength(20)
                .IsFixedLength();
        });
            modelBuilder.Entity<StaffMemberSalary>(entity =>
            {
                entity.ToTable("StaffMemberSalary", "skolenza");

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.BasicOverride).HasPrecision(18, 2);
                entity.Property(e => e.CTC).HasPrecision(18, 2);
                entity.Property(e => e.CurrencyCode).HasMaxLength(10);
                entity.Property(e => e.HRAOverride).HasPrecision(18, 2);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);

                entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberSalary)
                    .HasForeignKey(d => d.StaffMemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StaffMemberSalary_StaffMember");

                entity.HasOne(d => d.SalaryStructure).WithMany(p => p.StaffMemberSalary)
                    .HasForeignKey(d => d.SalaryStructureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StaffMemberSalary_Structure");
            });


            modelBuilder.Entity<StaffMemberTaxProfile>(entity =>
            {
                entity.ToTable("StaffMemberTaxProfile", "skolenza");

                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
                entity.Property(e => e.FinancialYear).HasMaxLength(10);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsSoftDeleted).HasDefaultValue(false);

                entity.HasOne(d => d.Country).WithMany(p => p.StaffMemberTaxProfile)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StaffMemberTaxProfile_Country");

                entity.HasOne(d => d.StaffMember).WithMany(p => p.StaffMemberTaxProfile)
                    .HasForeignKey(d => d.StaffMemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StaffMemberTaxProfile_StaffMember");

                entity.HasOne(d => d.Regime).WithMany(p => p.StaffMemberTaxProfile)
                    .HasForeignKey(d => d.RegimeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StaffMemberTaxProfile_Regime");

                entity.HasOne(d => d.State).WithMany(p => p.StaffMemberTaxProfile)
                    .HasForeignKey(d => d.StateId)
                    .HasConstraintName("FK_StaffMemberTaxProfile_State");
            });

            modelBuilder.Entity<StaffEmploymentType>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__StaffMember__3214EC0760C5ED38");

                entity.ToTable("StaffEmploymentType", "skolenza");

                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Remark).HasMaxLength(255);
                entity.Property(e => e.TypeName).HasMaxLength(255);
            });

            modelBuilder.Entity<StaffCategory>(entity =>
            {
                entity.ToTable("StaffCategory", "skolenza");
                entity.HasKey(e => e.Id).HasName("PK_StaffCategory");
                entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_StaffCategory_Code");
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsSystemDefined).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("now()");
            });

            #region Staff Professional Structure

            modelBuilder.Entity<InstitutionMasterEntity>(entity =>
            {
                entity.UseTpcMappingStrategy();
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Code).HasMaxLength(60);
                entity.Property(item => item.Name).HasMaxLength(150);
                entity.Property(item => item.Description).HasMaxLength(500);
                entity.Property(item => item.IsActive).HasDefaultValue(true);
                entity.Property(item => item.IsSystemDefined).HasDefaultValue(false);
                entity.Property(item => item.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
            ConfigureInstitutionMaster<StaffJobFamilyMaster>(modelBuilder, "StaffJobFamilyMaster");
            ConfigureInstitutionMaster<InstitutionStaffCategoryMaster>(modelBuilder, "InstitutionStaffCategoryMaster");
            ConfigureInstitutionMaster<StaffCapabilityMaster>(modelBuilder, "StaffCapabilityMaster");
            ConfigureInstitutionMaster<StaffDutyDefinitionMaster>(modelBuilder, "StaffDutyDefinitionMaster");
            ConfigureInstitutionMaster<StaffPositionMaster>(modelBuilder, "StaffPositionMaster");

            modelBuilder.Entity<StaffCapabilityMaster>(entity =>
            {
                entity.Property(item => item.RequiresCertification).HasDefaultValue(false);
                entity.Property(item => item.CertificationValidityRequired).HasDefaultValue(false);
            });

            modelBuilder.Entity<StaffDutyDefinitionMaster>(entity =>
            {
                entity.Property(item => item.DefaultFrequency).HasMaxLength(40);
                entity.Property(item => item.RequiresEvidence).HasDefaultValue(false);
            });

            modelBuilder.Entity<StaffPositionMaster>(entity =>
            {
                entity.HasIndex(item => item.StaffJobFamilyMasterId);
                entity.HasIndex(item => item.StaffCategoryId);
                entity.HasIndex(item => item.InstitutionStaffCategoryMasterId);
                entity.HasOne<InstitutionStaffCategoryMaster>().WithMany()
                    .HasForeignKey(item => item.InstitutionStaffCategoryMasterId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_StaffPositionMaster_InstitutionCategory");
            });

            modelBuilder.Entity<InstitutionStaffCategoryMaster>(entity =>
            {
                entity.HasIndex(item => item.StaffCategoryTypeId);
            });

            modelBuilder.Entity<StaffCategoryJobFamily>(entity =>
            {
                entity.ToTable("StaffCategoryJobFamily", "skolenza");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new
                {
                    item.InstitutionId,
                    item.InstitutionStaffCategoryMasterId,
                    item.StaffJobFamilyMasterId
                }).IsUnique();
                entity.HasOne<InstitutionStaffCategoryMaster>().WithMany()
                    .HasForeignKey(item => item.InstitutionStaffCategoryMasterId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<StaffJobFamilyMaster>().WithMany()
                    .HasForeignKey(item => item.StaffJobFamilyMasterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StaffPositionCapability>(entity =>
            {
                entity.ToTable("StaffPositionCapability", "skolenza");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.StaffPositionMasterId, item.StaffCapabilityMasterId }).IsUnique();
            });

            modelBuilder.Entity<StaffPositionDuty>(entity =>
            {
                entity.ToTable("StaffPositionDuty", "skolenza");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.StaffPositionMasterId, item.StaffDutyDefinitionMasterId }).IsUnique();
            });

            modelBuilder.Entity<StaffPositionAssignment>(entity =>
            {
                entity.ToTable("StaffPositionAssignment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Remark).HasMaxLength(500);
            });

            modelBuilder.Entity<StaffCapabilityAssignment>(entity =>
            {
                entity.ToTable("StaffCapabilityAssignment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.VerificationRemark).HasMaxLength(500);
            });

            modelBuilder.Entity<StaffDutyAssignment>(entity =>
            {
                entity.ToTable("StaffDutyAssignment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Remark).HasMaxLength(500);
            });

            #endregion

            #region Staff Academic Structure

            modelBuilder.Entity<InstitutionAcademicEntity>(entity =>
            {
                entity.UseTpcMappingStrategy();
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Code).HasMaxLength(60);
                entity.Property(item => item.Name).HasMaxLength(150);
                entity.Property(item => item.Description).HasMaxLength(500);
                entity.Property(item => item.IsActive).HasDefaultValue(true);
                entity.Property(item => item.IsSoftDeleted).HasDefaultValue(false);
                entity.Property(item => item.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            ConfigureAcademicEntity<AcademicSession>(modelBuilder, "AcademicSessionMaster");
            ConfigureAcademicEntity<AcademicProgramme>(modelBuilder, "AcademicProgrammeMaster");
            ConfigureAcademicEntity<AcademicGroup>(modelBuilder, "AcademicGroupMaster");
            ConfigureAcademicEntity<ClassSection>(modelBuilder, "ClassSectionMaster");
            ConfigureAcademicEntity<Subject>(modelBuilder, "SubjectMaster");
            ConfigureAcademicEntity<CourseOffering>(modelBuilder, "CourseOfferingMaster");

            modelBuilder.Entity<AcademicProgramme>(entity => entity.Property(item => item.Level).HasMaxLength(80));
            modelBuilder.Entity<CourseOffering>(entity =>
            {
                entity.Property(item => item.CurriculumVersion).HasMaxLength(80);
                entity.HasIndex(item => new { item.InstitutionId, item.AcademicSessionId, item.AcademicGroupId, item.SubjectId }).IsUnique();
            });
            modelBuilder.Entity<TeachingAssignmentTypeMaster>(entity =>
            {
                entity.ToTable("TeachingAssignmentTypeMaster", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Code).HasMaxLength(50);
                entity.Property(item => item.Name).HasMaxLength(100);
                entity.Property(item => item.Description).HasMaxLength(300);
                entity.HasIndex(item => item.Code).IsUnique();
            });
            modelBuilder.Entity<StaffTeachingAssignment>(entity =>
            {
                entity.ToTable("StaffTeachingAssignment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ContributionPercent).HasPrecision(5, 2);
                entity.HasOne<TeachingAssignmentTypeMaster>()
                    .WithMany()
                    .HasForeignKey(item => item.AssignmentType)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ClassTeacherAssignment>(entity =>
            {
                entity.ToTable("ClassTeacherAssignment", "skolenza");
                entity.HasKey(item => item.Id);
            });

            ConfigureAcademicEntity<CurriculumFrameworkMaster>(modelBuilder, "CurriculumFrameworkMaster");
            ConfigureAcademicEntity<CurriculumUnit>(modelBuilder, "CurriculumUnit");
            ConfigureAcademicEntity<CurriculumTopic>(modelBuilder, "CurriculumTopic");
            modelBuilder.Entity<AcademicWorkflowStatusMaster>(entity =>
            {
                entity.ToTable("AcademicWorkflowStatusMaster", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Code).HasMaxLength(40);
                entity.Property(item => item.Name).HasMaxLength(80);
                entity.HasIndex(item => item.Code).IsUnique();
            });
            modelBuilder.Entity<CurriculumFrameworkMaster>(entity =>
            {
                entity.Property(item => item.AuthorityName).HasMaxLength(150);
                entity.Property(item => item.Version).HasMaxLength(60);
                entity.Property(item => item.ReviewRemark).HasMaxLength(500);
                entity.HasIndex(item => new { item.InstitutionId, item.Code }).IsUnique();
            });
            modelBuilder.Entity<CurriculumUnit>(entity =>
            {
                entity.Property(item => item.LearningOutcome).HasMaxLength(1000);
                entity.HasIndex(item => new { item.InstitutionId, item.CurriculumFrameworkId, item.Code }).IsUnique();
            });
            modelBuilder.Entity<CurriculumTopic>(entity =>
            {
                entity.Property(item => item.LearningOutcome).HasMaxLength(1000);
                entity.HasIndex(item => new { item.InstitutionId, item.CurriculumUnitId, item.Code }).IsUnique();
            });
            modelBuilder.Entity<LessonPlan>(entity =>
            {
                entity.ToTable("LessonPlan", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.PeriodReference).HasMaxLength(80);
                entity.Property(item => item.Objectives).HasMaxLength(2000);
                entity.Property(item => item.TeachingMethod).HasMaxLength(1000);
                entity.Property(item => item.Resources).HasMaxLength(1000);
                entity.Property(item => item.Activities).HasMaxLength(2000);
                entity.Property(item => item.Assessment).HasMaxLength(1000);
                entity.Property(item => item.Homework).HasMaxLength(1000);
                entity.Property(item => item.ReviewRemark).HasMaxLength(500);
            });
            modelBuilder.Entity<LessonPlanCurriculumTopic>(entity =>
            {
                entity.ToTable("LessonPlanCurriculumTopic", "skolenza");
                entity.HasKey(item => new { item.LessonPlanId, item.CurriculumTopicId });
            });
            modelBuilder.Entity<LessonDeliveryLog>(entity =>
            {
                entity.ToTable("LessonDeliveryLog", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.CoverageNotes).HasMaxLength(2000);
                entity.Property(item => item.Homework).HasMaxLength(1000);
                entity.Property(item => item.EvidenceReference).HasMaxLength(500);
                entity.Property(item => item.ExceptionReason).HasMaxLength(1000);
                entity.Property(item => item.ReplanAction).HasMaxLength(1000);
                entity.Property(item => item.ReviewRemark).HasMaxLength(500);
            });
            modelBuilder.Entity<LessonDeliveryCoverage>(entity =>
            {
                entity.ToTable("LessonDeliveryCoverage", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.CompletionPercent).HasPrecision(5, 2);
                entity.HasIndex(item => new { item.LessonDeliveryLogId, item.CurriculumTopicId }).IsUnique();
            });

            ConfigureAcademicEntity<TimetablePeriodMaster>(modelBuilder, "TimetablePeriodMaster");
            ConfigureAcademicEntity<TimetableWorkflowMaster>(modelBuilder, "TimetableWorkflowMaster");
            ConfigureAcademicEntity<SubstituteWorkflowMaster>(modelBuilder, "SubstituteWorkflowMaster");
            ConfigureAcademicEntity<StaffQueryCategoryMaster>(modelBuilder, "StaffQueryCategoryMaster");
            modelBuilder.Entity<TimetableEntry>(entity =>
            {
                entity.ToTable("TimetableEntry", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.RoomReference).HasMaxLength(100);
                entity.Property(item => item.StatusCode).HasMaxLength(40);
                entity.Property(item => item.ReviewRemark).HasMaxLength(500);
                entity.HasIndex(item => new { item.InstitutionId, item.StaffTeachingAssignmentId, item.DayOfWeek, item.TimetablePeriodMasterId, item.EffectiveFrom });
                entity.HasIndex(item => new { item.InstitutionId, item.ClassSectionId, item.DayOfWeek, item.TimetablePeriodMasterId, item.EffectiveFrom });
            });
            modelBuilder.Entity<TimetableWorkflowStep>(entity =>
            {
                entity.ToTable("TimetableWorkflowStep", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ApproverTypeCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.TimetableWorkflowMasterId, item.Sequence }).IsUnique();
            });
            modelBuilder.Entity<TimetableApprovalHistory>(entity =>
            {
                entity.ToTable("TimetableApprovalHistory", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ActionCode).HasMaxLength(40);
                entity.Property(item => item.Remark).HasMaxLength(500);
                entity.HasIndex(item => new { item.TimetableEntryId, item.Sequence });
            });
            modelBuilder.Entity<SubstituteWorkflowStep>(entity =>
            {
                entity.ToTable("SubstituteWorkflowStep", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ApproverTypeCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.SubstituteWorkflowMasterId, item.Sequence }).IsUnique();
            });
            modelBuilder.Entity<StaffAbsenceImpact>(entity =>
            {
                entity.ToTable("StaffAbsenceImpact", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.StatusCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.InstitutionId, item.LeaveRequestId, item.TimetableEntryId, item.ImpactDate }).IsUnique();
            });
            modelBuilder.Entity<SubstituteAssignment>(entity =>
            {
                entity.ToTable("SubstituteAssignment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.StatusCode).HasMaxLength(40);
                entity.Property(item => item.Remark).HasMaxLength(500);
                entity.HasIndex(item => item.StaffAbsenceImpactId).IsUnique();
            });
            modelBuilder.Entity<SubstituteApprovalHistory>(entity =>
            {
                entity.ToTable("SubstituteApprovalHistory", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ActionCode).HasMaxLength(40);
                entity.Property(item => item.Remark).HasMaxLength(500);
                entity.HasIndex(item => new { item.SubstituteAssignmentId, item.Sequence });
            });
            modelBuilder.Entity<InstitutionCommunicationSetting>(entity =>
            {
                entity.ToTable("InstitutionCommunicationSetting", "skolenza");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.InstitutionId).IsUnique();
            });
            modelBuilder.Entity<StaffConversation>(entity =>
            {
                entity.ToTable("StaffConversation", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ConversationTypeCode).HasMaxLength(40);
                entity.Property(item => item.Subject).HasMaxLength(300);
                entity.Property(item => item.StatusCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.InstitutionId, item.CreatedDateTime });
            });
            modelBuilder.Entity<StaffConversationParticipant>(entity =>
            {
                entity.ToTable("StaffConversationParticipant", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ParticipantRoleCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.StaffConversationId, item.StaffMemberId }).IsUnique();
            });
            modelBuilder.Entity<StaffConversationMessage>(entity =>
            {
                entity.ToTable("StaffConversationMessage", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Body).HasMaxLength(10000);
                entity.HasIndex(item => new { item.StaffConversationId, item.SentDateTime });
            });
            modelBuilder.Entity<StaffMessageAttachment>(entity =>
            {
                entity.ToTable("StaffMessageAttachment", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.FileName).HasMaxLength(255);
                entity.Property(item => item.ContentType).HasMaxLength(100);
                entity.Property(item => item.StorageObjectKey).HasMaxLength(1000);
            });
            modelBuilder.Entity<StaffBroadcastAudience>(entity =>
            {
                entity.ToTable("StaffBroadcastAudience", "skolenza");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.AudienceTypeCode).HasMaxLength(40);
                entity.HasIndex(item => new { item.StaffConversationId, item.AudienceTypeCode, item.TargetId }).IsUnique();
            });

            #endregion

            #region Institution Configuration

            modelBuilder.Entity<Campus>(entity =>
            {
                entity.ToTable("Campus", "skolenza");
                entity.HasIndex(e => e.DistrictId, "IX_Campus_DistrictId");
                entity.HasIndex(e => e.LocalityId, "IX_Campus_LocalityId");
                entity.HasIndex(e => e.CountryId, "IX_Campus_CountryId");
                entity.HasIndex(e => new { e.Id, e.InstitutionId }, "UQ_Campus_Id_InstitutionId").IsUnique();
                entity.HasIndex(e => e.InstitutionId, "IX_Campus_InstitutionId");
                entity.HasIndex(e => new { e.InstitutionId, e.IsActive }, "IX_Campus_InstitutionId_IsActive");
                entity.HasIndex(e => new { e.InstitutionId, e.IsSoftDeleted }, "IX_Campus_InstitutionId_IsSoftDeleted");
                entity.HasIndex(e => e.StateId, "IX_Campus_StateId");
                entity.Property(e => e.LocationCode).HasMaxLength(50);
                entity.Property(e => e.LocationName).HasMaxLength(150);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Landmark).HasMaxLength(200);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Latitude).HasPrecision(10, 7);
                entity.Property(e => e.Longitude).HasPrecision(10, 7);
                entity.Property(e => e.TimeZoneId).HasMaxLength(100);
                entity.Property(e => e.IsAttendanceAllowed).HasDefaultValue(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.Campuss)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Campus_Institution");
                entity.HasOne(e => e.Country).WithMany(e => e.Campuss)
                    .HasForeignKey(e => e.CountryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Campus_Country");
                entity.Property(e => e.LocalityId).HasColumnName("CityId");
                entity.HasOne(e => e.Locality).WithMany(e => e.Campuss)
                    .HasForeignKey(e => e.LocalityId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Campus_Locality");
                entity.HasOne(e => e.District).WithMany()
                    .HasForeignKey(e => e.DistrictId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Campus_District");
            });

            modelBuilder.Entity<AttendancePolicy>(entity =>
            {
                entity.ToTable("AttendancePolicy", "skolenza");
                entity.HasIndex(e => new { e.InstitutionId, e.IsActive, e.IsSoftDeleted }, "IX_AttendancePolicy_InstitutionId_IsActive_IsSoftDeleted");
                entity.HasIndex(e => e.PolicyTypeId, "IX_AttendancePolicy_PolicyTypeId");
                entity.HasIndex(e => e.InstitutionId, "IX_AttendancePolicy_InstitutionId");
                entity.Property(e => e.PolicyName).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.AllowBiometric).HasDefaultValue(true);
                entity.Property(e => e.RequireGeoFenceForOffice).HasDefaultValue(true);
                entity.Property(e => e.RequireGpsForRemote).HasDefaultValue(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.AttendancePolicies)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AttendancePolicy_Institution");
                entity.HasOne(e => e.PolicyType).WithMany(e => e.AttendancePolicies)
                    .HasForeignKey(e => e.PolicyTypeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AttendancePolicy_PolicyType");
            });

            modelBuilder.Entity<InstitutionDevice>(entity =>
            {
                entity.ToTable("InstitutionDevice", "skolenza");
                entity.HasIndex(e => e.InstitutionId, "IX_InstitutionDevice_InstitutionId");
                entity.HasIndex(e => e.CampusId, "IX_InstitutionDevice_CampusId");
                entity.HasIndex(e => e.DeviceMasterId, "IX_InstitutionDevice_DeviceMasterId");
                entity.HasIndex(e => new { e.DeviceMasterId, e.IsActive }, "IX_InstitutionDevice_DeviceMasterId_IsActive");
                entity.HasIndex(e => new { e.InstitutionId, e.IsActive, e.IsSoftDeleted }, "IX_InstitutionDevice_InstitutionId_IsActive_IsSoftDeleted");
                entity.HasIndex(e => new { e.InstitutionId, e.CampusId }, "IX_InstitutionDevice_InstitutionId_CampusId");
                entity.HasIndex(e => new { e.CampusId, e.IsActive }, "IX_InstitutionDevice_CampusId_IsActive");
                entity.Property(e => e.DeviceCode).HasMaxLength(50);
                entity.Property(e => e.DeviceName).HasMaxLength(150);
                entity.Property(e => e.InstallationRemark).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Remark).HasMaxLength(1000);
                entity.Property(e => e.IsAttendanceDevice).HasDefaultValue(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.InstitutionDevices)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_InstitutionDevice_Institution");
                entity.HasOne(e => e.Campus).WithMany(e => e.InstitutionDevice)
                    .HasForeignKey(e => new { e.CampusId, e.InstitutionId })
                    .HasPrincipalKey(e => new { e.Id, e.InstitutionId })
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_InstitutionDevice_Campus");
                entity.HasOne(e => e.DeviceMaster).WithMany(e => e.InstitutionDevice)
                    .HasForeignKey(e => e.DeviceMasterId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_InstitutionDevice_DeviceMaster");
            });

            modelBuilder.Entity<InstitutionDeviceConfiguration>(entity =>
            {
                entity.ToTable("InstitutionDeviceConfiguration", "skolenza");
                entity.HasIndex(e => e.InstitutionDeviceId, "UX_InstitutionDeviceConfiguration_InstitutionDeviceId").IsUnique();
                entity.HasIndex(e => e.LastHeartbeatDateTime, "IX_InstitutionDeviceConfiguration_LastHeartbeatDateTime");
                  entity.Property(e => e.IpAddress).HasMaxLength(45);
                  entity.Property(e => e.MacAddress).HasMaxLength(50);
                  entity.Property(e => e.MqttTransport);
                entity.Property(e => e.CommandTransport);
                entity.Property(e => e.HttpsIngressTokenHash).HasMaxLength(64);
                entity.Property(e => e.PendingHttpsIngressTokenHash).HasMaxLength(64);
                entity.HasIndex(e => e.PendingHttpsIngressTokenHash, "UX_InstitutionDeviceConfiguration_PendingHttpsIngressTokenHash")
                    .IsUnique().HasFilter("\"PendingHttpsIngressTokenHash\" IS NOT NULL");
                  entity.Property(e => e.ServerHost).HasMaxLength(300);
                entity.Property(e => e.ServerPath).HasMaxLength(300);
                entity.Property(e => e.ServerUrl).HasMaxLength(500);
                entity.Property(e => e.PushMode).HasMaxLength(100);
                entity.Property(e => e.TimeZoneId).HasMaxLength(100);
                entity.Property(e => e.Configuration).HasColumnType("jsonb");
                entity.Property(e => e.LastConnectionError).HasMaxLength(1000);
                entity.Property(e => e.IsEnrollmentEnabled).HasDefaultValue(true);
                entity.Property(e => e.IsAttendancePushEnabled).HasDefaultValue(true);
                entity.Property(e => e.IsAutoSyncEnabled).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.InstitutionDevice).WithOne(e => e.InstitutionDeviceConfiguration)
                    .HasForeignKey<InstitutionDeviceConfiguration>(e => e.InstitutionDeviceId)
                    .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_InstitutionDeviceConfiguration_InstitutionDevice");
            });

            modelBuilder.Entity<StaffMemberLocationAssignment>(entity =>
            {
                entity.ToTable("StaffMemberLocationAssignment", "skolenza");
                entity.HasIndex(e => e.StaffMemberId, "IX_StaffMemberLocationAssignment_StaffMemberId");
                entity.HasIndex(e => e.InstitutionId, "IX_StaffMemberLocationAssignment_InstitutionId");
                entity.HasIndex(e => e.CampusId, "IX_StaffMemberLocationAssignment_CampusId");
                // Date-window overlap is enforced by the application; a unique key would reject valid history/future rows.
                entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.CampusId, e.EffectiveFrom }, "IX_StaffMemberLocationAssignment_StaffMember_Location_EffectiveFrom");
                entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.IsPrimary, e.EffectiveFrom }, "IX_StaffMemberLocationAssignment_Primary_EffectiveFrom");
                entity.Property(e => e.IsAttendanceAllowed).HasDefaultValue(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.StaffMemberLocationAssignments)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberLocationAssignment_Institution");
                entity.HasOne(e => e.StaffMember).WithMany(e => e.StaffMemberLocationAssignments)
                    .HasForeignKey(e => e.StaffMemberId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberLocationAssignment_StaffMember");
                entity.HasOne(e => e.Campus).WithMany(e => e.StaffMemberLocationAssignment)
                    .HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberLocationAssignment_Campus");
            });

            modelBuilder.Entity<StaffMemberMonitoringPolicy>(entity =>
            {
                entity.ToTable("StaffMemberMonitoringPolicy", "skolenza");
                entity.HasIndex(e => e.InstitutionId, "UX_StaffMemberMonitoringPolicy_Institution")
                    .IsUnique().HasFilter("((\"IsActive\" = true) AND (\"IsSoftDeleted\" = false))");
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<Institution>().WithMany().HasForeignKey(e => e.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberMonitoringPolicy_Institution");
            });

            modelBuilder.Entity<StaffMemberMonitoringAgent>(entity =>
            {
                entity.ToTable("StaffMemberMonitoringAgent", "skolenza");
                entity.HasIndex(e => e.AgentInstanceId, "UX_StaffMemberMonitoringAgent_Instance").IsUnique();
                entity.HasIndex(e => e.CredentialHash, "UX_StaffMemberMonitoringAgent_CredentialHash").IsUnique();
                entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId }, "IX_StaffMemberMonitoringAgent_Institution_StaffMember");
                entity.HasIndex(e => e.LastHeartbeatDateTime, "IX_StaffMemberMonitoringAgent_LastHeartbeat");
                entity.Property(e => e.DeviceName).HasMaxLength(200);
                entity.Property(e => e.CredentialHash).HasMaxLength(64);
                entity.Property(e => e.AgentVersion).HasMaxLength(50);
                entity.Property(e => e.LastErrorCode).HasMaxLength(100);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Policy).WithMany().HasForeignKey(e => e.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberMonitoringAgent_Policy");
                entity.HasOne<Institution>().WithMany().HasForeignKey(e => e.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberMonitoringAgent_Institution");
                entity.HasOne<StaffMember>().WithMany().HasForeignKey(e => e.StaffMemberId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberMonitoringAgent_StaffMember");
            });

            modelBuilder.Entity<StaffMemberScreenCapture>(entity =>
            {
                entity.ToTable("StaffMemberScreenCapture", "skolenza");
                entity.HasIndex(e => new { e.MonitoringAgentId, e.CaptureId }, "UX_StaffMemberScreenCapture_Agent_Capture").IsUnique();
                entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.CapturedAtUtc }, "IX_StaffMemberScreenCapture_Institution_StaffMember_Captured");
                entity.Property(e => e.ContentType).HasMaxLength(50);
                entity.Property(e => e.ChecksumSha256).HasMaxLength(64);
                entity.Property(e => e.StorageObjectKey).HasMaxLength(1000);
                entity.HasOne(e => e.MonitoringAgent).WithMany().HasForeignKey(e => e.MonitoringAgentId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberScreenCapture_Agent");
                entity.HasOne<Institution>().WithMany().HasForeignKey(e => e.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberScreenCapture_Institution");
                entity.HasOne<StaffMember>().WithMany().HasForeignKey(e => e.StaffMemberId)
                    .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberScreenCapture_StaffMember");
            });

            modelBuilder.Entity<StaffMemberDeviceEnrollment>(entity =>
            {
                entity.ToTable("StaffMemberDeviceEnrollment", "skolenza");
                entity.HasIndex(e => e.StaffMemberId, "IX_StaffMemberDeviceEnrollment_StaffMemberId");
                entity.HasIndex(e => e.InstitutionDeviceId, "IX_StaffMemberDeviceEnrollment_InstitutionDeviceId");
                entity.HasIndex(e => e.CampusId, "IX_StaffMemberDeviceEnrollment_CampusId");
                entity.HasIndex(e => e.InstitutionCardMasterId, "IX_StaffMemberDeviceEnrollment_InstitutionCardMasterId");
                entity.HasIndex(e => e.InstitutionId, "IX_StaffMemberDeviceEnrollment_InstitutionId");
                entity.Property(e => e.EnrollId).HasMaxLength(100);
                entity.Property(e => e.CardNumber).HasMaxLength(100);
                entity.Property(e => e.FaceImageHash).HasMaxLength(64);
                entity.Property(e => e.FaceDeploymentStatus).HasDefaultValue((short)DeviceCredentialDeploymentStatus.NotConfigured);
                entity.Property(e => e.CardDeploymentStatus).HasDefaultValue((short)DeviceCredentialDeploymentStatus.NotConfigured);
                entity.Property(e => e.PinDeploymentStatus).HasDefaultValue((short)DeviceCredentialDeploymentStatus.NotConfigured);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.StaffMemberDeviceEnrollments)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_Institution");
                entity.HasOne(e => e.StaffMember).WithMany(e => e.StaffMemberDeviceEnrollments)
                    .HasForeignKey(e => e.StaffMemberId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_StaffMember");
                entity.HasOne(e => e.InstitutionDevice).WithMany(e => e.StaffMemberDeviceEnrollments)
                    .HasForeignKey(e => e.InstitutionDeviceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_InstitutionDevice");
                entity.HasOne(e => e.Campus).WithMany(e => e.StaffMemberDeviceEnrollments)
                    .HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_Campus");
                entity.HasOne(e => e.InstitutionCardMaster).WithMany(e => e.StaffMemberDeviceEnrollments)
                    .HasForeignKey(e => e.InstitutionCardMasterId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_InstitutionCardMaster");
                entity.HasOne(e => e.FaceDeviceCommand).WithMany().HasForeignKey(e => e.FaceDeviceCommandId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_FaceDeviceCommand");
                entity.HasOne(e => e.CardDeviceCommand).WithMany().HasForeignKey(e => e.CardDeviceCommandId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_CardDeviceCommand");
                entity.HasOne(e => e.PinDeviceCommand).WithMany().HasForeignKey(e => e.PinDeviceCommandId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_PinDeviceCommand");
                entity.HasOne(e => e.UserActivationDeviceCommand).WithMany().HasForeignKey(e => e.UserActivationDeviceCommandId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceEnrollment_UserActivationDeviceCommand");
            });

            modelBuilder.Entity<StaffMemberDeviceAccessWindow>(entity =>
            {
                entity.ToTable("StaffMemberDeviceAccessWindow", "skolenza");
                entity.HasIndex(e => e.StaffMemberDeviceEnrollmentId, "IX_StaffMemberDeviceAccessWindow_EnrollmentId");
                entity.HasIndex(e => new { e.StaffMemberDeviceEnrollmentId, e.DayOfWeek, e.StartLocalTime }, "UX_StaffMemberDeviceAccessWindow_Enrollment_Day_Start")
                    .IsUnique().HasFilter("((\"IsActive\" = true) AND (\"IsSoftDeleted\" = false))");
                entity.Property(e => e.StartLocalTime).HasColumnType("time without time zone");
                entity.Property(e => e.EndLocalTime).HasColumnType("time without time zone");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.StaffMemberDeviceEnrollment).WithMany(e => e.StaffMemberDeviceAccessWindows)
                    .HasForeignKey(e => e.StaffMemberDeviceEnrollmentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberDeviceAccessWindow_Enrollment");
            });

            modelBuilder.Entity<InstitutionCardMaster>(entity =>
            {
                entity.ToTable("InstitutionCardMaster", "skolenza");
                entity.HasIndex(e => e.InstitutionId, "IX_InstitutionCardMaster_InstitutionId");
                entity.HasIndex(e => new { e.InstitutionId, e.CardNumberLookupHash }, "UX_InstitutionCardMaster_Institution_CardHash")
                    .IsUnique().HasFilter("(\"IsSoftDeleted\" = false)");
                entity.HasIndex(e => new { e.InstitutionId, e.CardStatus, e.IsActive }, "IX_InstitutionCardMaster_AvailableLookup");
                entity.Property(e => e.CardNumberEncrypted).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.CardNumberLookupHash).HasMaxLength(64).IsRequired();
                entity.Property(e => e.CardReference).HasMaxLength(100);
                entity.Property(e => e.PurchaseCurrencyCode).HasMaxLength(3).IsRequired();
                entity.Property(e => e.SupplierName).HasMaxLength(200);
                entity.Property(e => e.SupplierTaxRegistrationNumber).HasMaxLength(100);
                entity.Property(e => e.PurchaseInvoiceNumber).HasMaxLength(100);
                entity.Property(e => e.ForeignTaxLabel).HasMaxLength(100);
                entity.Property(e => e.UnitPurchasePriceExcludingTax).HasPrecision(18, 4);
                entity.Property(e => e.CgstRate).HasPrecision(9, 4);
                entity.Property(e => e.CgstAmount).HasPrecision(18, 4);
                entity.Property(e => e.SgstRate).HasPrecision(9, 4);
                entity.Property(e => e.SgstAmount).HasPrecision(18, 4);
                entity.Property(e => e.IgstRate).HasPrecision(9, 4);
                entity.Property(e => e.IgstAmount).HasPrecision(18, 4);
                entity.Property(e => e.ForeignTaxRate).HasPrecision(9, 4);
                entity.Property(e => e.ForeignTaxAmount).HasPrecision(18, 4);
                entity.Property(e => e.CustomsDutyAmount).HasPrecision(18, 4);
                entity.Property(e => e.FreightAmount).HasPrecision(18, 4);
                entity.Property(e => e.LandedCost).HasPrecision(18, 4);
                entity.Property(e => e.CardStatus).HasDefaultValue((short)InstitutionCardStatus.Available);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.InstitutionCardMasters)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_InstitutionCardMaster_Institution");
            });

            modelBuilder.Entity<StaffMemberWorkArrangement>(entity =>
            {
                entity.ToTable("StaffMemberWorkArrangement", "skolenza");
                entity.HasIndex(e => e.AttendancePolicyId, "IX_StaffMemberWorkArrangement_AttendancePolicyId");
                entity.HasIndex(e => e.PolicyVersionId, "IX_StaffMemberWorkArrangement_PolicyVersionId");
                entity.HasIndex(e => e.StaffMemberId, "IX_StaffMemberWorkArrangement_StaffMemberId");
                entity.HasIndex(e => e.PrimaryCampusId, "IX_StaffMemberWorkArrangement_PrimaryLocationId");
                entity.HasIndex(e => e.InstitutionId, "IX_StaffMemberWorkArrangement_InstitutionId");
                // Multiple active historical/future rows are valid when their effective windows do not overlap.
                entity.HasIndex(e => new { e.InstitutionId, e.StaffMemberId, e.EffectiveFrom }, "IX_StaffMemberWorkArrangement_StaffMember_EffectiveFrom");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.StaffMemberWorkArrangements)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkArrangement_Institution");
                entity.HasOne(e => e.StaffMember).WithMany(e => e.StaffMemberWorkArrangements)
                    .HasForeignKey(e => e.StaffMemberId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkArrangement_StaffMember");
                entity.HasOne(e => e.AttendancePolicy).WithMany(e => e.StaffMemberWorkArrangement)
                    .HasForeignKey(e => e.AttendancePolicyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkArrangement_AttendancePolicy");
                entity.HasOne(e => e.PolicyVersion).WithMany(e => e.StaffMemberWorkArrangements)
                    .HasForeignKey(e => e.PolicyVersionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkArrangement_PolicyVersion");
                entity.HasOne(e => e.PrimaryCampus).WithMany(e => e.StaffMemberWorkArrangement)
                    .HasForeignKey(e => e.PrimaryCampusId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkArrangement_PrimaryLocation");
            });

            modelBuilder.Entity<StaffMemberWorkPattern>(entity =>
            {
                entity.ToTable("StaffMemberWorkPattern", "skolenza");
                entity.HasIndex(e => e.CampusId, "IX_StaffMemberWorkPattern_CampusId");
                entity.HasIndex(e => e.StaffMemberWorkArrangementId, "IX_StaffMemberWorkPattern_WorkArrangementId");
                entity.HasIndex(e => new { e.StaffMemberWorkArrangementId, e.DayOfWeek }, "UX_StaffMemberWorkPattern_Arrangement_Day")
                    .IsUnique().HasFilter("((\"IsActive\" = true) AND (\"IsSoftDeleted\" = false))");
                entity.Property(e => e.IsWorkingDay).HasDefaultValue(true);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.StaffMemberWorkPatterns)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkPattern_Institution");
                entity.HasOne(e => e.StaffMemberWorkArrangement).WithMany(e => e.StaffMemberWorkPattern)
                    .HasForeignKey(e => e.StaffMemberWorkArrangementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkPattern_WorkArrangement");
                entity.HasOne(e => e.Campus).WithMany(e => e.StaffMemberWorkPattern)
                    .HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_StaffMemberWorkPattern_Campus");
            });

            modelBuilder.Entity<StaffMemberWorkModeOverrideRequest>(entity =>
            {
                entity.ToTable("StaffMemberWorkModeOverrideRequest", "skolenza");
                entity.HasIndex(e => e.ApprovalStatus, "IX_WorkModeOverride_ApprovalStatus");
                entity.HasIndex(e => e.StaffMemberId, "IX_WorkModeOverride_StaffMemberId");
                entity.HasIndex(e => new { e.StaffMemberId, e.FromDate, e.ToDate }, "IX_WorkModeOverride_StaffMember_Date");
                entity.HasIndex(e => e.InstitutionId, "IX_WorkModeOverride_InstitutionId");
                entity.Property(e => e.ApprovalStatus).HasDefaultValue((short)1);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.ApprovalRemark).HasMaxLength(500);
                entity.Property(e => e.RejectionRemark).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AddedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Institution).WithMany(e => e.StaffMemberWorkModeOverrideRequests)
                    .HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_WorkModeOverride_Institution");
                entity.HasOne(e => e.StaffMember).WithMany(e => e.StaffMemberWorkModeOverrideRequests)
                    .HasForeignKey(e => e.StaffMemberId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_WorkModeOverride_StaffMember");
                entity.HasOne(e => e.StaffMemberWorkArrangement).WithMany(e => e.StaffMemberWorkModeOverrideRequest)
                    .HasForeignKey(e => e.StaffMemberWorkArrangementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_WorkModeOverride_WorkArrangement");
                entity.HasOne(e => e.Campus).WithMany(e => e.StaffMemberWorkModeOverrideRequest)
                    .HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_WorkModeOverride_Campus");
            });

            #endregion

            ConfigureGenericPolicyFramework(modelBuilder);

            // These legacy objects were retired from the production schema.
            // Keep their source contracts temporarily for dependent-code retirement,
            // but exclude them from EF so startup and active features match the DB.
            modelBuilder.Ignore<BasicMenu>();
            modelBuilder.Ignore<Candidate>();
            modelBuilder.Ignore<CandidateCategorySkill>();
            modelBuilder.Ignore<StaffMemberDeviceAccessWindow>();
            modelBuilder.Ignore<ServiceProvider>();
            modelBuilder.Ignore<InstitutionStaffMemberSectionDefault>();
            modelBuilder.Ignore<TravelMode>();
            modelBuilder.Ignore<WorkflowStage>();
            modelBuilder.Ignore<WorkstationType>();

            modelBuilder.Entity<StaffMemberCountResponseStatsSp>().HasNoKey();

        }

        private static void ConfigureInstitutionMaster<TEntity>(
            ModelBuilder modelBuilder,
            string tableName)
            where TEntity : InstitutionMasterEntity
        {
            modelBuilder.Entity<TEntity>(entity =>
            {
                entity.ToTable(tableName, "skolenza");
            });
        }

        private static void ConfigureAcademicEntity<TEntity>(
            ModelBuilder modelBuilder,
            string tableName)
            where TEntity : InstitutionAcademicEntity
        {
            modelBuilder.Entity<TEntity>(entity =>
            {
                entity.ToTable(tableName, "skolenza");
                entity.HasIndex(item => new { item.InstitutionId, item.Code }).IsUnique();
            });
        }
      
        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
    }



}
