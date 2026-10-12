using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Staff;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Persists institution-scoped professional masters and effective-dated StaffMember assignments.</summary>
public sealed class StaffProfessionalStructureRepository(SkolenzaDbContext context)
    : IStaffProfessionalStructureRepository
{
    public Task SeedInstitutionMastersAsync(
        long institutionId,
        long? actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT skolenza.\"SeedInstitutionStaffProfessionalMasters\"({institutionId}, {actorStaffMemberId})",
            cancellationToken);
    }

    public Task<List<StaffProfessionalMasterResponseDTO>> GetMastersAsync(
        StaffProfessionalMasterType type,
        long institutionId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        return type switch
        {
            StaffProfessionalMasterType.JobFamily => QueryMasters(context.StaffJobFamilyMasters, institutionId, activeOnly, cancellationToken),
            StaffProfessionalMasterType.Capability => QueryCapabilities(institutionId, activeOnly, cancellationToken),
            StaffProfessionalMasterType.DutyDefinition => QueryDuties(institutionId, activeOnly, cancellationToken),
            _ => throw new ValidationErrorException("Unsupported professional master type.")
        };
    }

    public async Task<StaffProfessionalMasterResponseDTO> SaveMasterAsync(
        StaffProfessionalMasterType type,
        StaffProfessionalMasterRequestDTO request,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        ValidateMasterRequest(request);
        return type switch
        {
            StaffProfessionalMasterType.JobFamily => await SaveMasterEntityAsync(
                context.StaffJobFamilyMasters, request, institutionId, actorStaffMemberId, cancellationToken),
            StaffProfessionalMasterType.Capability => await SaveCapabilityAsync(
                request, institutionId, actorStaffMemberId, cancellationToken),
            StaffProfessionalMasterType.DutyDefinition => await SaveDutyAsync(
                request, institutionId, actorStaffMemberId, cancellationToken),
            _ => throw new ValidationErrorException("Unsupported professional master type.")
        };
    }

    public async Task DeleteMasterAsync(
        StaffProfessionalMasterType type,
        int id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        InstitutionMasterEntity entity = type switch
        {
            StaffProfessionalMasterType.JobFamily => await FindMasterAsync(context.StaffJobFamilyMasters, id, institutionId, cancellationToken),
            StaffProfessionalMasterType.Capability => await FindMasterAsync(context.StaffCapabilityMasters, id, institutionId, cancellationToken),
            StaffProfessionalMasterType.DutyDefinition => await FindMasterAsync(context.StaffDutyDefinitionMasters, id, institutionId, cancellationToken),
            _ => throw new ValidationErrorException("Unsupported professional master type.")
        };

        bool hasDependency = type switch
        {
            StaffProfessionalMasterType.JobFamily => await context.StaffPositionMasters.AnyAsync(
                item => item.InstitutionId == institutionId && item.StaffJobFamilyMasterId == id && !item.IsSoftDeleted,
                cancellationToken),
            StaffProfessionalMasterType.Capability => await context.StaffPositionCapabilities.AnyAsync(
                item => item.InstitutionId == institutionId && item.StaffCapabilityMasterId == id,
                cancellationToken),
            StaffProfessionalMasterType.DutyDefinition => await context.StaffPositionDuties.AnyAsync(
                item => item.InstitutionId == institutionId && item.StaffDutyDefinitionMasterId == id,
                cancellationToken),
            _ => false
        };
        if (hasDependency)
        {
            throw new ConflictException("This master is in use. Remove its position dependencies before deleting it.");
        }

        entity.IsSoftDeleted = true;
        entity.IsActive = false;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<StaffPositionMasterResponseDTO>> GetPositionsAsync(
        long institutionId,
        bool activeOnly,
        short? staffCategoryId,
        CancellationToken cancellationToken = default)
    {
        var query = context.StaffPositionMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }
        if (staffCategoryId.HasValue)
        {
            query = query.Where(item => item.StaffCategoryId == staffCategoryId.Value);
        }

        var positions = await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        return await ProjectPositionsAsync(positions, institutionId, cancellationToken);
    }

    public async Task<StaffPositionMasterResponseDTO> SavePositionAsync(
        StaffPositionMasterRequestDTO request,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) ||
            request.StaffJobFamilyMasterId <= 0 || request.StaffCategoryId <= 0)
        {
            throw new ValidationErrorException("Code, name, job family and staff category are required.");
        }

        bool referencesValid = await context.StaffJobFamilyMasters.AnyAsync(
                item => item.Id == request.StaffJobFamilyMasterId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) &&
            await context.StaffCategories.AnyAsync(
                item => item.Id == request.StaffCategoryId && item.IsActive,
                cancellationToken);
        if (!referencesValid)
        {
            throw new ValidationErrorException("The selected job family or staff category is unavailable.");
        }
        if (request.DepartmentId.HasValue && !await context.Departments.AnyAsync(
                item => item.Id == request.DepartmentId && item.InstitutionId == institutionId && item.IsActive && item.IsSoftDeleted != true,
                cancellationToken))
        {
            throw new ValidationErrorException("The selected department is unavailable.");
        }
        if (request.DesignationId.HasValue && !await context.Designations.AnyAsync(
                item => item.Id == request.DesignationId && item.InstitutionId == institutionId && item.IsActive && item.IsSoftDeleted != true,
                cancellationToken))
        {
            throw new ValidationErrorException("The selected designation is unavailable.");
        }

        var capabilityIds = request.CapabilityIds.Distinct().ToList();
        var dutyIds = request.DutyDefinitionIds.Distinct().ToList();
        if (capabilityIds.Count != await context.StaffCapabilityMasters.CountAsync(
                item => capabilityIds.Contains(item.Id) && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) ||
            dutyIds.Count != await context.StaffDutyDefinitionMasters.CountAsync(
                item => dutyIds.Contains(item.Id) && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken))
        {
            throw new ValidationErrorException("One or more selected capabilities or duties are unavailable.");
        }

        StaffPositionMaster entity;
        if (request.Id.HasValue)
        {
            entity = await context.StaffPositionMasters.FirstOrDefaultAsync(
                    item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Position was not found.");
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new StaffPositionMaster
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };
            await context.StaffPositionMasters.AddAsync(entity, cancellationToken);
        }

        string code = NormalizeCode(request.Code);
        if (await context.StaffPositionMasters.AnyAsync(
                item => item.InstitutionId == institutionId && item.Id != entity.Id && !item.IsSoftDeleted && item.Code.ToLower() == code.ToLower(),
                cancellationToken))
        {
            throw new ConflictException("A position with this code already exists.");
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.StaffJobFamilyMasterId = request.StaffJobFamilyMasterId;
        entity.StaffCategoryId = request.StaffCategoryId;
        entity.DepartmentId = request.DepartmentId;
        entity.DesignationId = request.DesignationId;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);

        var oldCapabilities = await context.StaffPositionCapabilities.Where(item => item.StaffPositionMasterId == entity.Id).ToListAsync(cancellationToken);
        var oldDuties = await context.StaffPositionDuties.Where(item => item.StaffPositionMasterId == entity.Id).ToListAsync(cancellationToken);
        context.StaffPositionCapabilities.RemoveRange(oldCapabilities);
        context.StaffPositionDuties.RemoveRange(oldDuties);
        await context.StaffPositionCapabilities.AddRangeAsync(capabilityIds.Select((id, index) => new StaffPositionCapability
        {
            InstitutionId = institutionId,
            StaffPositionMasterId = entity.Id,
            StaffCapabilityMasterId = id,
            IsMandatory = true,
            DisplayOrder = checked((short)((index + 1) * 10)),
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        }), cancellationToken);
        await context.StaffPositionDuties.AddRangeAsync(dutyIds.Select((id, index) => new StaffPositionDuty
        {
            InstitutionId = institutionId,
            StaffPositionMasterId = entity.Id,
            StaffDutyDefinitionMasterId = id,
            IsMandatory = true,
            DisplayOrder = checked((short)((index + 1) * 10)),
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        }), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return (await ProjectPositionsAsync([entity], institutionId, cancellationToken)).Single();
    }

    public async Task DeletePositionAsync(
        int id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        var position = await context.StaffPositionMasters.FirstOrDefaultAsync(
                item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken)
            ?? throw new KeyNotFoundException("Position was not found.");
        if (await context.StaffPositionAssignments.AnyAsync(item => item.StaffPositionMasterId == id, cancellationToken))
        {
            throw new ConflictException("This position has StaffMember assignments and cannot be deleted. Deactivate it instead.");
        }
        position.IsActive = false;
        position.IsSoftDeleted = true;
        position.SoftDeletedById = actorStaffMemberId;
        position.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignPrimaryPositionAsync(
        long institutionId,
        long staffMemberId,
        int positionId,
        DateOnly effectiveFrom,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        if (await context.StaffPositionAssignments.AnyAsync(
                item => item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId && item.IsPrimary && item.IsActive,
                cancellationToken))
        {
            throw new ConflictException("The StaffMember already has an active primary position.");
        }
        await CreatePositionAssignmentGraphAsync(institutionId, staffMemberId, positionId, effectiveFrom, actorStaffMemberId, cancellationToken);
    }

    public async Task ReplacePrimaryPositionAsync(
        long institutionId,
        long staffMemberId,
        int positionId,
        DateOnly effectiveFrom,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        var current = await context.StaffPositionAssignments
            .Where(item => item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId && item.IsPrimary && item.IsActive)
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (current?.StaffPositionMasterId == positionId)
        {
            return;
        }
        if (current is not null)
        {
            current.IsActive = false;
            current.EffectiveTo = effectiveFrom > current.EffectiveFrom ? effectiveFrom.AddDays(-1) : current.EffectiveFrom;
            current.UpdatedById = actorStaffMemberId;
            current.UpdatedDateTime = DateTime.UtcNow;
            var capabilityAssignments = await context.StaffCapabilityAssignments.Where(item => item.SourcePositionAssignmentId == current.Id && item.EffectiveTo == null).ToListAsync(cancellationToken);
            var dutyAssignments = await context.StaffDutyAssignments.Where(item => item.SourcePositionAssignmentId == current.Id && item.IsActive).ToListAsync(cancellationToken);
            foreach (var capability in capabilityAssignments)
            {
                capability.EffectiveTo = current.EffectiveTo;
                capability.UpdatedById = actorStaffMemberId;
                capability.UpdatedDateTime = DateTime.UtcNow;
            }
            foreach (var duty in dutyAssignments)
            {
                duty.EffectiveTo = current.EffectiveTo;
                duty.IsActive = false;
                duty.UpdatedById = actorStaffMemberId;
                duty.UpdatedDateTime = DateTime.UtcNow;
            }
            await context.SaveChangesAsync(cancellationToken);
        }
        await CreatePositionAssignmentGraphAsync(institutionId, staffMemberId, positionId, effectiveFrom, actorStaffMemberId, cancellationToken);
    }

    public async Task<StaffProfessionalProfileResponseDTO?> GetStaffProfileAsync(
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await context.StaffPositionAssignments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId && item.IsPrimary && item.IsActive)
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return null;
        }
        var position = await context.StaffPositionMasters.AsNoTracking().SingleAsync(item => item.Id == assignment.StaffPositionMasterId, cancellationToken);
        var family = await context.StaffJobFamilyMasters.AsNoTracking().SingleAsync(item => item.Id == position.StaffJobFamilyMasterId, cancellationToken);
        var capabilities = await (
            from assigned in context.StaffCapabilityAssignments.AsNoTracking()
            join master in context.StaffCapabilityMasters.AsNoTracking() on assigned.StaffCapabilityMasterId equals master.Id
            where assigned.SourcePositionAssignmentId == assignment.Id
            orderby master.DisplayOrder, master.Name
            select new StaffCapabilityAssignmentResponseDTO
            {
                Id = assigned.Id,
                StaffCapabilityMasterId = master.Id,
                Name = master.Name,
                VerificationStatus = assigned.VerificationStatus,
                VerificationStatusName = assigned.VerificationStatus == 1 ? "Pending Verification" : assigned.VerificationStatus == 2 ? "Verified" : "Rejected",
                IsRequired = assigned.IsRequired,
                VerifiedDateTime = assigned.VerifiedDateTime,
                VerificationRemark = assigned.VerificationRemark
            }).ToListAsync(cancellationToken);
        var duties = await (
            from assigned in context.StaffDutyAssignments.AsNoTracking()
            join master in context.StaffDutyDefinitionMasters.AsNoTracking() on assigned.StaffDutyDefinitionMasterId equals master.Id
            where assigned.SourcePositionAssignmentId == assignment.Id
            orderby master.DisplayOrder, master.Name
            select new StaffDutyAssignmentResponseDTO
            {
                Id = assigned.Id,
                StaffDutyDefinitionMasterId = master.Id,
                Name = master.Name,
                Frequency = master.DefaultFrequency,
                IsActive = assigned.IsActive
            }).ToListAsync(cancellationToken);
        return new StaffProfessionalProfileResponseDTO
        {
            PositionAssignmentId = assignment.Id,
            StaffPositionMasterId = position.Id,
            PositionName = position.Name,
            StaffJobFamilyMasterId = family.Id,
            JobFamilyName = family.Name,
            EffectiveFrom = assignment.EffectiveFrom,
            EffectiveTo = assignment.EffectiveTo,
            IsPrimary = assignment.IsPrimary,
            Capabilities = capabilities,
            Duties = duties
        };
    }

    public async Task VerifyCapabilityAsync(
        long institutionId,
        long assignmentId,
        bool approved,
        string? remark,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await context.StaffCapabilityAssignments.FirstOrDefaultAsync(
                item => item.Id == assignmentId && item.InstitutionId == institutionId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Capability assignment was not found.");
        assignment.VerificationStatus = (short)(approved
            ? StaffCapabilityVerificationStatus.Verified
            : StaffCapabilityVerificationStatus.Rejected);
        assignment.VerifiedById = actorStaffMemberId;
        assignment.VerifiedDateTime = DateTime.UtcNow;
        assignment.VerificationRemark = NormalizeOptional(remark);
        assignment.UpdatedById = actorStaffMemberId;
        assignment.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task CreatePositionAssignmentGraphAsync(
        long institutionId,
        long staffMemberId,
        int positionId,
        DateOnly effectiveFrom,
        long actorStaffMemberId,
        CancellationToken cancellationToken)
    {
        var position = await context.StaffPositionMasters.AsNoTracking().FirstOrDefaultAsync(
                item => item.Id == positionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken)
            ?? throw new ValidationErrorException("The selected position is unavailable.");
        var staffMember = await context.StaffMembers.AsNoTracking().FirstOrDefaultAsync(
                item => item.Id == staffMemberId && item.InstitutionId == institutionId && item.IsSoftDeleted != true,
                cancellationToken)
            ?? throw new ValidationErrorException("The selected StaffMember is unavailable.");
        if (staffMember.StaffCategoryId != position.StaffCategoryId)
        {
            throw new ValidationErrorException("The selected position is not available for the StaffMember category.");
        }

        var assignment = new StaffPositionAssignment
        {
            InstitutionId = institutionId,
            StaffMemberId = staffMemberId,
            StaffPositionMasterId = position.Id,
            EffectiveFrom = effectiveFrom,
            IsPrimary = true,
            IsActive = true,
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        };
        await context.StaffPositionAssignments.AddAsync(assignment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var capabilityMappings = await context.StaffPositionCapabilities.AsNoTracking()
            .Where(item => item.StaffPositionMasterId == position.Id)
            .ToListAsync(cancellationToken);
        var dutyMappings = await context.StaffPositionDuties.AsNoTracking()
            .Where(item => item.StaffPositionMasterId == position.Id)
            .ToListAsync(cancellationToken);
        await context.StaffCapabilityAssignments.AddRangeAsync(capabilityMappings.Select(mapping => new StaffCapabilityAssignment
        {
            InstitutionId = institutionId,
            StaffMemberId = staffMemberId,
            StaffCapabilityMasterId = mapping.StaffCapabilityMasterId,
            SourcePositionAssignmentId = assignment.Id,
            VerificationStatus = (short)StaffCapabilityVerificationStatus.PendingVerification,
            IsRequired = mapping.IsMandatory,
            EffectiveFrom = effectiveFrom,
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        }), cancellationToken);
        await context.StaffDutyAssignments.AddRangeAsync(dutyMappings.Select(mapping => new StaffDutyAssignment
        {
            InstitutionId = institutionId,
            StaffMemberId = staffMemberId,
            StaffDutyDefinitionMasterId = mapping.StaffDutyDefinitionMasterId,
            SourcePositionAssignmentId = assignment.Id,
            EffectiveFrom = effectiveFrom,
            IsActive = true,
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        }), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<StaffPositionMasterResponseDTO>> ProjectPositionsAsync(
        List<StaffPositionMaster> positions,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var ids = positions.Select(item => item.Id).ToList();
        var familyIds = positions.Select(item => item.StaffJobFamilyMasterId).Distinct().ToList();
        var categoryIds = positions.Select(item => item.StaffCategoryId).Distinct().ToList();
        var families = await context.StaffJobFamilyMasters.AsNoTracking().Where(item => familyIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        var categories = await context.StaffCategories.AsNoTracking().Where(item => categoryIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        var capabilityRows = await (
            from mapping in context.StaffPositionCapabilities.AsNoTracking()
            join master in context.StaffCapabilityMasters.AsNoTracking() on mapping.StaffCapabilityMasterId equals master.Id
            where ids.Contains(mapping.StaffPositionMasterId) && mapping.InstitutionId == institutionId
            select new { mapping.StaffPositionMasterId, Master = master }).ToListAsync(cancellationToken);
        var dutyRows = await (
            from mapping in context.StaffPositionDuties.AsNoTracking()
            join master in context.StaffDutyDefinitionMasters.AsNoTracking() on mapping.StaffDutyDefinitionMasterId equals master.Id
            where ids.Contains(mapping.StaffPositionMasterId) && mapping.InstitutionId == institutionId
            select new { mapping.StaffPositionMasterId, Master = master }).ToListAsync(cancellationToken);
        return positions.Select(position => new StaffPositionMasterResponseDTO
        {
            Id = position.Id,
            StaffJobFamilyMasterId = position.StaffJobFamilyMasterId,
            JobFamilyName = families[position.StaffJobFamilyMasterId].Name,
            StaffCategoryId = position.StaffCategoryId,
            StaffCategoryName = categories[position.StaffCategoryId].Name,
            DepartmentId = position.DepartmentId,
            DesignationId = position.DesignationId,
            Code = position.Code,
            Name = position.Name,
            Description = position.Description,
            DisplayOrder = position.DisplayOrder,
            IsActive = position.IsActive,
            Capabilities = capabilityRows.Where(item => item.StaffPositionMasterId == position.Id).Select(item => MapMaster(item.Master)).ToList(),
            Duties = dutyRows.Where(item => item.StaffPositionMasterId == position.Id).Select(item => MapMaster(item.Master)).ToList()
        }).ToList();
    }

    private static async Task<List<StaffProfessionalMasterResponseDTO>> QueryMasters<TEntity>(
        IQueryable<TEntity> source,
        long institutionId,
        bool activeOnly,
        CancellationToken cancellationToken)
        where TEntity : InstitutionMasterEntity
    {
        var query = source.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }
        return await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name)
            .Select(item => new StaffProfessionalMasterResponseDTO
            {
                Id = item.Id, Code = item.Code, Name = item.Name, Description = item.Description,
                DisplayOrder = item.DisplayOrder, IsActive = item.IsActive
            }).ToListAsync(cancellationToken);
    }

    private Task<List<StaffProfessionalMasterResponseDTO>> QueryCapabilities(long institutionId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = context.StaffCapabilityMasters.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (activeOnly) query = query.Where(item => item.IsActive);
        return query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).Select(item => new StaffProfessionalMasterResponseDTO
        {
            Id = item.Id, Code = item.Code, Name = item.Name, Description = item.Description, DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive, RequiresCertification = item.RequiresCertification,
            CertificationValidityRequired = item.CertificationValidityRequired
        }).ToListAsync(cancellationToken);
    }

    private Task<List<StaffProfessionalMasterResponseDTO>> QueryDuties(long institutionId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = context.StaffDutyDefinitionMasters.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (activeOnly) query = query.Where(item => item.IsActive);
        return query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).Select(item => new StaffProfessionalMasterResponseDTO
        {
            Id = item.Id, Code = item.Code, Name = item.Name, Description = item.Description, DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive, DefaultFrequency = item.DefaultFrequency, RequiresEvidence = item.RequiresEvidence
        }).ToListAsync(cancellationToken);
    }

    private async Task<StaffProfessionalMasterResponseDTO> SaveCapabilityAsync(StaffProfessionalMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken)
    {
        var response = await SaveMasterEntityAsync(context.StaffCapabilityMasters, request, institutionId, actorStaffMemberId, cancellationToken);
        var entity = await context.StaffCapabilityMasters.SingleAsync(item => item.Id == response.Id, cancellationToken);
        entity.RequiresCertification = request.RequiresCertification;
        entity.CertificationValidityRequired = request.CertificationValidityRequired;
        await context.SaveChangesAsync(cancellationToken);
        return MapMaster(entity);
    }

    private async Task<StaffProfessionalMasterResponseDTO> SaveDutyAsync(StaffProfessionalMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken)
    {
        var response = await SaveMasterEntityAsync(context.StaffDutyDefinitionMasters, request, institutionId, actorStaffMemberId, cancellationToken);
        var entity = await context.StaffDutyDefinitionMasters.SingleAsync(item => item.Id == response.Id, cancellationToken);
        entity.DefaultFrequency = NormalizeOptional(request.DefaultFrequency);
        entity.RequiresEvidence = request.RequiresEvidence;
        await context.SaveChangesAsync(cancellationToken);
        return MapMaster(entity);
    }

    private async Task<StaffProfessionalMasterResponseDTO> SaveMasterEntityAsync<TEntity>(DbSet<TEntity> set, StaffProfessionalMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken)
        where TEntity : InstitutionMasterEntity, new()
    {
        TEntity entity;
        if (request.Id.HasValue)
        {
            entity = await set.FirstOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Master record was not found.");
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new TEntity { InstitutionId = institutionId, AddedById = actorStaffMemberId, AddedDateTime = DateTime.UtcNow };
            await set.AddAsync(entity, cancellationToken);
        }
        string code = NormalizeCode(request.Code);
        if (await set.AnyAsync(item => item.InstitutionId == institutionId && item.Id != entity.Id && !item.IsSoftDeleted && item.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            throw new ConflictException("A master record with this code already exists.");
        }
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return MapMaster(entity);
    }

    private static async Task<TEntity> FindMasterAsync<TEntity>(DbSet<TEntity> set, int id, long institutionId, CancellationToken cancellationToken)
        where TEntity : InstitutionMasterEntity
    {
        return await set.FirstOrDefaultAsync(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Master record was not found.");
    }

    private static StaffProfessionalMasterResponseDTO MapMaster(InstitutionMasterEntity entity)
    {
        var response = new StaffProfessionalMasterResponseDTO
        {
            Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description,
            DisplayOrder = entity.DisplayOrder, IsActive = entity.IsActive
        };
        if (entity is StaffCapabilityMaster capability)
        {
            response.RequiresCertification = capability.RequiresCertification;
            response.CertificationValidityRequired = capability.CertificationValidityRequired;
        }
        if (entity is StaffDutyDefinitionMaster duty)
        {
            response.DefaultFrequency = duty.DefaultFrequency;
            response.RequiresEvidence = duty.RequiresEvidence;
        }
        return response;
    }

    private static void ValidateMasterRequest(StaffProfessionalMasterRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationErrorException("Code and name are required.");
        }
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant().Replace(' ', '_');
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
