using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Policy;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class GenericPolicyRepository(SkolenzaDbContext context) : IGenericPolicyRepository
{
    public async Task<IReadOnlyList<PolicyLookupResponseDTO>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await context.PolicyCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CategoryName)
            .Select(x => new PolicyLookupResponseDTO(x.Id, x.CategoryCode, x.CategoryName, x.Description)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PolicyLookupResponseDTO>> GetStatusesAsync(CancellationToken cancellationToken) =>
        await context.PolicyStatuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id)
            .Select(x => new PolicyLookupResponseDTO(x.Id, x.StatusCode, x.StatusName)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PolicyLookupResponseDTO>> GetRuleTypesAsync(CancellationToken cancellationToken) =>
        await context.PolicyRuleTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.RuleTypeName)
            .Select(x => new PolicyLookupResponseDTO(x.Id, x.RuleTypeCode, x.RuleTypeName, x.Description)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PolicyLeaveTypeResponseDTO>> GetLeaveTypesAsync(long institutionId, CancellationToken cancellationToken) =>
        await context.LeaveTypes.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDeleted != true)
            .OrderBy(x => x.LeaveName)
            .Select(x => new PolicyLeaveTypeResponseDTO(x.Id, x.LeaveName))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PolicyRuleDefinitionResponseDTO>> GetRuleDefinitionsAsync(CancellationToken cancellationToken)
    {
        var mappings = await (
            from mapping in context.PolicyCategoryRuleTypes.AsNoTracking()
            join category in context.PolicyCategories.AsNoTracking() on mapping.PolicyCategoryId equals category.Id
            join ruleType in context.PolicyRuleTypes.AsNoTracking() on mapping.PolicyRuleTypeId equals ruleType.Id
            where mapping.IsActive && category.IsActive && ruleType.IsActive
            orderby category.CategoryName, mapping.DisplayOrder
            select new { mapping, category.CategoryCode, ruleType.RuleTypeCode, ruleType.RuleTypeName })
            .ToListAsync(cancellationToken);
        var ruleTypeIds = mappings.Select(x => x.mapping.PolicyRuleTypeId).Distinct().ToArray();
        var settings = await context.PolicyRuleSettingDefinitions.AsNoTracking()
            .Where(x => ruleTypeIds.Contains(x.PolicyRuleTypeId) && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
        var settingIds = settings.Select(x => x.Id).ToArray();
        var options = await context.PolicyRuleSettingOptions.AsNoTracking()
            .Where(x => settingIds.Contains(x.PolicyRuleSettingDefinitionId) && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
        var dependencies = await context.PolicyRuleSettingDependencies.AsNoTracking()
            .Where(x => settingIds.Contains(x.PolicyRuleSettingDefinitionId))
            .ToListAsync(cancellationToken);
        var settingCodeById = settings.ToDictionary(x => x.Id, x => x.SettingCode);

        return mappings.Select(x => new PolicyRuleDefinitionResponseDTO(
            x.CategoryCode,
            x.RuleTypeCode,
            x.RuleTypeName,
            x.mapping.IsRequired,
            x.mapping.AllowMultiple,
            x.mapping.DisplayOrder,
            settings.Where(setting => setting.PolicyRuleTypeId == x.mapping.PolicyRuleTypeId)
                .Select(setting => new PolicyRuleSettingDefinitionResponseDTO(
                    setting.SettingCode,
                    setting.DisplayName,
                    setting.DataTypeCode,
                    setting.IsRequired,
                    setting.AllowMultiple,
                    setting.DefaultValueJson,
                    setting.MinimumValue,
                    setting.MaximumValue,
                    setting.RegexPattern,
                    setting.Placeholder,
                    setting.HelpText,
                    setting.DisplayOrder,
                    options.Where(option => option.PolicyRuleSettingDefinitionId == setting.Id)
                        .Select(option => new PolicyRuleSettingOptionResponseDTO(
                            option.OptionCode, option.OptionLabel, option.ValueJson, option.DisplayOrder)).ToList(),
                    dependencies.Where(dependency => dependency.PolicyRuleSettingDefinitionId == setting.Id)
                        .Select(dependency => new PolicyRuleSettingDependencyResponseDTO(
                            settingCodeById[dependency.DependsOnSettingDefinitionId], dependency.OperatorCode,
                            dependency.ExpectedValueJson, dependency.ActionCode)).ToList()))
                .ToList())).ToList();
    }

    public async Task<IReadOnlyList<PolicyLookupResponseDTO>> GetDocumentTypesAsync(CancellationToken cancellationToken) =>
        await context.PolicyDocumentTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id)
            .Select(x => new PolicyLookupResponseDTO(x.Id, x.DocumentTypeCode, x.DocumentTypeName)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PolicyTypeResponseDTO>> GetPolicyTypesAsync(long institutionId, bool isActive, CancellationToken cancellationToken) =>
        await (from policyType in context.PolicyTypes.AsNoTracking()
               join category in context.PolicyCategories.AsNoTracking()
                   on policyType.PolicyCategoryId equals category.Id
               where policyType.InstitutionId == institutionId && policyType.IsActive == isActive
                   && policyType.IsSoftDelete != true && category.IsActive
               orderby policyType.PolicyName
               select new PolicyTypeResponseDTO(
                   policyType.Id,
                   policyType.PolicyTypeCode ?? string.Empty,
                   policyType.PolicyName,
                   policyType.Description,
                   policyType.PolicyCategoryId,
                   category.CategoryCode,
                   policyType.DefaultCurrencyCode,
                   policyType.IsActive == true))
            .ToListAsync(cancellationToken);

    public async Task<PolicyTypeResponseDTO> CreatePolicyTypeAsync(long institutionId, long actorId, CreateGenericPolicyTypeRequestDTO dto, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(dto.PolicyTypeCode);
        var categoryCode = await GetCategoryCodeAsync(dto.PolicyCategoryId, cancellationToken);
        if (await context.PolicyTypes.AnyAsync(x => x.InstitutionId == institutionId && x.PolicyTypeCode == code && x.IsSoftDelete != true, cancellationToken))
        {
            throw new ConflictException("Policy type code already exists for this institution.");
        }

        var entity = new PolicyType
        {
            InstitutionId = institutionId,
            PolicyTypeCode = code,
            PolicyName = dto.PolicyName.Trim(),
            Description = dto.Description?.Trim(),
            PolicyCategoryId = dto.PolicyCategoryId,
            DefaultCurrencyCode = NormalizeCurrency(dto.DefaultCurrencyCode),
            IsActive = true,
            IsSoftDelete = false,
            IsStructured = true,
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
        context.PolicyTypes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return MapType(entity, categoryCode);
    }

    public async Task<PolicyTypeResponseDTO> UpdatePolicyTypeAsync(long institutionId, long actorId, UpdateGenericPolicyTypeRequestDTO dto, CancellationToken cancellationToken)
    {
        var entity = await context.PolicyTypes.FirstOrDefaultAsync(x => x.Id == dto.Id && x.InstitutionId == institutionId && x.IsSoftDelete != true, cancellationToken)
            ?? throw new NotFoundException("Policy type was not found.");
        var code = NormalizeCode(dto.PolicyTypeCode);
        var categoryCode = await GetCategoryCodeAsync(dto.PolicyCategoryId, cancellationToken);
        if (await context.PolicyTypes.AnyAsync(x => x.Id != dto.Id && x.InstitutionId == institutionId && x.PolicyTypeCode == code && x.IsSoftDelete != true, cancellationToken))
        {
            throw new ConflictException("Policy type code already exists for this institution.");
        }
        entity.PolicyTypeCode = code;
        entity.PolicyName = dto.PolicyName.Trim();
        entity.Description = dto.Description?.Trim();
        entity.PolicyCategoryId = dto.PolicyCategoryId;
        entity.DefaultCurrencyCode = NormalizeCurrency(dto.DefaultCurrencyCode);
        entity.IsActive = dto.IsActive;
        entity.UpdateById = actorId;
        entity.UpdateDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return MapType(entity, categoryCode);
    }

    public async Task<bool> ChangePolicyTypeStatusAsync(long institutionId, long actorId, ChangePolicyTypeStatusRequestDTO dto, CancellationToken cancellationToken)
    {
        var entity = await context.PolicyTypes.FirstOrDefaultAsync(x => x.Id == dto.Id && x.InstitutionId == institutionId && x.IsSoftDelete != true, cancellationToken)
            ?? throw new NotFoundException("Policy type was not found.");
        entity.IsActive = dto.IsActive;
        entity.UpdateById = actorId;
        entity.UpdateDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<PolicySummaryResponseDTO> Items, int Total)> GetPoliciesAsync(long institutionId, PolicyListRequestDTO dto, CancellationToken cancellationToken)
    {
        var query = context.Policies.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (dto.PolicyTypeId.HasValue) query = query.Where(x => x.PolicyTypeId == dto.PolicyTypeId);
        if (dto.StatusId.HasValue) query = query.Where(x => context.PolicyVersions.Any(version => version.PolicyId == x.Id && version.InstitutionId == institutionId && version.PolicyStatusId == dto.StatusId));
        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.Trim().ToLower();
            query = query.Where(x => x.PolicyCode.ToLower().Contains(search) || x.PolicyName.ToLower().Contains(search));
        }
        var total = await query.CountAsync(cancellationToken);
        var baseItems = await query.OrderBy(x => x.PolicyName).Skip((dto.PageNumber - 1) * dto.PageSize).Take(dto.PageSize).ToListAsync(cancellationToken);
        var policyIds = baseItems.Select(x => x.Id).ToArray();
        var versions = await context.PolicyVersions.AsNoTracking().Where(x => policyIds.Contains(x.PolicyId))
            .OrderByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);
        var statuses = await context.PolicyStatuses.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.StatusName, cancellationToken);
        var items = baseItems.Select(p =>
        {
            var v = versions.FirstOrDefault(x => x.PolicyId == p.Id
                && (!dto.StatusId.HasValue || x.PolicyStatusId == dto.StatusId));
            return new PolicySummaryResponseDTO(p.Id, p.PolicyCode, p.PolicyName, p.PolicyTypeId, p.IsActive, v?.Id, v?.VersionNumber, v != null && statuses.TryGetValue(v.PolicyStatusId, out var status) ? status : null);
        }).Where(x => !dto.StatusId.HasValue || x.CurrentVersionId.HasValue).ToList();
        return (items, total);
    }

    public Task<PolicyDetailResponseDTO> GetPolicyAsync(long institutionId, long policyId, CancellationToken cancellationToken) => GetDetailAsync(institutionId, policyId, null, cancellationToken);

    public Task<PolicyDetailResponseDTO> GetPolicyVersionAsync(long institutionId, long policyId, long policyVersionId, CancellationToken cancellationToken) =>
        GetDetailAsync(institutionId, policyId, policyVersionId, cancellationToken);

    public async Task<PolicyDetailResponseDTO> CreatePolicyAsync(long institutionId, long actorId, CreatePolicyRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        ValidateDates(dto.EffectiveFrom, dto.EffectiveTo);
        ValidateRulesAndScopes(dto.Rules, dto.Applicability);
        await ValidateLeaveTargetsAsync(institutionId, dto.PolicyTypeId, dto.LeaveTypeIds, dto.Rules, dto.Applicability, cancellationToken);
        await ValidatePolicyRulesAsync(institutionId, dto.PolicyTypeId, dto.Rules, cancellationToken);
        await ValidateScopeReferencesAsync(institutionId, dto.Applicability, cancellationToken);
        await ValidatePolicyReferencesAsync(institutionId, dto.OwnerDepartmentId, dto.Rules, cancellationToken);
        await ValidateAttendanceConfigurationAsync(institutionId, dto.PolicyTypeId, dto.AttendanceConfiguration, cancellationToken);
        var code = NormalizeCode(dto.PolicyCode);
        if (!await context.PolicyTypes.AnyAsync(x => x.Id == dto.PolicyTypeId && x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDelete != true, cancellationToken))
            throw new ValidationErrorException("PolicyTypeId is not active for this institution.");
        if (await context.Policies.AnyAsync(x => x.InstitutionId == institutionId && x.PolicyCode == code && !x.IsSoftDeleted, cancellationToken))
            throw new ConflictException("Policy code already exists for this institution.");
        var name = dto.PolicyName.Trim();
        var normalizedName = name.ToUpper();
        if (await context.Policies.AnyAsync(x => x.InstitutionId == institutionId
            && x.PolicyName.Trim().ToUpper() == normalizedName
            && !x.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException(
                $"A policy named '{name}' already exists. Open the existing policy and create its next version instead.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var policy = new Policy { InstitutionId = institutionId, PolicyTypeId = dto.PolicyTypeId, PolicyCode = code, PolicyName = name, Summary = dto.Summary?.Trim(), OwnerDepartmentId = dto.OwnerDepartmentId, DefaultCurrencyCode = NormalizeCurrency(dto.DefaultCurrencyCode), IsActive = true, AddedById = actorId, AddedDateTime = now };
        context.Policies.Add(policy);
        await context.SaveChangesAsync(cancellationToken);
        var version = new PolicyVersion { InstitutionId = institutionId, PolicyId = policy.Id, VersionNumber = 1, PolicyStatusId = statusIds.Draft, EffectiveFrom = dto.EffectiveFrom, EffectiveTo = dto.EffectiveTo, ChangeSummary = dto.ChangeSummary?.Trim(), RuleSchemaVersion = 1, IsActive = true, AddedById = actorId, AddedDateTime = now };
        context.PolicyVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);
        await UpsertAttendanceConfigurationAsync(institutionId, actorId, version.Id, dto.AttendanceConfiguration, now, cancellationToken);
        var added = AddRulesAndApplicability(institutionId, actorId, version.Id, dto.Rules, dto.Applicability, now);
        AddAudit(institutionId, actorId, policy.Id, version.Id, "Policy", policy.Id, "CREATE", null, JsonSerializer.Serialize(new { policy.PolicyCode, policy.PolicyName }));
        await context.SaveChangesAsync(cancellationToken);
        AddLeaveTargetMappings(institutionId, actorId, version.Id, dto.LeaveTypeIds, added);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailAsync(institutionId, policy.Id, version.Id, cancellationToken);
    }

    public async Task<PolicyDetailResponseDTO> UpdateDraftAsync(long institutionId, long actorId, UpdatePolicyDraftRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        ValidateDates(dto.EffectiveFrom, dto.EffectiveTo);
        ValidateRulesAndScopes(dto.Rules, dto.Applicability);
        await ValidateLeaveTargetsAsync(institutionId, dto.PolicyTypeId, dto.LeaveTypeIds, dto.Rules, dto.Applicability, cancellationToken);
        await ValidatePolicyRulesAsync(institutionId, dto.PolicyTypeId, dto.Rules, cancellationToken);
        await ValidateScopeReferencesAsync(institutionId, dto.Applicability, cancellationToken);
        await ValidatePolicyReferencesAsync(institutionId, dto.OwnerDepartmentId, dto.Rules, cancellationToken);
        await ValidateAttendanceConfigurationAsync(institutionId, dto.PolicyTypeId, dto.AttendanceConfiguration, cancellationToken);
        var policy = await context.Policies.FirstOrDefaultAsync(x => x.Id == dto.PolicyId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken) ?? throw new NotFoundException("Policy was not found.");
        var version = await context.PolicyVersions.FirstOrDefaultAsync(x => x.Id == dto.PolicyVersionId && x.PolicyId == policy.Id && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("Policy version was not found.");
        if (version.PolicyStatusId != statusIds.Draft && version.PolicyStatusId != statusIds.Rejected) throw new ConflictException("Only draft or rejected versions can be edited.");
        var code = NormalizeCode(dto.PolicyCode);
        if (!await context.PolicyTypes.AnyAsync(x => x.Id == dto.PolicyTypeId && x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDelete != true, cancellationToken)) throw new ValidationErrorException("PolicyTypeId is not active for this institution.");
        if (await context.Policies.AnyAsync(x => x.Id != policy.Id && x.InstitutionId == institutionId && x.PolicyCode == code && !x.IsSoftDeleted, cancellationToken)) throw new ConflictException("Policy code already exists for this institution.");
        var name = dto.PolicyName.Trim();
        var normalizedName = name.ToUpper();
        if (await context.Policies.AnyAsync(x => x.Id != policy.Id
            && x.InstitutionId == institutionId
            && x.PolicyName.Trim().ToUpper() == normalizedName
            && !x.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException(
                $"A policy named '{name}' already exists. Open the existing policy and create its next version instead.");
        }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        policy.PolicyTypeId = dto.PolicyTypeId; policy.PolicyCode = code; policy.PolicyName = name; policy.Summary = dto.Summary?.Trim(); policy.OwnerDepartmentId = dto.OwnerDepartmentId; policy.DefaultCurrencyCode = NormalizeCurrency(dto.DefaultCurrencyCode); policy.UpdatedById = actorId; policy.UpdatedDateTime = DateTime.UtcNow;
        version.EffectiveFrom = dto.EffectiveFrom; version.EffectiveTo = dto.EffectiveTo; version.ChangeSummary = dto.ChangeSummary?.Trim(); version.UpdatedById = actorId; version.UpdatedDateTime = DateTime.UtcNow; version.PolicyStatusId = statusIds.Draft;
        context.PolicyRules.RemoveRange(context.PolicyRules.Where(x => x.PolicyVersionId == version.Id && x.InstitutionId == institutionId));
        context.PolicyApplicabilities.RemoveRange(context.PolicyApplicabilities.Where(x => x.PolicyVersionId == version.Id && x.InstitutionId == institutionId));
        context.PolicyVersionLeaveTypes.RemoveRange(context.PolicyVersionLeaveTypes.Where(x => x.PolicyVersionId == version.Id && x.InstitutionId == institutionId));
        await UpsertAttendanceConfigurationAsync(institutionId, actorId, version.Id, dto.AttendanceConfiguration, DateTime.UtcNow, cancellationToken);

        // Flush the old version graph before inserting its replacement. PolicyRule and
        // PolicyVersionLeaveType have version-scoped unique keys, so batching deletes and
        // inserts in one SaveChanges can attempt the inserts before PostgreSQL has released
        // the existing keys. The surrounding transaction keeps this replacement atomic.
        await context.SaveChangesAsync(cancellationToken);

        var added = AddRulesAndApplicability(institutionId, actorId, version.Id, dto.Rules, dto.Applicability, DateTime.UtcNow);
        AddAudit(institutionId, actorId, policy.Id, version.Id, "PolicyVersion", version.Id, "UPDATE_DRAFT", null, null);
        await context.SaveChangesAsync(cancellationToken);
        AddLeaveTargetMappings(institutionId, actorId, version.Id, dto.LeaveTypeIds, added);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailAsync(institutionId, policy.Id, version.Id, cancellationToken);
    }

    public async Task<PolicyDetailResponseDTO> CloneVersionAsync(long institutionId, long actorId, ClonePolicyVersionRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var policy = await context.Policies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PolicyId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken) ?? throw new NotFoundException("Policy was not found.");
        var source = await context.PolicyVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.SourceVersionId && x.PolicyId == policy.Id && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("Source version was not found.");
        if (await context.PolicyVersions.AnyAsync(x => x.PolicyId == policy.Id && x.PolicyStatusId == statusIds.Draft, cancellationToken)) throw new ConflictException("A draft version already exists.");
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var number = await context.PolicyVersions.Where(x => x.PolicyId == policy.Id).MaxAsync(x => x.VersionNumber, cancellationToken) + 1;
        var version = new PolicyVersion { InstitutionId = institutionId, PolicyId = policy.Id, VersionNumber = number, PolicyStatusId = statusIds.Draft, EffectiveFrom = dto.EffectiveFrom, ChangeSummary = dto.ChangeSummary?.Trim(), RuleSchemaVersion = source.RuleSchemaVersion, IsActive = true, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        context.PolicyVersions.Add(version); await context.SaveChangesAsync(cancellationToken);
        var rules = await context.PolicyRules.AsNoTracking().Where(x => x.PolicyVersionId == source.Id).ToListAsync(cancellationToken);
        var scopes = await context.PolicyApplicabilities.AsNoTracking().Where(x => x.PolicyVersionId == source.Id).ToListAsync(cancellationToken);
        var attendanceConfiguration = await context.AttendancePolicyVersionConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PolicyVersionId == source.Id && x.InstitutionId == institutionId, cancellationToken);
        var clonedRules = rules.Select(x => new PolicyRule { InstitutionId = institutionId, PolicyVersionId = version.Id, PolicyRuleTypeId = x.PolicyRuleTypeId, RuleName = x.RuleName, RuleOrder = x.RuleOrder, RuleConfiguration = x.RuleConfiguration, IsActive = x.IsActive, AddedById = actorId, AddedDateTime = DateTime.UtcNow }).ToList();
        var clonedScopes = scopes.Select(x => CloneScope(x, version.Id, actorId)).ToList();
        context.PolicyRules.AddRange(clonedRules);
        context.PolicyApplicabilities.AddRange(clonedScopes);
        if (attendanceConfiguration != null)
        {
            context.AttendancePolicyVersionConfigurations.Add(MapAttendanceConfiguration(
                institutionId,
                actorId,
                version.Id,
                ToDto(attendanceConfiguration),
                DateTime.UtcNow));
        }
        AddAudit(institutionId, actorId, policy.Id, version.Id, "PolicyVersion", version.Id, "CLONE", null, JsonSerializer.Serialize(new { SourceVersionId = source.Id }));
        await context.SaveChangesAsync(cancellationToken);
        var sourceVersionLeaveTypes = await context.PolicyVersionLeaveTypes.AsNoTracking().Where(x => x.PolicyVersionId == source.Id && x.IsActive).ToListAsync(cancellationToken);
        context.PolicyVersionLeaveTypes.AddRange(sourceVersionLeaveTypes.Select(x => new PolicyVersionLeaveType { InstitutionId = institutionId, PolicyVersionId = version.Id, LeaveTypeId = x.LeaveTypeId, IsActive = true, AddedById = actorId, AddedDateTime = DateTime.UtcNow }));
        var sourceRuleLinks = await context.PolicyRuleLeaveTypes.AsNoTracking().Where(x => rules.Select(rule => rule.Id).Contains(x.PolicyRuleId)).ToListAsync(cancellationToken);
        context.PolicyRuleLeaveTypes.AddRange(rules.Zip(clonedRules).SelectMany(pair => sourceRuleLinks.Where(x => x.PolicyRuleId == pair.First.Id).Select(x => new PolicyRuleLeaveType { InstitutionId = institutionId, PolicyRuleId = pair.Second.Id, LeaveTypeId = x.LeaveTypeId })));
        var sourceScopeLinks = await context.PolicyApplicabilityLeaveTypes.AsNoTracking().Where(x => scopes.Select(scope => scope.Id).Contains(x.PolicyApplicabilityId)).ToListAsync(cancellationToken);
        context.PolicyApplicabilityLeaveTypes.AddRange(scopes.Zip(clonedScopes).SelectMany(pair => sourceScopeLinks.Where(x => x.PolicyApplicabilityId == pair.First.Id).Select(x => new PolicyApplicabilityLeaveType { InstitutionId = institutionId, PolicyApplicabilityId = pair.Second.Id, LeaveTypeId = x.LeaveTypeId })));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailAsync(institutionId, policy.Id, version.Id, cancellationToken);
    }

    public async Task<PolicyDetailResponseDTO> TransitionAsync(long institutionId, long actorId, PolicyTransitionRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var version = await context.PolicyVersions
            .FirstOrDefaultAsync(x => x.Id == dto.PolicyVersionId && x.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Policy version was not found.");
        var action = dto.Action.Trim().ToUpperInvariant();

        if (action is "SUBMIT" or "PUBLISH")
        {
            await EnsureRequiredPolicyDocumentAsync(institutionId, version.Id, cancellationToken);
        }

        if (version.PolicyStatusId == statusIds.UnderReview && action is "APPROVE" or "REJECT")
        {
            return await RecordApprovalDecisionAsync(institutionId, actorId, version, action, dto.Comments, statusIds, cancellationToken);
        }

        var target = (version.PolicyStatusId, action) switch
        {
            (var current, "SUBMIT") when current == statusIds.Draft => statusIds.UnderReview,
            (var current, "SUBMIT") when current == statusIds.Rejected => statusIds.UnderReview,
            (var current, "PUBLISH") when current == statusIds.Approved => statusIds.Published,
            (var current, "ARCHIVE") when current == statusIds.Published => statusIds.Archived,
            _ => throw new ConflictException($"Action {action} is invalid for the current policy status.")
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        version.PolicyStatusId = target;
        version.UpdatedById = actorId;
        version.UpdatedDateTime = DateTime.UtcNow;
        if (target == statusIds.Published)
        {
            var currentContentChecksum = await ComputeVersionContentChecksumAsync(
                institutionId,
                version.Id,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(version.ApprovedContentChecksumSha256)
                || !string.Equals(version.ApprovedContentChecksumSha256, currentContentChecksum,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "The approved policy content no longer matches this version. Submit it for approval again before publishing.");
            }

            var categoryId = await GetPolicyCategoryIdAsync(institutionId, version.PolicyId, cancellationToken);
            var categoryCode = categoryId.HasValue
                ? await context.PolicyCategories.AsNoTracking()
                    .Where(x => x.Id == categoryId.Value)
                    .Select(x => x.CategoryCode)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            if (string.Equals(categoryCode, AppConstants.PolicyCategoryCodes.Attendance, StringComparison.OrdinalIgnoreCase)
                && !await context.AttendancePolicyVersionConfigurations.AsNoTracking()
                    .AnyAsync(x => x.InstitutionId == institutionId && x.PolicyVersionId == version.Id, cancellationToken))
            {
                throw new ConflictException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
            }

            var mandatoryStages = await context.PolicyApprovalStages.AsNoTracking()
                .Where(x => x.InstitutionId == institutionId && x.IsActive && x.IsMandatory
                    && (x.PolicyCategoryId == null || x.PolicyCategoryId == categoryId))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            if (mandatoryStages.Count > 0 && version.ApprovedDateTime == null)
            {
                throw new ConflictException("All mandatory approval stages must be completed before publishing.");
            }

            await context.PolicyVersions
                .Where(x => x.PolicyId == version.PolicyId && x.Id != version.Id && x.IsCurrent)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsCurrent, false), cancellationToken);
            version.PublishedById = actorId;
            version.PublishedDateTime = DateTime.UtcNow;
            version.IsCurrent = true;
        }
        if (target == statusIds.Archived)
        {
            version.IsCurrent = false;
        }

        AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyVersion", version.Id, action, null,
            JsonSerializer.Serialize(new { StatusId = target, dto.Comments }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailAsync(institutionId, version.PolicyId, version.Id, cancellationToken);
    }

    private async Task<PolicyDetailResponseDTO> RecordApprovalDecisionAsync(
        long institutionId,
        long actorId,
        PolicyVersion version,
        string action,
        string? comments,
        PolicyStatusIds statusIds,
        CancellationToken cancellationToken)
    {
        var categoryId = await GetPolicyCategoryIdAsync(institutionId, version.PolicyId, cancellationToken);
        var stages = await context.PolicyApprovalStages
            .Where(x => x.InstitutionId == institutionId && x.IsActive && x.IsMandatory
                && (x.PolicyCategoryId == null || x.PolicyCategoryId == categoryId))
            .OrderBy(x => x.StageOrder)
            .ToListAsync(cancellationToken);

        if (stages.Count == 0)
        {
            version.PolicyStatusId = action == "APPROVE" ? statusIds.Approved : statusIds.Rejected;
            version.UpdatedById = actorId;
            version.UpdatedDateTime = DateTime.UtcNow;
            if (action == "APPROVE")
            {
                version.ApprovedById = actorId;
                version.ApprovedDateTime = DateTime.UtcNow;
                version.ApprovedContentChecksumSha256 = await ComputeVersionContentChecksumAsync(
                    institutionId,
                    version.Id,
                    cancellationToken);
            }
            AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyVersion", version.Id, action, null,
                JsonSerializer.Serialize(new { version.PolicyStatusId, Comments = comments }));
            await context.SaveChangesAsync(cancellationToken);
            return await GetDetailAsync(institutionId, version.PolicyId, version.Id, cancellationToken);
        }

        var history = await context.PolicyApprovalHistories
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == version.Id)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);
        var lastRejectionSequence = history.Where(x => x.ActionType == 2)
            .Select(x => x.SequenceNumber)
            .DefaultIfEmpty(0)
            .Max();
        var currentCycle = history.Where(x => x.SequenceNumber > lastRejectionSequence).ToList();
        var currentStage = stages.FirstOrDefault(stage =>
            currentCycle.Count(x => x.PolicyApprovalStageId == stage.Id && x.ActionType == 1) < stage.MinimumApprovals);
        if (currentStage == null)
        {
            throw new ConflictException("All mandatory approval stages are already complete.");
        }

        if (currentStage.ApproverRoleId.HasValue)
        {
            var hasRole = await context.UserRoles.AsNoTracking().AnyAsync(x =>
                x.StaffMemberId == actorId && x.RoleId == currentStage.ApproverRoleId && x.IsActive
                && x.IsSoftDeleted != true && x.Role != null && x.Role.InstitutionId == institutionId,
                cancellationToken);
            if (!hasRole)
            {
                throw new ForbiddenAccessException("The current approval stage requires a different approver role.");
            }
        }
        if (currentCycle.Any(x => x.PolicyApprovalStageId == currentStage.Id && x.ActionById == actorId && x.ActionType == 1))
        {
            throw new ConflictException("This approver has already approved the current stage.");
        }

        var nextSequence = history.Select(x => x.SequenceNumber).DefaultIfEmpty(0).Max() + 1;
        var contentChecksum = await ComputeVersionContentChecksumAsync(
            institutionId,
            version.Id,
            cancellationToken);
        context.PolicyApprovalHistories.Add(new PolicyApprovalHistory
        {
            InstitutionId = institutionId,
            PolicyVersionId = version.Id,
            PolicyApprovalStageId = currentStage.Id,
            ActionType = action == "APPROVE" ? (short)1 : (short)2,
            ActionById = actorId,
            ActionDateTime = DateTime.UtcNow,
            Comments = comments?.Trim(),
            SequenceNumber = nextSequence,
            ContentChecksumSha256 = contentChecksum
        });

        if (action == "REJECT")
        {
            version.PolicyStatusId = statusIds.Rejected;
        }
        else
        {
            var approvalsAfterDecision = currentCycle.Count(x =>
                x.PolicyApprovalStageId == currentStage.Id && x.ActionType == 1) + 1;
            var stageCompleted = approvalsAfterDecision >= currentStage.MinimumApprovals;
            var laterStageExists = stages.Any(x => x.StageOrder > currentStage.StageOrder);
            if (stageCompleted && !laterStageExists)
            {
                version.PolicyStatusId = statusIds.Approved;
                version.ApprovedById = actorId;
                version.ApprovedDateTime = DateTime.UtcNow;
                version.ApprovedContentChecksumSha256 = contentChecksum;
            }
        }
        version.UpdatedById = actorId;
        version.UpdatedDateTime = DateTime.UtcNow;
        AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyApprovalHistory", null, action, null,
            JsonSerializer.Serialize(new { StageId = currentStage.Id, currentStage.StageOrder, Comments = comments }));
        await context.SaveChangesAsync(cancellationToken);
        return await GetDetailAsync(institutionId, version.PolicyId, version.Id, cancellationToken);
    }

    public async Task<PolicyAssignmentResultDTO> AssignAsync(long institutionId, long actorId, AssignPolicyRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        ValidateDates(dto.EffectiveFrom, dto.EffectiveTo);
        var version = await context.PolicyVersions.FirstOrDefaultAsync(x => x.Id == dto.PolicyVersionId
            && x.InstitutionId == institutionId && x.PolicyStatusId == statusIds.Published, cancellationToken)
            ?? throw new ConflictException("Only a published policy version can be assigned.");
        var staffMemberIds = dto.ResolvedStaffMemberIds.Distinct().ToList();
        var validStaffMemberIds = await context.StaffMembers
            .Where(x => x.InstitutionId == institutionId && staffMemberIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (validStaffMemberIds.Count != staffMemberIds.Count)
        {
            throw new ValidationErrorException("One or more staffMembers do not belong to the authenticated institution.");
        }
        var eligibleStaffMemberIds = await GetApplicabilityEligibleStaffMemberIdsAsync(
            context,
            institutionId,
            dto.PolicyVersionId,
            validStaffMemberIds,
            dto.EffectiveFrom,
            cancellationToken);
        if (eligibleStaffMemberIds.Count != validStaffMemberIds.Count)
        {
            throw new ValidationErrorException(
                "One or more staffMembers are outside the selected policy version applicability.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var matching = await context.PolicyAssignments.Where(x => x.InstitutionId == institutionId
            && x.PolicyVersionId == dto.PolicyVersionId && validStaffMemberIds.Contains(x.StaffMemberId)
            && x.EffectiveFrom == dto.EffectiveFrom).ToListAsync(cancellationToken);
        var activeStaffMemberIds = matching.Where(x => x.IsActive).Select(x => x.StaffMemberId).ToHashSet();
        var inactiveByStaffMember = matching.Where(x => !x.IsActive).GroupBy(x => x.StaffMemberId)
            .ToDictionary(x => x.Key, x => x.First());
        var insert = new List<PolicyAssignment>();
        foreach (var staffMemberId in validStaffMemberIds.Where(x => !activeStaffMemberIds.Contains(x)))
        {
            if (inactiveByStaffMember.TryGetValue(staffMemberId, out var inactive))
            {
                inactive.IsActive = true;
                inactive.EffectiveTo = dto.EffectiveTo;
                inactive.IsMandatory = dto.IsMandatory;
                inactive.AssignedById = actorId;
                inactive.AssignedDateTime = DateTime.UtcNow;
                inactive.RemovedById = null;
                inactive.RemovedDateTime = null;
            }
            else
            {
                insert.Add(new PolicyAssignment
                {
                    InstitutionId = institutionId,
                    PolicyVersionId = dto.PolicyVersionId,
                    StaffMemberId = staffMemberId,
                    AssignmentSource = 1,
                    EffectiveFrom = dto.EffectiveFrom,
                    EffectiveTo = dto.EffectiveTo,
                    IsMandatory = dto.IsMandatory,
                    IsActive = true,
                    AssignedById = actorId,
                    AssignedDateTime = DateTime.UtcNow
                });
            }
        }
        context.PolicyAssignments.AddRange(insert);
        var acknowledgementStaffMemberIds = await context.PolicyAcknowledgements
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == dto.PolicyVersionId
                && validStaffMemberIds.Contains(x.StaffMemberId))
            .Select(x => x.StaffMemberId)
            .ToListAsync(cancellationToken);
        context.PolicyAcknowledgements.AddRange(validStaffMemberIds.Except(acknowledgementStaffMemberIds)
            .Select(staffMemberId => new PolicyAcknowledgement
            {
                InstitutionId = institutionId,
                PolicyVersionId = dto.PolicyVersionId,
                StaffMemberId = staffMemberId,
                AcknowledgementStatus = 1,
                AssignedDateTime = DateTime.UtcNow
            }));
        AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyAssignment", null, "ASSIGN", null,
            JsonSerializer.Serialize(new { StaffMemberIds = validStaffMemberIds, dto.EffectiveFrom, dto.EffectiveTo }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PolicyAssignmentResultDTO(insert.Count + inactiveByStaffMember.Keys.Count(x => validStaffMemberIds.Contains(x)), activeStaffMemberIds.Count);
    }

    public async Task<bool> RemoveAssignmentAsync(long institutionId, long actorId, long assignmentId, CancellationToken cancellationToken)
    {
        var entity = await context.PolicyAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("Policy assignment was not found.");
        entity.IsActive = false; entity.RemovedById = actorId; entity.RemovedDateTime = DateTime.UtcNow; await context.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<long> CreateExceptionAsync(long institutionId, long actorId, CreatePolicyExceptionRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        ValidateDates(dto.EffectiveFrom, dto.EffectiveTo); ValidateJson(new[] { dto.OverrideConfiguration });
        if (!await context.PolicyVersions.AnyAsync(x => x.Id == dto.PolicyVersionId && x.InstitutionId == institutionId, cancellationToken) || !await context.StaffMembers.AnyAsync(x => x.Id == dto.ResolvedStaffMemberId && x.InstitutionId == institutionId, cancellationToken)) throw new ValidationErrorException("Policy version or staffMember is invalid for this institution.");
        var entity = new PolicyException { InstitutionId = institutionId, PolicyVersionId = dto.PolicyVersionId, StaffMemberId = dto.ResolvedStaffMemberId, ExceptionType = dto.ExceptionType, OverrideConfiguration = dto.OverrideConfiguration, Reason = dto.Reason.Trim(), EffectiveFrom = dto.EffectiveFrom, EffectiveTo = dto.EffectiveTo, ApprovalStatusId = statusIds.UnderReview, IsActive = true, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        context.PolicyExceptions.Add(entity); await context.SaveChangesAsync(cancellationToken); return entity.Id;
    }

    public async Task<bool> ApproveExceptionAsync(long institutionId, long actorId, ApprovePolicyExceptionRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var entity = await context.PolicyExceptions.FirstOrDefaultAsync(x => x.Id == dto.ExceptionId && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("Policy exception was not found.");
        entity.ApprovalStatusId = dto.Approve ? statusIds.Approved : statusIds.Rejected; entity.ApprovedById = actorId; entity.ApprovedDateTime = DateTime.UtcNow; await context.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<bool> AcknowledgeAsync(long institutionId, long staffMemberId, AcknowledgePolicyRequestDTO dto, CancellationToken cancellationToken)
    {
        if (!await context.PolicyAssignments.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId && x.PolicyVersionId == dto.PolicyVersionId && x.IsActive, cancellationToken)) throw new ForbiddenAccessException("The policy is not assigned to this staffMember.");
        if (dto.EvidenceJson != null) ValidateJson(new[] { dto.EvidenceJson });
        var entity = await context.PolicyAcknowledgements.FirstOrDefaultAsync(x => x.PolicyVersionId == dto.PolicyVersionId && x.StaffMemberId == staffMemberId, cancellationToken);
        if (entity == null)
        {
            entity = new PolicyAcknowledgement
            {
                InstitutionId = institutionId,
                PolicyVersionId = dto.PolicyVersionId,
                StaffMemberId = staffMemberId,
                AssignedDateTime = DateTime.UtcNow
            };
            context.PolicyAcknowledgements.Add(entity);
        }
        entity.AcknowledgementStatus = 3; entity.ViewedDateTime ??= DateTime.UtcNow; entity.AcknowledgedDateTime = DateTime.UtcNow; entity.EvidenceJson = dto.EvidenceJson;
        await context.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<IReadOnlyList<ResolvedPolicyResponseDTO>> ResolveAsync(long institutionId, long staffMemberId, ResolveStaffMemberPoliciesRequestDTO dto, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var staffMember = await context.StaffMembers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == staffMemberId && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("StaffMember was not found.");
        var date = dto.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var versions = await (from v in context.PolicyVersions.AsNoTracking() join p in context.Policies.AsNoTracking() on v.PolicyId equals p.Id where v.InstitutionId == institutionId && v.PolicyStatusId == statusIds.Published && v.IsCurrent && v.IsActive && p.IsActive && !p.IsSoftDeleted && v.EffectiveFrom <= date && (v.EffectiveTo == null || v.EffectiveTo >= date) select new { Version = v, Policy = p }).ToListAsync(cancellationToken);
        var ids = versions.Select(x => x.Version.Id).ToArray();
        if (dto.LeaveTypeId.HasValue && !await context.LeaveTypes.AsNoTracking().AnyAsync(
            x => x.Id == dto.LeaveTypeId && x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDeleted != true,
            cancellationToken))
        {
            throw new ValidationErrorException("LeaveTypeId is invalid for this institution.");
        }
        if (dto.LeaveTypeId.HasValue)
        {
            var coveredVersionIds = await context.PolicyVersionLeaveTypes.AsNoTracking()
                .Where(x => x.InstitutionId == institutionId && x.LeaveTypeId == dto.LeaveTypeId && x.IsActive)
                .Select(x => x.PolicyVersionId).ToListAsync(cancellationToken);
            versions.RemoveAll(x => !coveredVersionIds.Contains(x.Version.Id));
        }
        var resolvedRules = await (from rule in context.PolicyRules.AsNoTracking()
                                   join ruleType in context.PolicyRuleTypes.AsNoTracking() on rule.PolicyRuleTypeId equals ruleType.Id
                                   where ids.Contains(rule.PolicyVersionId) && rule.IsActive && ruleType.IsActive
                                   orderby rule.RuleOrder
                                   select new { RuleId = rule.Id, PolicyVersionId = rule.PolicyVersionId, ruleType.RuleTypeCode, rule.RuleName, rule.RuleOrder, rule.RuleConfiguration })
            .ToListAsync(cancellationToken);
        var resolvedRuleIds = resolvedRules.Select(x => x.RuleId).ToArray();
        var resolvedRuleLinks = await context.PolicyRuleLeaveTypes.AsNoTracking()
            .Where(x => resolvedRuleIds.Contains(x.PolicyRuleId)).ToListAsync(cancellationToken);
        var rulesByVersion = resolvedRules.GroupBy(x => x.PolicyVersionId).ToDictionary(x => x.Key,
            x => (IReadOnlyList<ResolvedPolicyRuleResponseDTO>)x
                .Where(y => !dto.LeaveTypeId.HasValue || resolvedRuleLinks.Any(link => link.PolicyRuleId == y.RuleId && link.LeaveTypeId == dto.LeaveTypeId))
                .Select(y => new ResolvedPolicyRuleResponseDTO(y.RuleTypeCode, y.RuleName, y.RuleOrder, y.RuleConfiguration,
                    resolvedRuleLinks.Where(link => link.PolicyRuleId == y.RuleId).Select(link => link.LeaveTypeId).ToList())).ToList());
        var scopes = await context.PolicyApplicabilities.AsNoTracking().Where(x => x.InstitutionId == institutionId && ids.Contains(x.PolicyVersionId) && x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).ToListAsync(cancellationToken);
        if (dto.LeaveTypeId.HasValue)
        {
            var scopeIds = scopes.Select(x => x.Id).ToArray();
            var matchingScopeIds = await context.PolicyApplicabilityLeaveTypes.AsNoTracking()
                .Where(x => scopeIds.Contains(x.PolicyApplicabilityId) && x.LeaveTypeId == dto.LeaveTypeId)
                .Select(x => x.PolicyApplicabilityId).ToListAsync(cancellationToken);
            scopes = scopes.Where(x => matchingScopeIds.Contains(x.Id)).ToList();
        }
        var manual = await context.PolicyAssignments.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMember.Id && ids.Contains(x.PolicyVersionId) && x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).Select(x => x.PolicyVersionId).ToListAsync(cancellationToken);
        var exceptions = await context.PolicyExceptions.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMember.Id
                && ids.Contains(x.PolicyVersionId) && x.IsActive
                && x.ApprovalStatusId == statusIds.Approved && x.EffectiveFrom <= date
                && x.EffectiveTo >= date)
            .OrderByDescending(x => x.ApprovedDateTime)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        var exceptionByVersion = exceptions
            .GroupBy(x => x.PolicyVersionId)
            .ToDictionary(x => x.Key, x => x.First());
        var locations = await (from assignment in context.StaffMemberLocationAssignments.AsNoTracking()
                               join location in context.Campuss.AsNoTracking() on assignment.CampusId equals location.Id
                               where assignment.InstitutionId == institutionId && assignment.StaffMemberId == staffMember.Id && assignment.IsActive && !assignment.IsSoftDeleted && assignment.EffectiveFrom <= date && (assignment.EffectiveTo == null || assignment.EffectiveTo >= date)
                               select location).ToListAsync(cancellationToken);
        var workMode = await context.StaffMemberWorkArrangements.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMember.Id && x.IsActive && !x.IsSoftDeleted && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).OrderByDescending(x => x.EffectiveFrom).Select(x => (short?)x.WorkMode).FirstOrDefaultAsync(cancellationToken);
        var positionId = await context.StaffPositionAssignments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMember.Id
                && x.IsPrimary && x.IsActive && x.EffectiveFrom <= date
                && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id)
            .Select(x => (int?)x.StaffPositionMasterId)
            .FirstOrDefaultAsync(cancellationToken);
        var result = new List<ResolvedPolicyResponseDTO>();
        foreach (var item in versions)
        {
            if (exceptionByVersion.TryGetValue(item.Version.Id, out var policyException))
            {
                var rules = rulesByVersion.GetValueOrDefault(item.Version.Id, Array.Empty<ResolvedPolicyRuleResponseDTO>());
                result.Add(new ResolvedPolicyResponseDTO(item.Policy.Id, item.Version.Id,
                    item.Policy.PolicyCode, item.Policy.PolicyName, int.MinValue, "EXCEPTION",
                    rules,
                    new ResolvedPolicyExceptionResponseDTO(
                        policyException.Id,
                        policyException.ExceptionType,
                        policyException.OverrideConfiguration,
                        policyException.Reason)));
                continue;
            }

            if (manual.Contains(item.Version.Id))
            {
                var rules = rulesByVersion.GetValueOrDefault(item.Version.Id, Array.Empty<ResolvedPolicyRuleResponseDTO>());
                result.Add(new ResolvedPolicyResponseDTO(item.Policy.Id, item.Version.Id,
                    item.Policy.PolicyCode, item.Policy.PolicyName, int.MinValue, "MANUAL_ASSIGNMENT",
                    rules));
                continue;
            }
            var matching = scopes
                .Where(x => x.PolicyVersionId == item.Version.Id && ScopeMatches(x, staffMember, locations, workMode, positionId, date))
                .ToList();
            if (matching.Count == 0)
            {
                continue;
            }
            var specificity = matching.Max(ScopeSpecificity);
            var mostSpecific = matching.Where(x => ScopeSpecificity(x) == specificity).ToList();
            var priority = mostSpecific.Min(x => x.Priority);
            var winners = mostSpecific.Where(x => x.Priority == priority).ToList();
            if (winners.Any(x => x.ApplicabilityMode == 2))
            {
                continue;
            }
            var include = winners.FirstOrDefault(x => x.ApplicabilityMode == 1);
            if (include != null)
            {
                var rules = rulesByVersion.GetValueOrDefault(item.Version.Id, Array.Empty<ResolvedPolicyRuleResponseDTO>());
                result.Add(new ResolvedPolicyResponseDTO(item.Policy.Id, item.Version.Id,
                    item.Policy.PolicyCode, item.Policy.PolicyName, include.Priority, "APPLICABILITY",
                    rules));
            }
        }
        return result.OrderBy(x => x.Priority).ThenBy(x => x.PolicyName).ToList();
    }

    public async Task<long> AddDocumentAsync(long institutionId, long actorId, UploadPolicyDocumentRequestDTO dto, string objectKey, string checksum, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var version = await context.PolicyVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PolicyVersionId && x.InstitutionId == institutionId, cancellationToken) ?? throw new NotFoundException("Policy version was not found.");
        if (version.PolicyStatusId != statusIds.Draft && version.PolicyStatusId != statusIds.Rejected)
            throw new ConflictException("Documents can be changed only on draft or rejected versions.");
        if (!await context.PolicyDocumentTypes.AnyAsync(x => x.Id == dto.PolicyDocumentTypeId && x.IsActive, cancellationToken)) throw new ValidationErrorException("PolicyDocumentTypeId is invalid.");
        var entity = new PolicyDocument { InstitutionId = institutionId, PolicyVersionId = dto.PolicyVersionId, PolicyDocumentTypeId = dto.PolicyDocumentTypeId, DocumentTitle = dto.DocumentTitle.Trim(), OriginalFileName = Path.GetFileName(dto.File.FileName), StorageProvider = "S3", ObjectKey = objectKey, ContentType = dto.File.ContentType, FileSizeBytes = dto.File.Length, ChecksumSha256 = checksum, LanguageCode = dto.LanguageCode?.Trim(), IsStaffMemberVisible = dto.IsStaffMemberVisible, IsActive = true, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        context.PolicyDocuments.Add(entity);
        AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyDocument", null, "UPLOAD", null, JsonSerializer.Serialize(new { entity.DocumentTitle, entity.OriginalFileName, entity.ChecksumSha256 }));
        await context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<IReadOnlyList<PolicyDocument>> GetDocumentsAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken)
    {
        return await context.PolicyDocuments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId
                && x.IsActive && !x.IsSoftDeleted)
            .OrderBy(x => x.DocumentTitle)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> DeleteDocumentAsync(long institutionId, long actorId, long documentId, CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var entity = await context.PolicyDocuments.FirstOrDefaultAsync(x => x.Id == documentId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken) ?? throw new NotFoundException("Policy document was not found.");
        var version = await context.PolicyVersions.FirstAsync(x => x.Id == entity.PolicyVersionId && x.InstitutionId == institutionId, cancellationToken);
        if (version.PolicyStatusId != statusIds.Draft && version.PolicyStatusId != statusIds.Rejected)
            throw new ConflictException("Documents can be changed only on draft or rejected versions.");
        entity.IsActive = false; entity.IsSoftDeleted = true; entity.SoftDeletedById = actorId; entity.SoftDeletedDateTime = DateTime.UtcNow;
        AddAudit(institutionId, actorId, version.PolicyId, version.Id, "PolicyDocument", entity.Id, "DELETE", JsonSerializer.Serialize(new { entity.DocumentTitle, entity.ObjectKey }), null);
        await context.SaveChangesAsync(cancellationToken); return entity.ObjectKey;
    }

    public async Task<IReadOnlyList<PolicyApprovalStageResponseDTO>> GetApprovalStagesAsync(
        long institutionId,
        PolicyApprovalStageListRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var query = context.PolicyApprovalStages.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive == dto.IsActive);
        if (dto.PolicyCategoryId.HasValue)
        {
            query = query.Where(x => x.PolicyCategoryId == null || x.PolicyCategoryId == dto.PolicyCategoryId);
        }
        return await query.OrderBy(x => x.StageOrder)
            .Select(x => MapApprovalStage(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyApprovalStageResponseDTO> CreateApprovalStageAsync(
        long institutionId,
        long actorId,
        CreatePolicyApprovalStageRequestDTO dto,
        CancellationToken cancellationToken)
    {
        await ValidateApprovalStageReferencesAsync(institutionId, dto.PolicyCategoryId, dto.ApproverRoleId, cancellationToken);
        if (await context.PolicyApprovalStages.AnyAsync(x => x.InstitutionId == institutionId
            && x.PolicyCategoryId == dto.PolicyCategoryId && x.StageOrder == dto.StageOrder, cancellationToken))
        {
            throw new ConflictException("An approval stage already exists at this order for the selected category.");
        }
        var entity = new PolicyApprovalStage
        {
            InstitutionId = institutionId,
            PolicyCategoryId = dto.PolicyCategoryId,
            StageName = dto.StageName.Trim(),
            StageOrder = dto.StageOrder,
            ApproverRoleId = dto.ApproverRoleId,
            MinimumApprovals = dto.MinimumApprovals,
            IsMandatory = dto.IsMandatory,
            IsActive = true,
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
        context.PolicyApprovalStages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return MapApprovalStage(entity);
    }

    public async Task<PolicyApprovalStageResponseDTO> UpdateApprovalStageAsync(
        long institutionId,
        long actorId,
        UpdatePolicyApprovalStageRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var entity = await context.PolicyApprovalStages.FirstOrDefaultAsync(
            x => x.Id == dto.Id && x.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Policy approval stage was not found.");
        await ValidateApprovalStageReferencesAsync(institutionId, dto.PolicyCategoryId, dto.ApproverRoleId, cancellationToken);
        if (await context.PolicyApprovalStages.AnyAsync(x => x.Id != dto.Id && x.InstitutionId == institutionId
            && x.PolicyCategoryId == dto.PolicyCategoryId && x.StageOrder == dto.StageOrder, cancellationToken))
        {
            throw new ConflictException("An approval stage already exists at this order for the selected category.");
        }
        entity.PolicyCategoryId = dto.PolicyCategoryId;
        entity.StageName = dto.StageName.Trim();
        entity.StageOrder = dto.StageOrder;
        entity.ApproverRoleId = dto.ApproverRoleId;
        entity.MinimumApprovals = dto.MinimumApprovals;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsActive = dto.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return MapApprovalStage(entity);
    }

    public async Task<bool> DeleteApprovalStageAsync(
        long institutionId,
        long actorId,
        long id,
        CancellationToken cancellationToken)
    {
        var entity = await context.PolicyApprovalStages.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Policy approval stage was not found.");
        entity.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PolicyApprovalProgressResponseDTO>> GetApprovalProgressAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        var version = await context.PolicyVersions.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == policyVersionId && x.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Policy version was not found.");
        var categoryId = await GetPolicyCategoryIdAsync(institutionId, version.PolicyId, cancellationToken);
        var stages = await context.PolicyApprovalStages.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive && x.IsMandatory
                && (x.PolicyCategoryId == null || x.PolicyCategoryId == categoryId))
            .OrderBy(x => x.StageOrder)
            .ToListAsync(cancellationToken);
        var history = await context.PolicyApprovalHistories.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId)
            .ToListAsync(cancellationToken);
        var lastRejectionSequence = history.Where(x => x.ActionType == 2)
            .Select(x => x.SequenceNumber).DefaultIfEmpty(0).Max();
        return stages.Select(stage =>
        {
            var count = history.Count(x => x.SequenceNumber > lastRejectionSequence
                && x.PolicyApprovalStageId == stage.Id && x.ActionType == 1);
            return new PolicyApprovalProgressResponseDTO(stage.Id, stage.StageName, stage.StageOrder,
                stage.MinimumApprovals, count, count >= stage.MinimumApprovals);
        }).ToList();
    }

    public async Task<IReadOnlyList<PolicyAssignmentResponseDTO>> GetAssignmentsAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        await EnsureVersionAsync(institutionId, policyVersionId, cancellationToken);
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var isArchived = await context.PolicyVersions.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.Id == policyVersionId)
            .Select(x => x.PolicyStatusId == statusIds.Archived)
            .SingleAsync(cancellationToken);
        return await context.PolicyAssignments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId)
            .OrderByDescending(x => x.AssignedDateTime)
            .Select(x => new PolicyAssignmentResponseDTO(x.Id, x.PolicyVersionId, x.StaffMemberId,
                x.AssignmentSource, x.EffectiveFrom, x.EffectiveTo, x.IsMandatory, x.IsActive,
                x.IsActive && !isArchived,
                isArchived && x.IsActive ? "VERSION_ARCHIVED" : x.IsActive ? null : "ASSIGNMENT_REMOVED"))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyAssignmentCandidateDataPageDTO> GetAssignmentCandidatesAsync(
        long institutionId,
        PolicyAssignmentCandidateRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var policyVersion = await (from version in context.PolicyVersions.AsNoTracking()
                                   join policy in context.Policies.AsNoTracking() on version.PolicyId equals policy.Id
                                   where version.Id == dto.PolicyVersionId && version.InstitutionId == institutionId
                                       && version.PolicyStatusId == statusIds.Published
                                       && version.IsActive && policy.IsActive && !policy.IsSoftDeleted
                                   select new
                                   {
                                       PolicyId = policy.Id,
                                       PolicyVersionId = version.Id,
                                       policy.PolicyCode,
                                       policy.PolicyName,
                                       version.VersionNumber,
                                       version.EffectiveFrom,
                                       version.EffectiveTo
                                   }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Only a published policy version can be mapped to staffMembers.");
        var effectiveDate = dto.EffectiveDate ?? policyVersion.EffectiveFrom;
        if (effectiveDate < policyVersion.EffectiveFrom
            || policyVersion.EffectiveTo.HasValue && effectiveDate > policyVersion.EffectiveTo.Value)
        {
            throw new ValidationErrorException("EffectiveDate must be inside the policy version effective period.");
        }

        var institutionStaffMemberIds = await context.StaffMembers.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var eligibleIds = await GetApplicabilityEligibleStaffMemberIdsAsync(context, institutionId,
            dto.PolicyVersionId, institutionStaffMemberIds, effectiveDate, cancellationToken);
        var assignedIds = await context.PolicyAssignments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == dto.PolicyVersionId
                && x.IsActive && x.EffectiveFrom <= effectiveDate
                && (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .Select(x => x.StaffMemberId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var assignedSet = assignedIds.ToHashSet();

        var eligibleStaffMemberRows = await (from staffMember in context.StaffMembers.AsNoTracking()
                                          where eligibleIds.Contains(staffMember.Id)
                                          join staffEmploymentType in context.StaffEmploymentTypes.AsNoTracking()
                                              on staffMember.StaffEmploymentTypeId equals (int?)staffEmploymentType.Id into staffEmploymentTypeRows
                                          from staffEmploymentType in staffEmploymentTypeRows.DefaultIfEmpty()
                                          join department in context.Departments.AsNoTracking()
                                              on staffMember.DepartmentId equals (int?)department.Id into departmentRows
                                          from department in departmentRows.DefaultIfEmpty()
                                          join position in StaffProfessionalStructureRepository.CurrentPositionIdentities(context, effectiveDate)
                                              on staffMember.Id equals position.StaffMemberId into positionRows
                                          from position in positionRows.DefaultIfEmpty()
                                          select new
                                          {
                                              staffMember.Id,
                                              staffMember.EmployementCode,
                                              staffMember.FirstName,
                                              staffMember.MiddleName,
                                              staffMember.LastName,
                                              staffMember.StaffEmploymentTypeId,
                                              StaffEmploymentTypeName = staffEmploymentType != null ? staffEmploymentType.TypeName : null,
                                              staffMember.DepartmentId,
                                              DepartmentName = department != null ? department.DepartmentName : null,
                                              StaffPositionMasterId = position != null ? (int?)position.StaffPositionMasterId : null,
                                              PositionName = position != null ? position.PositionName : null
                                          }).ToListAsync(cancellationToken);
        var eligibleStaffMembers = eligibleStaffMemberRows.Select(staffMember => new PolicyAssignmentCandidateDataDTO(
            staffMember.Id,
            staffMember.EmployementCode ?? string.Empty,
            BuildStaffMemberDisplayName(staffMember.FirstName, staffMember.MiddleName, staffMember.LastName) ?? "Unnamed staffMember",
            staffMember.StaffEmploymentTypeId,
            staffMember.StaffEmploymentTypeName,
            staffMember.DepartmentId,
            staffMember.DepartmentName,
            staffMember.StaffPositionMasterId,
            staffMember.PositionName,
            assignedSet.Contains(staffMember.Id))).ToList();

        var staffEmploymentTypes = eligibleStaffMembers.Where(x => x.StaffEmploymentTypeId.HasValue)
            .GroupBy(x => new { Id = x.StaffEmploymentTypeId!.Value, x.StaffEmploymentTypeName })
            .Select(x => new PolicyAssignmentFilterOptionDTO(x.Key.Id, x.Key.StaffEmploymentTypeName ?? string.Empty, x.Count()))
            .OrderBy(x => x.Name).ToList();
        var departments = eligibleStaffMembers.Where(x => x.DepartmentId.HasValue)
            .GroupBy(x => new { Id = x.DepartmentId!.Value, x.DepartmentName })
            .Select(x => new PolicyAssignmentFilterOptionDTO(x.Key.Id, x.Key.DepartmentName ?? string.Empty, x.Count()))
            .OrderBy(x => x.Name).ToList();
        var positions = eligibleStaffMembers
            .Where(x => x.StaffPositionMasterId.HasValue && (!dto.DepartmentId.HasValue || x.DepartmentId == dto.DepartmentId))
            .GroupBy(x => new { Id = x.StaffPositionMasterId!.Value, x.PositionName })
            .Select(x => new PolicyAssignmentFilterOptionDTO(x.Key.Id, x.Key.PositionName ?? string.Empty, x.Count()))
            .OrderBy(x => x.Name).ToList();

        IEnumerable<PolicyAssignmentCandidateDataDTO> filtered = eligibleStaffMembers;
        if (dto.StaffEmploymentTypeId.HasValue) filtered = filtered.Where(x => x.StaffEmploymentTypeId == dto.StaffEmploymentTypeId);
        if (dto.DepartmentId.HasValue) filtered = filtered.Where(x => x.DepartmentId == dto.DepartmentId);
        if (dto.StaffPositionMasterId.HasValue) filtered = filtered.Where(x => x.StaffPositionMasterId == dto.StaffPositionMasterId);
        if (!dto.IncludeAssigned) filtered = filtered.Where(x => !x.IsAssigned);
        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.Trim();
            filtered = filtered.Where(x => x.StaffMemberCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.StaffMemberName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        var ordered = filtered.OrderBy(x => x.DepartmentName).ThenBy(x => x.PositionName)
            .ThenBy(x => x.StaffMemberCode).ThenBy(x => x.StaffMemberName).ToList();
        var pageNumber = Math.Max(1, dto.PageNumber);
        var pageSize = Math.Clamp(dto.PageSize, 1, 200);
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PolicyAssignmentCandidateDataPageDTO(policyVersion.PolicyId,
            policyVersion.PolicyVersionId, policyVersion.PolicyCode, policyVersion.PolicyName,
            policyVersion.VersionNumber, effectiveDate, ordered.Count, page,
            staffEmploymentTypes, departments, positions);
    }

    public async Task<IReadOnlyList<PolicyAssignmentExportRowDTO>> GetAssignmentExportAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        await EnsureVersionAsync(institutionId, policyVersionId, cancellationToken);
        var statusIds = await GetPolicyStatusIdsAsync(cancellationToken);
        var isArchived = await context.PolicyVersions.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.Id == policyVersionId)
            .Select(x => x.PolicyStatusId == statusIds.Archived)
            .SingleAsync(cancellationToken);
        var rows = await (from assignment in context.PolicyAssignments.AsNoTracking()
                          where assignment.InstitutionId == institutionId && assignment.PolicyVersionId == policyVersionId
                          join staffMember in context.StaffMembers.AsNoTracking() on assignment.StaffMemberId equals staffMember.Id
                          join staffEmploymentType in context.StaffEmploymentTypes.AsNoTracking()
                              on staffMember.StaffEmploymentTypeId equals (int?)staffEmploymentType.Id into staffEmploymentTypes
                          from staffEmploymentType in staffEmploymentTypes.DefaultIfEmpty()
                          join department in context.Departments.AsNoTracking()
                              on staffMember.DepartmentId equals (int?)department.Id into departments
                          from department in departments.DefaultIfEmpty()
                          join position in StaffProfessionalStructureRepository.CurrentPositionIdentities(context, DateOnly.FromDateTime(DateTime.UtcNow))
                              on staffMember.Id equals position.StaffMemberId into positions
                          from position in positions.DefaultIfEmpty()
                          orderby department.DepartmentName, position.PositionName,
                              staffMember.EmployementCode, staffMember.FirstName
                          select new
                          {
                              assignment.Id,
                              StaffMemberId = staffMember.Id,
                              staffMember.EmployementCode,
                              staffMember.FirstName,
                              staffMember.MiddleName,
                              staffMember.LastName,
                              StaffEmploymentTypeName = staffEmploymentType != null ? staffEmploymentType.TypeName : null,
                              DepartmentName = department != null ? department.DepartmentName : null,
                              PositionName = position != null ? position.PositionName : null,
                              assignment.AssignmentSource,
                              assignment.EffectiveFrom,
                              assignment.EffectiveTo,
                              assignment.IsMandatory,
                              assignment.IsActive
                          }).ToListAsync(cancellationToken);
        return rows.Select(row => new PolicyAssignmentExportRowDTO(row.Id, row.StaffMemberId,
            row.EmployementCode ?? string.Empty,
            BuildStaffMemberDisplayName(row.FirstName, row.MiddleName, row.LastName) ?? "Unnamed staffMember",
            row.StaffEmploymentTypeName, row.DepartmentName, row.PositionName, row.AssignmentSource,
            row.EffectiveFrom, row.EffectiveTo, row.IsMandatory, row.IsActive,
            row.IsActive && !isArchived,
            isArchived && row.IsActive ? "VERSION_ARCHIVED" : row.IsActive ? null : "ASSIGNMENT_REMOVED"))
            .ToList();
    }

    public async Task<IReadOnlyList<PolicyExceptionResponseDTO>> GetExceptionsAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        await EnsureVersionAsync(institutionId, policyVersionId, cancellationToken);
        return await context.PolicyExceptions.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId)
            .OrderByDescending(x => x.AddedDateTime)
            .Select(x => new PolicyExceptionResponseDTO(x.Id, x.PolicyVersionId, x.StaffMemberId,
                x.ExceptionType, x.OverrideConfiguration, x.Reason, x.EffectiveFrom, x.EffectiveTo,
                x.ApprovalStatusId, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyAcknowledgementResponseDTO>> GetAcknowledgementsAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        await EnsureVersionAsync(institutionId, policyVersionId, cancellationToken);
        return await context.PolicyAcknowledgements.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId)
            .OrderBy(x => x.StaffMemberId)
            .Select(x => new PolicyAcknowledgementResponseDTO(x.Id, x.PolicyVersionId, x.StaffMemberId,
                x.AcknowledgementStatus, x.AssignedDateTime, x.ViewedDateTime, x.AcknowledgedDateTime))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyAuditResponseDTO>> GetAuditAsync(
        long institutionId,
        long policyId,
        CancellationToken cancellationToken)
    {
        if (!await context.Policies.AnyAsync(
                x => x.Id == policyId && x.InstitutionId == institutionId,
                cancellationToken))
        {
            throw new NotFoundException("Policy was not found.");
        }

        var rows = await (
            from audit in context.PolicyChangeAudits.AsNoTracking()
            where audit.InstitutionId == institutionId && audit.PolicyId == policyId
            join staffMember in context.StaffMembers.AsNoTracking().Where(x => x.InstitutionId == institutionId)
                on audit.ChangedById equals staffMember.Id into staffMembers
            from staffMember in staffMembers.DefaultIfEmpty()
            join version in context.PolicyVersions.AsNoTracking()
                    .Where(x => x.InstitutionId == institutionId && x.PolicyId == policyId)
                on audit.PolicyVersionId equals (long?)version.Id into versions
            from version in versions.DefaultIfEmpty()
            orderby audit.ChangedDateTime descending
            select new
            {
                Audit = audit,
                VersionNumber = version == null ? (int?)null : version.VersionNumber,
                StaffMemberFirstName = staffMember == null ? null : staffMember.FirstName,
                StaffMemberMiddleName = staffMember == null ? null : staffMember.MiddleName,
                StaffMemberLastName = staffMember == null ? null : staffMember.LastName
            }).ToListAsync(cancellationToken);

        return rows.Select(row => new PolicyAuditResponseDTO(
            row.Audit.Id,
            row.Audit.PolicyId,
            row.Audit.PolicyVersionId,
            row.VersionNumber,
            row.Audit.EntityName,
            row.Audit.EntityId,
            row.Audit.ActionName,
            row.Audit.BeforeData,
            row.Audit.AfterData,
            row.Audit.ChangedById,
            BuildStaffMemberDisplayName(
                row.StaffMemberFirstName,
                row.StaffMemberMiddleName,
                row.StaffMemberLastName),
            row.Audit.ChangedDateTime,
            row.Audit.CorrelationId)).ToList();
    }

    private static string? BuildStaffMemberDisplayName(
        string? firstName,
        string? middleName,
        string? lastName)
    {
        var name = string.Join(' ', new[] { firstName, middleName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private async Task<int?> GetPolicyCategoryIdAsync(long institutionId, long policyId, CancellationToken cancellationToken)
    {
        return await (from policy in context.Policies.AsNoTracking()
                      join policyType in context.PolicyTypes.AsNoTracking() on policy.PolicyTypeId equals policyType.Id
                      where policy.Id == policyId && policy.InstitutionId == institutionId && policyType.InstitutionId == institutionId
                      select policyType.PolicyCategoryId).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task ValidateApprovalStageReferencesAsync(
        long institutionId,
        int? categoryId,
        int? roleId,
        CancellationToken cancellationToken)
    {
        if (categoryId.HasValue && !await context.PolicyCategories.AsNoTracking()
            .AnyAsync(x => x.Id == categoryId && x.IsActive, cancellationToken))
        {
            throw new ValidationErrorException("PolicyCategoryId is invalid.");
        }
        if (roleId.HasValue && !await context.Roles.AsNoTracking()
            .AnyAsync(x => x.Id == roleId && x.InstitutionId == institutionId && x.IsActive && x.IsSoftDeleted != true,
                cancellationToken))
        {
            throw new ValidationErrorException("ApproverRoleId is invalid for this institution.");
        }
    }

    private async Task EnsureVersionAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken)
    {
        if (!await context.PolicyVersions.AsNoTracking()
            .AnyAsync(x => x.Id == policyVersionId && x.InstitutionId == institutionId, cancellationToken))
        {
            throw new NotFoundException("Policy version was not found.");
        }
    }

    private static PolicyApprovalStageResponseDTO MapApprovalStage(PolicyApprovalStage x)
    {
        return new PolicyApprovalStageResponseDTO(x.Id, x.PolicyCategoryId, x.StageName, x.StageOrder,
            x.ApproverRoleId, x.MinimumApprovals, x.IsMandatory, x.IsActive);
    }

    private async Task<PolicyDetailResponseDTO> GetDetailAsync(long institutionId, long policyId, long? versionId, CancellationToken cancellationToken)
    {
        var policy = await context.Policies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == policyId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken) ?? throw new NotFoundException("Policy was not found.");
        var versionQuery = context.PolicyVersions.AsNoTracking().Where(x => x.PolicyId == policyId && x.InstitutionId == institutionId);
        var version = versionId.HasValue ? await versionQuery.FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken) : await versionQuery.OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.VersionNumber).FirstOrDefaultAsync(cancellationToken);
        if (version == null) throw new NotFoundException("Policy version was not found.");
        var status = await context.PolicyStatuses.AsNoTracking().Where(x => x.Id == version.PolicyStatusId).Select(x => x.StatusName).FirstAsync(cancellationToken);
        var ruleEntities = await context.PolicyRules.AsNoTracking().Where(x => x.PolicyVersionId == version.Id && x.IsActive).OrderBy(x => x.RuleOrder).ToListAsync(cancellationToken);
        var ruleIds = ruleEntities.Select(x => x.Id).ToArray();
        var ruleLeaveTypes = await context.PolicyRuleLeaveTypes.AsNoTracking().Where(x => ruleIds.Contains(x.PolicyRuleId)).ToListAsync(cancellationToken);
        var rules = ruleEntities.Select(x => new PolicyRuleResponseDTO(x.Id, x.PolicyRuleTypeId, x.RuleName, x.RuleOrder, x.RuleConfiguration,
            ruleLeaveTypes.Where(link => link.PolicyRuleId == x.Id).Select(link => link.LeaveTypeId).ToList())).ToList();
        var scopeEntities = await context.PolicyApplicabilities.AsNoTracking()
            .Where(x => x.PolicyVersionId == version.Id && x.IsActive)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);
        var scopeIds = scopeEntities.Select(x => x.Id).ToArray();
        var scopeLeaveTypes = await context.PolicyApplicabilityLeaveTypes.AsNoTracking().Where(x => scopeIds.Contains(x.PolicyApplicabilityId)).ToListAsync(cancellationToken);
        var scopes = scopeEntities.Select(x => new PolicyApplicabilityResponseDTO(x.Id, x.ApplicabilityMode, x.Priority,
                x.CountryId, x.StateId, x.DistrictId, x.LocalityId, x.CampusId,
                x.StaffEmploymentTypeId, x.DepartmentId, x.StaffMemberId, x.GenderId,
                x.WorkArrangementType, x.EmploymentStatus, x.MinimumServiceDays,
                x.EffectiveFrom, x.EffectiveTo,
                scopeLeaveTypes.Where(link => link.PolicyApplicabilityId == x.Id).Select(link => link.LeaveTypeId).ToList())
            { StaffPositionMasterId = x.StaffPositionMasterId }).ToList();
        var leaveTypes = await (from link in context.PolicyVersionLeaveTypes.AsNoTracking()
                                join leaveType in context.LeaveTypes.AsNoTracking() on link.LeaveTypeId equals leaveType.Id
                                where link.PolicyVersionId == version.Id && link.InstitutionId == institutionId && link.IsActive
                                orderby leaveType.LeaveName
                                select new PolicyLeaveTypeResponseDTO(leaveType.Id, leaveType.LeaveName)).ToListAsync(cancellationToken);
        var attendanceConfiguration = await context.AttendancePolicyVersionConfigurations.AsNoTracking()
            .Where(x => x.PolicyVersionId == version.Id && x.InstitutionId == institutionId)
            .Select(x => new AttendancePolicyVersionConfigurationDTO
            {
                AttendanceLocationScope = (AttendanceLocationScope)x.AttendanceLocationScope,
                AllowBiometric = x.AllowBiometric,
                AllowMobile = x.AllowMobile,
                AllowWeb = x.AllowWeb,
                AllowManualAttendance = x.AllowManualAttendance,
                AllowWorkFromHome = x.AllowWorkFromHome,
                RequireGeoFenceForOffice = x.RequireGeoFenceForOffice,
                RequireGpsForRemote = x.RequireGpsForRemote,
                AllowOutsideLocationWithApproval = x.AllowOutsideLocationWithApproval
            })
            .FirstOrDefaultAsync(cancellationToken);
        return new PolicyDetailResponseDTO(policy.Id, policy.PolicyCode, policy.PolicyName,
            policy.Summary, policy.PolicyTypeId, policy.OwnerDepartmentId,
            policy.DefaultCurrencyCode, version.Id, version.VersionNumber,
            version.PolicyStatusId, status, version.EffectiveFrom, version.EffectiveTo,
            version.ChangeSummary, rules, scopes, attendanceConfiguration, leaveTypes);
    }

    private async Task ValidateAttendanceConfigurationAsync(
        long institutionId,
        int policyTypeId,
        AttendancePolicyVersionConfigurationDTO? configuration,
        CancellationToken cancellationToken)
    {
        var categoryCode = await (from policyType in context.PolicyTypes.AsNoTracking()
                                  join category in context.PolicyCategories.AsNoTracking()
                                      on policyType.PolicyCategoryId equals category.Id
                                  where policyType.Id == policyTypeId && policyType.InstitutionId == institutionId
                                  select category.CategoryCode)
            .FirstOrDefaultAsync(cancellationToken);
        var isAttendance = string.Equals(categoryCode, AppConstants.PolicyCategoryCodes.Attendance, StringComparison.OrdinalIgnoreCase);
        if (isAttendance && configuration == null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
        }
        if (!isAttendance && configuration != null)
        {
            throw new ValidationErrorException("AttendanceConfiguration is allowed only for an Attendance policy type.");
        }
        if (configuration == null)
        {
            return;
        }
        if (!Enum.IsDefined(configuration.AttendanceLocationScope))
        {
            throw new ValidationErrorException("AttendanceLocationScope is invalid.");
        }
        if (!configuration.AllowBiometric && !configuration.AllowMobile
            && !configuration.AllowWeb && !configuration.AllowManualAttendance)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyChannelRequired);
        }
    }

    private async Task UpsertAttendanceConfigurationAsync(long institutionId, long actorId, long policyVersionId,
        AttendancePolicyVersionConfigurationDTO? dto, DateTime now, CancellationToken cancellationToken)
    {
        var existing = context.AttendancePolicyVersionConfigurations.Local
            .FirstOrDefault(x => x.PolicyVersionId == policyVersionId && x.InstitutionId == institutionId)
            ?? await context.AttendancePolicyVersionConfigurations
                .FirstOrDefaultAsync(x => x.PolicyVersionId == policyVersionId && x.InstitutionId == institutionId, cancellationToken);
        if (dto == null)
        {
            if (existing != null)
            {
                context.AttendancePolicyVersionConfigurations.Remove(existing);
            }
            return;
        }
        if (existing == null)
        {
            context.AttendancePolicyVersionConfigurations.Add(
                MapAttendanceConfiguration(institutionId, actorId, policyVersionId, dto, now));
            return;
        }
        ApplyAttendanceConfiguration(existing, dto);
        existing.UpdatedById = actorId;
        existing.UpdatedDateTime = now;
    }

    private static AttendancePolicyVersionConfiguration MapAttendanceConfiguration(long institutionId, long actorId,
        long policyVersionId, AttendancePolicyVersionConfigurationDTO dto, DateTime now)
    {
        var entity = new AttendancePolicyVersionConfiguration
        {
            InstitutionId = institutionId,
            PolicyVersionId = policyVersionId,
            AddedById = actorId,
            AddedDateTime = now
        };
        ApplyAttendanceConfiguration(entity, dto);
        return entity;
    }

    private static void ApplyAttendanceConfiguration(AttendancePolicyVersionConfiguration entity,
        AttendancePolicyVersionConfigurationDTO dto)
    {
        entity.AttendanceLocationScope = (short)dto.AttendanceLocationScope;
        entity.AllowBiometric = dto.AllowBiometric;
        entity.AllowMobile = dto.AllowMobile;
        entity.AllowWeb = dto.AllowWeb;
        entity.AllowManualAttendance = dto.AllowManualAttendance;
        entity.AllowWorkFromHome = dto.AllowWorkFromHome;
        entity.RequireGeoFenceForOffice = dto.RequireGeoFenceForOffice;
        entity.RequireGpsForRemote = dto.RequireGpsForRemote;
        entity.AllowOutsideLocationWithApproval = dto.AllowOutsideLocationWithApproval;
    }

    private static AttendancePolicyVersionConfigurationDTO ToDto(AttendancePolicyVersionConfiguration entity) => new()
    {
        AttendanceLocationScope = (AttendanceLocationScope)entity.AttendanceLocationScope,
        AllowBiometric = entity.AllowBiometric,
        AllowMobile = entity.AllowMobile,
        AllowWeb = entity.AllowWeb,
        AllowManualAttendance = entity.AllowManualAttendance,
        AllowWorkFromHome = entity.AllowWorkFromHome,
        RequireGeoFenceForOffice = entity.RequireGeoFenceForOffice,
        RequireGpsForRemote = entity.RequireGpsForRemote,
        AllowOutsideLocationWithApproval = entity.AllowOutsideLocationWithApproval
    };

    private (IReadOnlyList<(PolicyRule Entity, PolicyRuleInputDTO Input)> Rules,
        IReadOnlyList<(PolicyApplicability Entity, PolicyApplicabilityInputDTO Input)> Scopes)
        AddRulesAndApplicability(long institutionId, long actorId, long versionId,
            IEnumerable<PolicyRuleInputDTO> rules, IEnumerable<PolicyApplicabilityInputDTO> scopes, DateTime now)
    {
        var addedRules = rules.Select(input => (Entity: new PolicyRule { InstitutionId = institutionId, PolicyVersionId = versionId, PolicyRuleTypeId = input.PolicyRuleTypeId, RuleName = input.RuleName.Trim(), RuleOrder = input.RuleOrder, RuleConfiguration = input.RuleConfiguration, IsActive = true, AddedById = actorId, AddedDateTime = now }, Input: input)).ToList();
        var addedScopes = scopes.Select(input => (Entity: new PolicyApplicability { InstitutionId = institutionId, PolicyVersionId = versionId, ApplicabilityMode = input.ApplicabilityMode, CountryId = input.CountryId, StateId = input.StateId, DistrictId = input.DistrictId, LocalityId = input.LocalityId, CampusId = input.CampusId, StaffEmploymentTypeId = input.StaffEmploymentTypeId, DepartmentId = input.DepartmentId, StaffPositionMasterId = input.StaffPositionMasterId, StaffMemberId = input.ResolvedStaffMemberId ?? input.StaffMemberId, GenderId = input.GenderId, WorkArrangementType = input.WorkArrangementType, EmploymentStatus = input.EmploymentStatus, MinimumServiceDays = input.MinimumServiceDays, Priority = input.Priority, EffectiveFrom = input.EffectiveFrom, EffectiveTo = input.EffectiveTo, IsActive = true, AddedById = actorId, AddedDateTime = now }, Input: input)).ToList();
        context.PolicyRules.AddRange(addedRules.Select(x => x.Entity));
        context.PolicyApplicabilities.AddRange(addedScopes.Select(x => x.Entity));
        return (addedRules, addedScopes);
    }

    private void AddLeaveTargetMappings(long institutionId, long actorId, long versionId,
        IReadOnlyCollection<int> leaveTypeIds,
        (IReadOnlyList<(PolicyRule Entity, PolicyRuleInputDTO Input)> Rules,
            IReadOnlyList<(PolicyApplicability Entity, PolicyApplicabilityInputDTO Input)> Scopes) added)
    {
        var now = DateTime.UtcNow;
        context.PolicyVersionLeaveTypes.AddRange(leaveTypeIds.Distinct().Select(leaveTypeId =>
            new PolicyVersionLeaveType { InstitutionId = institutionId, PolicyVersionId = versionId, LeaveTypeId = leaveTypeId, IsActive = true, AddedById = actorId, AddedDateTime = now }));
        context.PolicyRuleLeaveTypes.AddRange(added.Rules.SelectMany(item => item.Input.LeaveTypeIds.Distinct().Select(leaveTypeId =>
            new PolicyRuleLeaveType { InstitutionId = institutionId, PolicyRuleId = item.Entity.Id, LeaveTypeId = leaveTypeId })));
        context.PolicyApplicabilityLeaveTypes.AddRange(added.Scopes.SelectMany(item => item.Input.LeaveTypeIds.Distinct().Select(leaveTypeId =>
            new PolicyApplicabilityLeaveType { InstitutionId = institutionId, PolicyApplicabilityId = item.Entity.Id, LeaveTypeId = leaveTypeId })));
    }

    private void AddAudit(long institutionId, long actorId, long policyId, long? versionId,
        string entityName, long? entityId, string action, string? before, string? after)
    {
        context.PolicyChangeAudits.Add(new PolicyChangeAudit
        {
            InstitutionId = institutionId,
            PolicyId = policyId,
            PolicyVersionId = versionId,
            EntityName = entityName,
            EntityId = entityId,
            ActionName = action,
            BeforeData = before,
            AfterData = after,
            ChangedById = actorId,
            ChangedDateTime = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid()
        });
    }
    private static PolicyTypeResponseDTO MapType(PolicyType x, string categoryCode) =>
        new(x.Id, x.PolicyTypeCode ?? string.Empty, x.PolicyName, x.Description,
            x.PolicyCategoryId, categoryCode, x.DefaultCurrencyCode, x.IsActive == true);
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant().Replace(' ', '_');
    private static string? NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static void ValidateDates(DateOnly from, DateOnly? to)
    {
        if (from == default || to.HasValue && to < from)
        {
            throw new ValidationErrorException("Effective date range is invalid.");
        }
    }

    private static void ValidateJson(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException();
                }
            }
            catch (JsonException)
            {
                throw new ValidationErrorException("Rule and override configuration must be a JSON object.");
            }
        }
    }
    private static void ValidateRulesAndScopes(IReadOnlyCollection<PolicyRuleInputDTO> rules, IReadOnlyCollection<PolicyApplicabilityInputDTO> scopes)
    {
        ValidateJson(rules.Select(x => x.RuleConfiguration));
        if (rules.GroupBy(x => x.RuleOrder).Any(group => group.Count() > 1)) throw new ValidationErrorException("RuleOrder must be unique within a policy version.");
        foreach (var scope in scopes) ValidateDates(scope.EffectiveFrom, scope.EffectiveTo);
    }

    private async Task EnsureRequiredPolicyDocumentAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        var hasStaffMemberVisiblePolicyDocument = await (
            from document in context.PolicyDocuments.AsNoTracking()
            join documentType in context.PolicyDocumentTypes.AsNoTracking()
                on document.PolicyDocumentTypeId equals documentType.Id
            where document.InstitutionId == institutionId
                && document.PolicyVersionId == policyVersionId
                && document.IsActive
                && !document.IsSoftDeleted
                && document.IsStaffMemberVisible
                && documentType.IsActive
                && documentType.DocumentTypeCode == "POLICY_DOCUMENT"
            select document.Id).AnyAsync(cancellationToken);

        if (!hasStaffMemberVisiblePolicyDocument)
        {
            throw new ConflictException(
                "An active staffMember-visible Policy Document is required before review or publication.");
        }
    }

    private async Task<string> ComputeVersionContentChecksumAsync(
        long institutionId,
        long policyVersionId,
        CancellationToken cancellationToken)
    {
        var version = await context.PolicyVersions.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.Id == policyVersionId)
            .Select(x => new
            {
                x.PolicyId,
                x.VersionNumber,
                x.EffectiveFrom,
                x.EffectiveTo,
                x.ChangeSummary,
                x.RuleSchemaVersion,
                x.Policy.PolicyCode,
                x.Policy.PolicyName,
                x.Policy.Summary,
                x.Policy.PolicyTypeId,
                x.Policy.OwnerDepartmentId,
                x.Policy.DefaultCurrencyCode
            })
            .SingleAsync(cancellationToken);
        var rules = await context.PolicyRules.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId && x.IsActive)
            .OrderBy(x => x.RuleOrder)
            .ThenBy(x => x.Id)
            .Select(x => new { x.PolicyRuleTypeId, x.RuleName, x.RuleOrder, x.RuleConfiguration })
            .ToListAsync(cancellationToken);
        var applicability = await context.PolicyApplicabilities.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId && x.IsActive)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.ApplicabilityMode,
                x.CountryId,
                x.StateId,
                x.DistrictId,
                x.LocalityId,
                x.CampusId,
                x.StaffEmploymentTypeId,
                x.DepartmentId,
                x.StaffPositionMasterId,
                x.StaffMemberId,
                x.GenderId,
                x.WorkArrangementType,
                x.EmploymentStatus,
                x.MinimumServiceDays,
                x.Priority,
                x.EffectiveFrom,
                x.EffectiveTo
            })
            .ToListAsync(cancellationToken);
        var attendance = await context.AttendancePolicyVersionConfigurations.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId)
            .Select(x => new
            {
                x.AttendanceLocationScope,
                x.AllowBiometric,
                x.AllowMobile,
                x.AllowWeb,
                x.AllowManualAttendance,
                x.AllowWorkFromHome,
                x.RequireGeoFenceForOffice,
                x.RequireGpsForRemote,
                x.AllowOutsideLocationWithApproval
            })
            .SingleOrDefaultAsync(cancellationToken);
        var versionLeaveTypes = await context.PolicyVersionLeaveTypes.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId && x.IsActive)
            .OrderBy(x => x.LeaveTypeId)
            .Select(x => x.LeaveTypeId)
            .ToListAsync(cancellationToken);
        var checksumRuleIds = await context.PolicyRules.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var ruleLeaveTypes = await context.PolicyRuleLeaveTypes.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && checksumRuleIds.Contains(x.PolicyRuleId))
            .OrderBy(x => x.PolicyRuleId).ThenBy(x => x.LeaveTypeId)
            .Select(x => new { x.PolicyRuleId, x.LeaveTypeId })
            .ToListAsync(cancellationToken);
        var checksumScopeIds = await context.PolicyApplicabilities.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var applicabilityLeaveTypes = await context.PolicyApplicabilityLeaveTypes.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && checksumScopeIds.Contains(x.PolicyApplicabilityId))
            .OrderBy(x => x.PolicyApplicabilityId).ThenBy(x => x.LeaveTypeId)
            .Select(x => new { x.PolicyApplicabilityId, x.LeaveTypeId })
            .ToListAsync(cancellationToken);
        var documents = await context.PolicyDocuments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId
                && x.IsActive && !x.IsSoftDeleted)
            .OrderBy(x => x.PolicyDocumentTypeId)
            .ThenBy(x => x.DocumentTitle)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.PolicyDocumentTypeId,
                x.DocumentTitle,
                x.OriginalFileName,
                x.ContentType,
                x.FileSizeBytes,
                x.ChecksumSha256,
                x.LanguageCode,
                x.IsStaffMemberVisible
            })
            .ToListAsync(cancellationToken);
        object checksumApplicability = applicability.Any(x => x.StaffPositionMasterId.HasValue)
            ? applicability
            : applicability.Select(x => new
            {
                x.ApplicabilityMode,
                x.CountryId,
                x.StateId,
                x.DistrictId,
                x.LocalityId,
                x.CampusId,
                x.StaffEmploymentTypeId,
                x.DepartmentId,
                // Preserve the original approval fingerprint envelope; no legacy DB field is read.
                DesignationId = (int?)null,
                x.StaffMemberId,
                x.GenderId,
                x.WorkArrangementType,
                x.EmploymentStatus,
                x.MinimumServiceDays,
                x.Priority,
                x.EffectiveFrom,
                x.EffectiveTo
            }).ToList();
        var canonicalJson = JsonSerializer.Serialize(new
        {
            Version = version,
            Rules = rules,
            Applicability = checksumApplicability,
            Attendance = attendance,
            VersionLeaveTypes = versionLeaveTypes,
            RuleLeaveTypes = ruleLeaveTypes,
            ApplicabilityLeaveTypes = applicabilityLeaveTypes,
            Documents = documents
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson)));
    }

    private async Task<string> GetCategoryCodeAsync(int id, CancellationToken token)
    {
        var categoryCode = await context.PolicyCategories.AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => x.CategoryCode)
            .FirstOrDefaultAsync(token);
        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            throw new ValidationErrorException("PolicyCategoryId is invalid.");
        }
        return categoryCode;
    }

    private async Task<PolicyStatusIds> GetPolicyStatusIdsAsync(CancellationToken cancellationToken)
    {
        var requiredCodes = new[]
        {
            AppConstants.PolicyStatusCodes.Draft,
            AppConstants.PolicyStatusCodes.UnderReview,
            AppConstants.PolicyStatusCodes.Approved,
            AppConstants.PolicyStatusCodes.Published,
            AppConstants.PolicyStatusCodes.Archived,
            AppConstants.PolicyStatusCodes.Rejected
        };
        var idsByCode = await context.PolicyStatuses.AsNoTracking()
            .Where(status => status.IsActive && requiredCodes.Contains(status.StatusCode))
            .ToDictionaryAsync(status => status.StatusCode, status => status.Id, cancellationToken);
        if (idsByCode.Count != requiredCodes.Length)
        {
            throw new InvalidOperationException("Policy lifecycle status metadata is incomplete.");
        }

        return new PolicyStatusIds(
            idsByCode[AppConstants.PolicyStatusCodes.Draft],
            idsByCode[AppConstants.PolicyStatusCodes.UnderReview],
            idsByCode[AppConstants.PolicyStatusCodes.Approved],
            idsByCode[AppConstants.PolicyStatusCodes.Published],
            idsByCode[AppConstants.PolicyStatusCodes.Archived],
            idsByCode[AppConstants.PolicyStatusCodes.Rejected]);
    }
    private async Task ValidatePolicyReferencesAsync(
        long institutionId,
        int? ownerDepartmentId,
        IReadOnlyCollection<PolicyRuleInputDTO> rules,
        CancellationToken cancellationToken)
    {
        if (ownerDepartmentId.HasValue && !await context.Departments.AsNoTracking().AnyAsync(
            x => x.Id == ownerDepartmentId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted,
            cancellationToken))
        {
            throw new ValidationErrorException("OwnerDepartmentId is invalid for this institution.");
        }
        var ruleTypeIds = rules.Select(x => x.PolicyRuleTypeId).Distinct().ToList();
        var validRuleTypeCount = await context.PolicyRuleTypes.AsNoTracking()
            .CountAsync(x => ruleTypeIds.Contains(x.Id) && x.IsActive, cancellationToken);
        if (validRuleTypeCount != ruleTypeIds.Count)
        {
            throw new ValidationErrorException("One or more PolicyRuleTypeId values are invalid.");
        }
    }

    private async Task ValidatePolicyRulesAsync(
        long institutionId,
        int policyTypeId,
        IReadOnlyCollection<PolicyRuleInputDTO> rules,
        CancellationToken cancellationToken)
    {
        var categoryId = await context.PolicyTypes.AsNoTracking()
            .Where(x => x.Id == policyTypeId && x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDelete != true)
            .Select(x => x.PolicyCategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!categoryId.HasValue)
        {
            throw new ValidationErrorException("PolicyTypeId is not active for this institution.");
        }

        var allowedMappings = await context.PolicyCategoryRuleTypes.AsNoTracking()
            .Where(x => x.PolicyCategoryId == categoryId.Value && x.IsActive)
            .ToListAsync(cancellationToken);
        var allowedIds = allowedMappings.Select(x => x.PolicyRuleTypeId).ToHashSet();
        if (rules.Any(rule => !allowedIds.Contains(rule.PolicyRuleTypeId)))
        {
            throw new ValidationErrorException("One or more rules are not allowed for this policy category.");
        }
        if (allowedMappings.Where(x => x.IsRequired).Any(x => rules.All(rule => rule.PolicyRuleTypeId != x.PolicyRuleTypeId)))
        {
            throw new ValidationErrorException("One or more required rules are missing for this policy category.");
        }
        if (allowedMappings.Where(x => !x.AllowMultiple).Any(x => rules.Count(rule => rule.PolicyRuleTypeId == x.PolicyRuleTypeId) > 1))
        {
            throw new ValidationErrorException("A rule type configured as single-use was added more than once.");
        }

        var definitions = await context.PolicyRuleSettingDefinitions.AsNoTracking()
            .Where(x => allowedIds.Contains(x.PolicyRuleTypeId) && x.IsActive)
            .ToListAsync(cancellationToken);
        var definitionIds = definitions.Select(x => x.Id).ToArray();
        var options = await context.PolicyRuleSettingOptions.AsNoTracking()
            .Where(x => definitionIds.Contains(x.PolicyRuleSettingDefinitionId) && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            using var document = JsonDocument.Parse(rule.RuleConfiguration);
            var root = document.RootElement;
            var ruleDefinitions = definitions.Where(x => x.PolicyRuleTypeId == rule.PolicyRuleTypeId).ToList();
            var allowedCodes = ruleDefinitions.Select(x => x.SettingCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unsupported = root.EnumerateObject().FirstOrDefault(property => !allowedCodes.Contains(property.Name));
            if (!string.IsNullOrEmpty(unsupported.Name))
            {
                throw new ValidationErrorException($"Rule setting '{unsupported.Name}' is not supported.");
            }
            foreach (var definition in ruleDefinitions)
            {
                if (!root.TryGetProperty(definition.SettingCode, out var value))
                {
                    if (definition.IsRequired)
                    {
                        throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' is required.");
                    }
                    continue;
                }
                ValidateSettingValue(definition, value, options);
            }

            ValidateAccrualCalculation(ruleDefinitions, root);
        }
    }

    private static void ValidateAccrualCalculation(
        IReadOnlyCollection<PolicyRuleSettingDefinition> definitions,
        JsonElement configuration)
    {
        var settingCodes = definitions
            .Select(x => x.SettingCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!settingCodes.Contains("annualEntitlement")
            || !settingCodes.Contains("frequency")
            || !settingCodes.Contains("amountPerCycle"))
        {
            return;
        }

        if (!configuration.TryGetProperty("annualEntitlement", out var annualElement)
            || !annualElement.TryGetDecimal(out var annualEntitlement)
            || !configuration.TryGetProperty("frequency", out var frequencyElement)
            || frequencyElement.ValueKind != JsonValueKind.String
            || !configuration.TryGetProperty("amountPerCycle", out var amountElement)
            || !amountElement.TryGetDecimal(out var suppliedAmount))
        {
            return;
        }

        decimal expectedAmount;
        try
        {
            expectedAmount = PolicyAccrualCalculationHelper.CalculateAmountPerCycle(
                annualEntitlement,
                frequencyElement.GetString() ?? string.Empty);
        }
        catch (ArgumentException exception)
        {
            throw new ValidationErrorException(exception.Message);
        }

        if (suppliedAmount != expectedAmount)
        {
            throw new ValidationErrorException(
                $"Rule setting 'amountPerCycle' must equal {expectedAmount} for the selected annual entitlement and accrual frequency.");
        }
    }

    private async Task ValidateLeaveTargetsAsync(
        long institutionId,
        int policyTypeId,
        IReadOnlyCollection<int> leaveTypeIds,
        IReadOnlyCollection<PolicyRuleInputDTO> rules,
        IReadOnlyCollection<PolicyApplicabilityInputDTO> applicability,
        CancellationToken cancellationToken)
    {
        var categoryCode = await (from policyType in context.PolicyTypes.AsNoTracking()
                                  join category in context.PolicyCategories.AsNoTracking()
                                      on policyType.PolicyCategoryId equals category.Id
                                  where policyType.Id == policyTypeId && policyType.InstitutionId == institutionId
                                      && policyType.IsActive == true && policyType.IsSoftDelete != true
                                  select category.CategoryCode)
            .FirstOrDefaultAsync(cancellationToken);
        var isLeavePolicy = string.Equals(categoryCode, "LEAVE", StringComparison.OrdinalIgnoreCase);
        var distinctIds = leaveTypeIds.Where(id => id > 0).Distinct().ToArray();

        if (!isLeavePolicy)
        {
            if (distinctIds.Length > 0 || rules.Any(x => x.LeaveTypeIds.Count > 0)
                || applicability.Any(x => x.LeaveTypeIds.Count > 0))
            {
                throw new ValidationErrorException("Leave type targets are allowed only for a Leave policy.");
            }
            return;
        }

        if (distinctIds.Length == 0)
        {
            throw new ValidationErrorException("Select at least one covered Leave Type for a Leave policy.");
        }
        var validCount = await context.LeaveTypes.AsNoTracking().CountAsync(
            x => distinctIds.Contains(x.Id) && x.InstitutionId == institutionId && x.IsActive == true
                && x.IsSoftDeleted != true,
            cancellationToken);
        if (validCount != distinctIds.Length)
        {
            throw new ValidationErrorException("One or more LeaveTypeId values are invalid for this institution.");
        }
        var selected = distinctIds.ToHashSet();
        if (rules.Any(rule => rule.LeaveTypeIds.Count == 0
            || rule.LeaveTypeIds.Any(id => !selected.Contains(id))))
        {
            throw new ValidationErrorException("Every Leave rule must target one or more covered Leave Types.");
        }
        if (applicability.Any(scope => scope.LeaveTypeIds.Count == 0
            || scope.LeaveTypeIds.Any(id => !selected.Contains(id))))
        {
            throw new ValidationErrorException("Every Leave applicability row must target one or more covered Leave Types.");
        }
    }

    private static void ValidateSettingValue(
        PolicyRuleSettingDefinition definition,
        JsonElement value,
        IReadOnlyCollection<PolicyRuleSettingOption> options)
    {
        if (definition.AllowMultiple)
        {
            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' must be an array.");
            }
            if (definition.IsRequired && value.GetArrayLength() == 0)
            {
                throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' requires at least one value.");
            }
            foreach (var item in value.EnumerateArray())
            {
                ValidateSingleSettingValue(definition, item, options);
            }
            return;
        }

        ValidateSingleSettingValue(definition, value, options);
    }

    private static void ValidateSingleSettingValue(
        PolicyRuleSettingDefinition definition,
        JsonElement value,
        IReadOnlyCollection<PolicyRuleSettingOption> options)
    {
        var validType = definition.DataTypeCode switch
        {
            "BOOLEAN" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "INTEGER" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "DECIMAL" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _),
            "STRING" or "CODE" => value.ValueKind == JsonValueKind.String,
            _ => false
        };
        if (!validType)
        {
            throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' must be {definition.DataTypeCode}.");
        }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            && (definition.MinimumValue.HasValue && number < definition.MinimumValue
                || definition.MaximumValue.HasValue && number > definition.MaximumValue))
        {
            throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' is outside its allowed range.");
        }
        var allowedOptions = options.Where(x => x.PolicyRuleSettingDefinitionId == definition.Id).ToList();
        if (allowedOptions.Count > 0 && allowedOptions.All(option => option.ValueJson != value.GetRawText()))
        {
            throw new ValidationErrorException($"Rule setting '{definition.SettingCode}' has an unsupported option.");
        }
    }

    private async Task ValidateScopeReferencesAsync(
        long institutionId,
        IReadOnlyCollection<PolicyApplicabilityInputDTO> scopes,
        CancellationToken cancellationToken)
    {
        foreach (var scope in scopes)
        {

            State? state = null;
            District? district = null;
            Locality? locality = null;
            Campus? location = null;
            if (scope.CountryId.HasValue && !await context.Countries.AsNoTracking()
                .AnyAsync(x => x.Id == scope.CountryId && x.IsActive == true, cancellationToken))
            {
                throw new ValidationErrorException("CountryId is invalid.");
            }
            if (scope.StateId.HasValue)
            {
                state = await context.States.AsNoTracking().FirstOrDefaultAsync(
                    x => x.Id == scope.StateId && x.IsActive == true, cancellationToken)
                    ?? throw new ValidationErrorException("StateId is invalid.");
                if (scope.CountryId.HasValue && state.CountryId != scope.CountryId)
                {
                    throw new ValidationErrorException("StateId does not belong to CountryId.");
                }
            }
            if (scope.DistrictId.HasValue)
            {
                district = await context.Districts.AsNoTracking().FirstOrDefaultAsync(
                    x => x.Id == scope.DistrictId && x.IsActive, cancellationToken)
                    ?? throw new ValidationErrorException("DistrictId is invalid.");
                if (scope.StateId.HasValue && district.StateId != scope.StateId)
                {
                    throw new ValidationErrorException("DistrictId does not belong to StateId.");
                }
            }
            if (scope.LocalityId.HasValue)
            {
                locality = await context.Localities.AsNoTracking().FirstOrDefaultAsync(
                    x => x.Id == scope.LocalityId && x.IsActive == true, cancellationToken)
                    ?? throw new ValidationErrorException("LocalityId is invalid.");
                if (scope.DistrictId.HasValue && locality.DistrictId != scope.DistrictId)
                {
                    throw new ValidationErrorException("LocalityId does not belong to DistrictId.");
                }
                if (scope.StateId.HasValue && locality.StateId != scope.StateId)
                {
                    throw new ValidationErrorException("LocalityId does not belong to StateId.");
                }
            }
            if (scope.CampusId.HasValue)
            {
                location = await context.Campuss.AsNoTracking().FirstOrDefaultAsync(
                    x => x.Id == scope.CampusId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted,
                    cancellationToken) ?? throw new ValidationErrorException("CampusId is invalid for this institution.");
                if ((scope.CountryId.HasValue && location.CountryId != scope.CountryId)
                    || (scope.StateId.HasValue && location.StateId != scope.StateId)
                    || (scope.DistrictId.HasValue && location.DistrictId != scope.DistrictId)
                    || (scope.LocalityId.HasValue && location.LocalityId != scope.LocalityId))
                {
                    throw new ValidationErrorException("CampusId does not match the supplied geography.");
                }
            }
            if (scope.DepartmentId.HasValue && !await context.Departments.AsNoTracking().AnyAsync(
                x => x.Id == scope.DepartmentId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken))
            {
                throw new ValidationErrorException("DepartmentId is invalid for this institution.");
            }

            if (scope.StaffPositionMasterId.HasValue && !await context.StaffPositionMasters.AsNoTracking()
                .AnyAsync(position => position.Id == scope.StaffPositionMasterId &&
                    position.InstitutionId == institutionId && position.IsActive && !position.IsSoftDeleted &&
                    (!scope.DepartmentId.HasValue || !position.DepartmentId.HasValue || position.DepartmentId == scope.DepartmentId),
                    cancellationToken))
            {
                throw new ValidationErrorException("StaffPositionMasterId is invalid for this institution or department.");
            }
            if (scope.StaffEmploymentTypeId.HasValue && !await context.StaffEmploymentTypes.AsNoTracking().AnyAsync(
                x => x.Id == scope.StaffEmploymentTypeId && x.InstitutionId == institutionId && x.IsSoftDeleted != true,
                cancellationToken))
            {
                throw new ValidationErrorException("StaffEmploymentTypeId is invalid for this institution.");
            }
            var staffMemberId = scope.ResolvedStaffMemberId ?? scope.StaffMemberId;
            if (staffMemberId.HasValue && !await context.StaffMembers.AsNoTracking().AnyAsync(
                x => x.Id == staffMemberId && x.InstitutionId == institutionId, cancellationToken))
            {
                throw new ValidationErrorException("StaffMemberId is invalid for this institution.");
            }
            if (scope.GenderId.HasValue && !await context.Genders.AsNoTracking().AnyAsync(
                x => x.Id == scope.GenderId, cancellationToken))
            {
                throw new ValidationErrorException("GenderId is invalid.");
            }
        }
    }

    private static int ScopeSpecificity(PolicyApplicability scope)
    {
        if (scope.StaffMemberId.HasValue) return 700;
        if (scope.CampusId.HasValue || scope.LocalityId.HasValue) return 600;
        if (scope.DistrictId.HasValue) return 500;
        if (scope.StateId.HasValue) return 400;
        if (scope.CountryId.HasValue) return 300;
        if (scope.StaffEmploymentTypeId.HasValue) return 200;
        if (scope.DepartmentId.HasValue) return 100;
        return 0;
    }

    internal static async Task<HashSet<long>> GetApplicabilityEligibleStaffMemberIdsAsync(
        SkolenzaDbContext database,
        long institutionId,
        long policyVersionId,
        IReadOnlyCollection<long> staffMemberIds,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        if (staffMemberIds.Count == 0)
        {
            return new HashSet<long>();
        }
        var scopes = await database.PolicyApplicabilities.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId
                && x.IsActive && x.EffectiveFrom <= effectiveDate
                && (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);
        if (scopes.Count == 0)
        {
            return new HashSet<long>();
        }
        var staffMembers = await database.StaffMembers.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && staffMemberIds.Contains(x.Id)
                && x.IsActive && !x.IsSoftDeleted)
            .ToListAsync(cancellationToken);
        var locations = await (from assignment in database.StaffMemberLocationAssignments.AsNoTracking()
                               join location in database.Campuss.AsNoTracking()
                                   on assignment.CampusId equals location.Id
                               where assignment.InstitutionId == institutionId && staffMemberIds.Contains(assignment.StaffMemberId)
                                   && assignment.IsActive && !assignment.IsSoftDeleted
                                   && assignment.EffectiveFrom <= effectiveDate
                                   && (assignment.EffectiveTo == null || assignment.EffectiveTo >= effectiveDate)
                               select new { assignment.StaffMemberId, Location = location })
            .ToListAsync(cancellationToken);
        var locationsByStaffMember = locations.GroupBy(x => x.StaffMemberId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Campus>)x.Select(y => y.Location).ToList());
        var workModes = await database.StaffMemberWorkArrangements.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && staffMemberIds.Contains(x.StaffMemberId)
                && x.IsActive && !x.IsSoftDeleted && x.EffectiveFrom <= effectiveDate
                && (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.StaffMemberId, x.WorkMode })
            .ToListAsync(cancellationToken);
        var workModeByStaffMember = workModes.GroupBy(x => x.StaffMemberId)
            .ToDictionary(x => x.Key, x => (short?)x.First().WorkMode);
        var positions = await database.StaffPositionAssignments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && staffMemberIds.Contains(x.StaffMemberId)
                && x.IsPrimary && x.IsActive && x.EffectiveFrom <= effectiveDate
                && (!x.EffectiveTo.HasValue || x.EffectiveTo >= effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.StaffMemberId, x.StaffPositionMasterId })
            .ToListAsync(cancellationToken);
        var positionByStaffMember = positions.GroupBy(x => x.StaffMemberId)
            .ToDictionary(x => x.Key, x => (int?)x.First().StaffPositionMasterId);
        var eligible = new HashSet<long>();
        foreach (var staffMember in staffMembers)
        {
            var staffMemberLocations = locationsByStaffMember.GetValueOrDefault(staffMember.Id,
                Array.Empty<Campus>());
            var workMode = workModeByStaffMember.GetValueOrDefault(staffMember.Id);
            var positionId = positionByStaffMember.GetValueOrDefault(staffMember.Id);
            var matching = scopes.Where(x => ScopeMatches(x, staffMember, staffMemberLocations,
                workMode, positionId, effectiveDate)).ToList();
            if (matching.Count == 0)
            {
                continue;
            }
            var specificity = matching.Max(ScopeSpecificity);
            var mostSpecific = matching.Where(x => ScopeSpecificity(x) == specificity).ToList();
            var priority = mostSpecific.Min(x => x.Priority);
            var winners = mostSpecific.Where(x => x.Priority == priority).ToList();
            if (!winners.Any(x => x.ApplicabilityMode == 2)
                && winners.Any(x => x.ApplicabilityMode == 1))
            {
                eligible.Add(staffMember.Id);
            }
        }
        return eligible;
    }

    private static bool ScopeMatches(PolicyApplicability x, StaffMember e, IReadOnlyCollection<Campus> locations, short? workMode, int? positionId, DateOnly date)
    {
        var serviceDays = e.DateOfOnBoarding.HasValue
            ? Math.Max(0, date.DayNumber - DateOnly.FromDateTime(e.DateOfOnBoarding.Value).DayNumber)
            : 0;
        var geographyMatches = (!x.CampusId.HasValue || locations.Any(location => location.Id == x.CampusId))
            && (!x.CountryId.HasValue || e.CountryId == x.CountryId || locations.Any(location => location.CountryId == x.CountryId))
            && (!x.StateId.HasValue || locations.Any(location => location.StateId == x.StateId))
            && (!x.DistrictId.HasValue || locations.Any(location => location.DistrictId == x.DistrictId))
            && (!x.LocalityId.HasValue || locations.Any(location => location.LocalityId == x.LocalityId));
        return geographyMatches
            && (!x.StaffMemberId.HasValue || x.StaffMemberId == e.Id)
            && (!x.StaffEmploymentTypeId.HasValue || x.StaffEmploymentTypeId == e.StaffEmploymentTypeId)
            && (!x.DepartmentId.HasValue || x.DepartmentId == e.DepartmentId)
            && (!x.StaffPositionMasterId.HasValue || x.StaffPositionMasterId == positionId)
            && (!x.GenderId.HasValue || x.GenderId == e.GenderId)
            && (!x.WorkArrangementType.HasValue || x.WorkArrangementType == workMode)
            && (!x.EmploymentStatus.HasValue || (x.EmploymentStatus == 1) == e.IsActive)
            && (!x.MinimumServiceDays.HasValue || serviceDays >= x.MinimumServiceDays);
    }
    private static PolicyApplicability CloneScope(PolicyApplicability x, long versionId, long actorId) => new() { InstitutionId = x.InstitutionId, PolicyVersionId = versionId, ApplicabilityMode = x.ApplicabilityMode, CountryId = x.CountryId, StateId = x.StateId, DistrictId = x.DistrictId, LocalityId = x.LocalityId, CampusId = x.CampusId, StaffEmploymentTypeId = x.StaffEmploymentTypeId, DepartmentId = x.DepartmentId, StaffPositionMasterId = x.StaffPositionMasterId, StaffMemberId = x.StaffMemberId, GenderId = x.GenderId, WorkArrangementType = x.WorkArrangementType, EmploymentStatus = x.EmploymentStatus, MinimumServiceDays = x.MinimumServiceDays, Priority = x.Priority, EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, IsActive = x.IsActive, AddedById = actorId, AddedDateTime = DateTime.UtcNow };

    private sealed record PolicyStatusIds(
        short Draft,
        short UnderReview,
        short Approved,
        short Published,
        short Archived,
        short Rejected);
}
