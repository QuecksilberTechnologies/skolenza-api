using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Data.Context;

public partial class SkolenzaDbContext
{
    private static void ConfigureGenericPolicyFramework(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PolicyCategory>(entity =>
        {
            entity.ToTable("PolicyCategory", "skolenza");
            entity.Property(e => e.CategoryCode).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<PolicyStatus>(entity =>
        {
            entity.ToTable("PolicyStatus", "skolenza");
            entity.Property(e => e.StatusCode).HasMaxLength(30);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<PolicyRuleType>(entity =>
        {
            entity.ToTable("PolicyRuleType", "skolenza");
            entity.Property(e => e.RuleTypeCode).HasMaxLength(50);
            entity.Property(e => e.RuleTypeName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<PolicyCategoryRuleType>(entity =>
        {
            entity.ToTable("PolicyCategoryRuleType", "skolenza");
            entity.HasIndex(e => new { e.PolicyCategoryId, e.PolicyRuleTypeId }).IsUnique();
        });

        modelBuilder.Entity<PolicyRuleSettingDefinition>(entity =>
        {
            entity.ToTable("PolicyRuleSettingDefinition", "skolenza");
            entity.Property(e => e.SettingCode).HasMaxLength(80);
            entity.Property(e => e.DisplayName).HasMaxLength(120);
            entity.Property(e => e.DataTypeCode).HasMaxLength(30);
            entity.Property(e => e.DefaultValueJson).HasColumnType("jsonb");
            entity.Property(e => e.RegexPattern).HasMaxLength(500);
            entity.Property(e => e.Placeholder).HasMaxLength(200);
            entity.Property(e => e.HelpText).HasMaxLength(500);
            entity.HasIndex(e => new { e.PolicyRuleTypeId, e.SettingCode }).IsUnique();
        });

        modelBuilder.Entity<PolicyRuleSettingOption>(entity =>
        {
            entity.ToTable("PolicyRuleSettingOption", "skolenza");
            entity.Property(e => e.OptionCode).HasMaxLength(80);
            entity.Property(e => e.OptionLabel).HasMaxLength(120);
            entity.Property(e => e.ValueJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.PolicyRuleSettingDefinitionId, e.OptionCode }).IsUnique();
        });

        modelBuilder.Entity<PolicyRuleSettingDependency>(entity =>
        {
            entity.ToTable("PolicyRuleSettingDependency", "skolenza");
            entity.Property(e => e.OperatorCode).HasMaxLength(30);
            entity.Property(e => e.ActionCode).HasMaxLength(30);
            entity.Property(e => e.ExpectedValueJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.PolicyRuleSettingDefinitionId, e.DependsOnSettingDefinitionId, e.ActionCode }).IsUnique();
        });

        modelBuilder.Entity<PolicyDocumentType>(entity =>
        {
            entity.ToTable("PolicyDocumentType", "skolenza");
            entity.Property(e => e.DocumentTypeCode).HasMaxLength(30);
            entity.Property(e => e.DocumentTypeName).HasMaxLength(100);
        });

        modelBuilder.Entity<PolicyType>(entity =>
        {
            entity.ToTable("PolicyType", "skolenza");
            entity.Property(e => e.PolicyName).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PolicyTypeCode).HasMaxLength(50);
            entity.Property(e => e.DefaultCurrencyCode).HasMaxLength(3);
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policy", "skolenza");
            entity.Property(e => e.PolicyCode).HasMaxLength(50);
            entity.Property(e => e.PolicyName).HasMaxLength(200);
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.DefaultCurrencyCode).HasMaxLength(3);
            entity.HasIndex(e => new { e.InstitutionId, e.PolicyCode }).IsUnique();
        });

        modelBuilder.Entity<PolicyVersion>(entity =>
        {
            entity.ToTable("PolicyVersion", "skolenza");
            entity.Property(e => e.EffectiveFrom).HasColumnType("date");
            entity.Property(e => e.EffectiveTo).HasColumnType("date");
            entity.Property(e => e.ChangeSummary).HasMaxLength(1000);
            entity.HasIndex(e => new { e.PolicyId, e.VersionNumber }).IsUnique();
            entity.Property(e => e.ApprovedContentChecksumSha256).HasMaxLength(64);
            entity.HasOne(e => e.Policy).WithMany(e => e.PolicyVersions)
                .HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttendancePolicyVersionConfiguration>(entity =>
        {
            entity.ToTable("AttendancePolicyVersionConfiguration", "skolenza");
            entity.HasIndex(e => e.PolicyVersionId).IsUnique();
            entity.HasIndex(e => new { e.InstitutionId, e.PolicyVersionId }).IsUnique();
            entity.HasOne(e => e.PolicyVersion).WithOne(e => e.AttendanceConfiguration)
                .HasForeignKey<AttendancePolicyVersionConfiguration>(e => e.PolicyVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyRule>(entity =>
        {
            entity.ToTable("PolicyRule", "skolenza");
            entity.Property(e => e.RuleName).HasMaxLength(150);
            entity.Property(e => e.RuleConfiguration).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.PolicyVersionId, e.RuleOrder }).IsUnique();
        });

        modelBuilder.Entity<PolicyVersionLeaveType>(entity =>
        {
            entity.ToTable("PolicyVersionLeaveType", "skolenza");
            entity.HasIndex(e => new { e.PolicyVersionId, e.LeaveTypeId }).IsUnique();
            entity.HasOne<PolicyVersion>().WithMany().HasForeignKey(e => e.PolicyVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<LeaveType>().WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyRuleLeaveType>(entity =>
        {
            entity.ToTable("PolicyRuleLeaveType", "skolenza");
            entity.HasIndex(e => new { e.PolicyRuleId, e.LeaveTypeId }).IsUnique();
            entity.HasOne<PolicyRule>().WithMany().HasForeignKey(e => e.PolicyRuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<LeaveType>().WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyApplicabilityLeaveType>(entity =>
        {
            entity.ToTable("PolicyApplicabilityLeaveType", "skolenza");
            entity.HasIndex(e => new { e.PolicyApplicabilityId, e.LeaveTypeId }).IsUnique();
            entity.HasOne<PolicyApplicability>().WithMany().HasForeignKey(e => e.PolicyApplicabilityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<LeaveType>().WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyApplicability>(entity =>
        {
            entity.ToTable("PolicyApplicability", "skolenza");
            entity.Property(e => e.EffectiveFrom).HasColumnType("date");
            entity.Property(e => e.EffectiveTo).HasColumnType("date");
            entity.HasOne<StaffPositionMaster>().WithMany()
                .HasForeignKey(e => e.StaffPositionMasterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyAssignment>(entity =>
        {
            entity.ToTable("PolicyAssignment", "skolenza");
            entity.Property(e => e.EffectiveFrom).HasColumnType("date");
            entity.Property(e => e.EffectiveTo).HasColumnType("date");
            entity.HasIndex(e => new { e.PolicyVersionId, e.StaffMemberId, e.EffectiveFrom }).IsUnique();
        });

        modelBuilder.Entity<PolicyException>(entity =>
        {
            entity.ToTable("PolicyException", "skolenza");
            entity.Property(e => e.OverrideConfiguration).HasColumnType("jsonb");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.EffectiveFrom).HasColumnType("date");
            entity.Property(e => e.EffectiveTo).HasColumnType("date");
        });

        modelBuilder.Entity<PolicyDocument>(entity =>
        {
            entity.ToTable("PolicyDocument", "skolenza");
            entity.Property(e => e.DocumentTitle).HasMaxLength(200);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.StorageProvider).HasMaxLength(30);
            entity.Property(e => e.ObjectKey).HasMaxLength(1000);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.ChecksumSha256).HasMaxLength(64);
            entity.Property(e => e.LanguageCode).HasMaxLength(10);
        });

        modelBuilder.Entity<PolicyApprovalStage>(entity =>
        {
            entity.ToTable("PolicyApprovalStage", "skolenza");
            entity.Property(e => e.StageName).HasMaxLength(100);
            entity.HasIndex(e => new { e.InstitutionId, e.PolicyCategoryId, e.StageOrder }).IsUnique();
        });

        modelBuilder.Entity<PolicyApprovalHistory>(entity =>
        {
            entity.ToTable("PolicyApprovalHistory", "skolenza");
            entity.Property(e => e.Comments).HasMaxLength(1000);
            entity.HasIndex(e => new { e.PolicyVersionId, e.SequenceNumber }).IsUnique();
            entity.Property(e => e.ContentChecksumSha256).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<PolicyAcknowledgement>(entity =>
        {
            entity.ToTable("PolicyAcknowledgement", "skolenza");
            entity.Property(e => e.SourceIpHash).HasMaxLength(64);
            entity.Property(e => e.EvidenceJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.PolicyVersionId, e.StaffMemberId }).IsUnique();
        });

        modelBuilder.Entity<PolicyChangeAudit>(entity =>
        {
            entity.ToTable("PolicyChangeAudit", "skolenza");
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.ActionName).HasMaxLength(50);
            entity.Property(e => e.BeforeData).HasColumnType("jsonb");
            entity.Property(e => e.AfterData).HasColumnType("jsonb");
        });
    }
}
