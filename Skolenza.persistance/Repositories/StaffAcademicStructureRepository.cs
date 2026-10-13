using skolenza.application.DTOS.Staff;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Persists institution-owned academic structure and effective-dated StaffMember academic assignments.</summary>
public sealed partial class StaffAcademicStructureRepository(SkolenzaDbContext context)
    : IStaffAcademicStructureRepository
{
    public async Task<List<TeachingAssignmentTypeOptionDTO>> GetTeachingAssignmentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await context.TeachingAssignmentTypes.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new TeachingAssignmentTypeOptionDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AcademicMasterResponseDTO>> GetMastersAsync(
        AcademicMasterType type,
        long institutionId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        List<InstitutionAcademicEntity> rows = type switch
        {
            AcademicMasterType.Session => (await Query(context.AcademicSessions, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            AcademicMasterType.Programme => (await Query(context.AcademicProgrammes, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            AcademicMasterType.Group => (await Query(context.AcademicGroups, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            AcademicMasterType.Section => (await Query(context.ClassSections, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            AcademicMasterType.Subject => (await Query(context.Subjects, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            AcademicMasterType.CourseOffering => (await Query(context.CourseOfferings, institutionId, activeOnly).ToListAsync(cancellationToken)).Cast<InstitutionAcademicEntity>().ToList(),
            _ => throw new ValidationErrorException("Unsupported academic master type.")
        };

        return await ProjectMastersAsync(rows, institutionId, cancellationToken);
    }

    public async Task<AcademicMasterResponseDTO> SaveMasterAsync(
        AcademicMasterType type,
        AcademicMasterRequestDTO request,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationErrorException("Code and name are required.");
        }

        InstitutionAcademicEntity entity = type switch
        {
            AcademicMasterType.Session => await SaveSessionAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            AcademicMasterType.Programme => await SaveProgrammeAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            AcademicMasterType.Group => await SaveGroupAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            AcademicMasterType.Section => await SaveSectionAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            AcademicMasterType.Subject => await SaveSubjectAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            AcademicMasterType.CourseOffering => await SaveCourseOfferingAsync(request, institutionId, actorStaffMemberId, cancellationToken),
            _ => throw new ValidationErrorException("Unsupported academic master type.")
        };

        return (await ProjectMastersAsync([entity], institutionId, cancellationToken)).Single();
    }

    public async Task DeleteMasterAsync(
        AcademicMasterType type,
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        InstitutionAcademicEntity entity = type switch
        {
            AcademicMasterType.Session => await FindAsync(context.AcademicSessions, id, institutionId, cancellationToken),
            AcademicMasterType.Programme => await FindAsync(context.AcademicProgrammes, id, institutionId, cancellationToken),
            AcademicMasterType.Group => await FindAsync(context.AcademicGroups, id, institutionId, cancellationToken),
            AcademicMasterType.Section => await FindAsync(context.ClassSections, id, institutionId, cancellationToken),
            AcademicMasterType.Subject => await FindAsync(context.Subjects, id, institutionId, cancellationToken),
            AcademicMasterType.CourseOffering => await FindAsync(context.CourseOfferings, id, institutionId, cancellationToken),
            _ => throw new ValidationErrorException("Unsupported academic master type.")
        };

        if (await HasDependenciesAsync(type, id, institutionId, cancellationToken))
        {
            throw new ConflictException("This academic record is in use. Remove dependent records before deleting it.");
        }

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffAcademicProfileResponseDTO> GetStaffProfileAsync(
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStaffMemberAsync(staffMemberId, institutionId, cancellationToken);

        var teaching = await (
            from assignment in context.StaffTeachingAssignments.AsNoTracking()
            join offering in context.CourseOfferings.AsNoTracking() on assignment.CourseOfferingId equals offering.Id
            join assignmentType in context.TeachingAssignmentTypes.AsNoTracking() on assignment.AssignmentType equals assignmentType.Id
            join subject in context.Subjects.AsNoTracking() on offering.SubjectId equals subject.Id
            join section in context.ClassSections.AsNoTracking() on assignment.ClassSectionId equals section.Id
            join academicGroup in context.AcademicGroups.AsNoTracking() on section.AcademicGroupId equals academicGroup.Id
            where assignment.InstitutionId == institutionId && assignment.StaffMemberId == staffMemberId
            orderby assignment.IsActive descending, academicGroup.Name, subject.Name
            select new StaffTeachingAssignmentResponseDTO
            {
                Id = assignment.Id,
                CourseOfferingId = offering.Id,
                CourseOfferingName = offering.Name,
                SubjectName = subject.Name,
                ClassSectionId = section.Id,
                ClassSectionName = section.Name,
                AcademicGroupName = academicGroup.Name,
                AssignmentType = assignment.AssignmentType,
                AssignmentTypeName = assignmentType.Name,
                ContributionPercent = assignment.ContributionPercent,
                EffectiveFrom = assignment.EffectiveFrom,
                EffectiveTo = assignment.EffectiveTo,
                IsPrimary = assignment.IsPrimary,
                IsActive = assignment.IsActive
            }).ToListAsync(cancellationToken);

        var classTeacher = await (
            from assignment in context.ClassTeacherAssignments.AsNoTracking()
            join section in context.ClassSections.AsNoTracking() on assignment.ClassSectionId equals section.Id
            join academicGroup in context.AcademicGroups.AsNoTracking() on section.AcademicGroupId equals academicGroup.Id
            where assignment.InstitutionId == institutionId && assignment.StaffMemberId == staffMemberId
            orderby assignment.IsActive descending, academicGroup.Name, section.Name
            select new ClassTeacherAssignmentResponseDTO
            {
                Id = assignment.Id,
                ClassSectionId = section.Id,
                ClassSectionName = section.Name,
                AcademicGroupName = academicGroup.Name,
                EffectiveFrom = assignment.EffectiveFrom,
                EffectiveTo = assignment.EffectiveTo,
                IsPrimary = assignment.IsPrimary,
                IsActive = assignment.IsActive
            }).ToListAsync(cancellationToken);

        return new StaffAcademicProfileResponseDTO
        {
            TeachingAssignments = teaching,
            ClassTeacherAssignments = classTeacher
        };
    }

    public async Task<StaffTeachingAssignmentResponseDTO> SaveTeachingAssignmentAsync(
        StaffTeachingAssignmentRequestDTO request,
        long institutionId,
        long staffMemberId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        if (request.CourseOfferingId <= 0 || request.ClassSectionId <= 0 || request.EffectiveFrom == default ||
            request.AssignmentType <= 0 || request.ContributionPercent <= 0 || request.ContributionPercent > 100)
        {
            throw new ValidationErrorException("Course offering, section, effective date and a valid contribution percentage are required.");
        }

        await EnsureStaffMemberAsync(staffMemberId, institutionId, cancellationToken);
        if (!await context.TeachingAssignmentTypes.AsNoTracking().AnyAsync(
            item => item.Id == request.AssignmentType && item.IsActive,
            cancellationToken))
        {
            throw new ValidationErrorException("The selected teaching assignment type is unavailable.");
        }

        var offering = await context.CourseOfferings.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == request.CourseOfferingId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new ValidationErrorException("The selected course offering is unavailable.");
        var section = await context.ClassSections.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == request.ClassSectionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new ValidationErrorException("The selected section is unavailable.");
        if (offering.AcademicGroupId != section.AcademicGroupId)
        {
            throw new ValidationErrorException("The selected section does not belong to the course offering's academic group.");
        }
        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);

        StaffTeachingAssignment entity;
        if (request.Id.HasValue)
        {
            entity = await context.StaffTeachingAssignments.SingleOrDefaultAsync(
                item => item.Id == request.Id && item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId,
                cancellationToken) ?? throw new KeyNotFoundException("Teaching assignment was not found.");
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new StaffTeachingAssignment
            {
                InstitutionId = institutionId,
                StaffMemberId = staffMemberId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };
            await context.StaffTeachingAssignments.AddAsync(entity, cancellationToken);
        }

        bool duplicate = await context.StaffTeachingAssignments.AnyAsync(
            item => item.Id != entity.Id && item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId &&
                item.ClassSectionId == request.ClassSectionId && item.CourseOfferingId == request.CourseOfferingId &&
                item.IsActive && request.IsActive &&
                item.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) &&
                (item.EffectiveTo ?? DateOnly.MaxValue) >= request.EffectiveFrom,
            cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("The StaffMember already has this teaching assignment for the selected effective date.");
        }

        entity.CourseOfferingId = request.CourseOfferingId;
        entity.ClassSectionId = request.ClassSectionId;
        entity.AssignmentType = request.AssignmentType;
        entity.ContributionPercent = request.ContributionPercent;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return (await GetStaffProfileAsync(institutionId, staffMemberId, cancellationToken)).TeachingAssignments.Single(item => item.Id == entity.Id);
    }

    public async Task<ClassTeacherAssignmentResponseDTO> SaveClassTeacherAssignmentAsync(
        ClassTeacherAssignmentRequestDTO request,
        long institutionId,
        long staffMemberId,
        long actorStaffMemberId,
        CancellationToken cancellationToken = default)
    {
        if (request.ClassSectionId <= 0 || request.EffectiveFrom == default)
        {
            throw new ValidationErrorException("Section and effective date are required.");
        }
        await EnsureStaffMemberAsync(staffMemberId, institutionId, cancellationToken);

        if (!await context.ClassSections.AnyAsync(
            item => item.Id == request.ClassSectionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken))
        {
            throw new ValidationErrorException("The selected section is unavailable.");
        }
        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);

        ClassTeacherAssignment entity;
        if (request.Id.HasValue)
        {
            entity = await context.ClassTeacherAssignments.SingleOrDefaultAsync(
                item => item.Id == request.Id && item.InstitutionId == institutionId && item.StaffMemberId == staffMemberId,
                cancellationToken) ?? throw new KeyNotFoundException("Class teacher assignment was not found.");
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new ClassTeacherAssignment
            {
                InstitutionId = institutionId,
                StaffMemberId = staffMemberId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };
            await context.ClassTeacherAssignments.AddAsync(entity, cancellationToken);
        }

        if (request.IsActive && request.IsPrimary && await context.ClassTeacherAssignments.AnyAsync(
            item => item.Id != entity.Id && item.InstitutionId == institutionId && item.ClassSectionId == request.ClassSectionId &&
                item.IsActive && item.IsPrimary &&
                item.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) &&
                (item.EffectiveTo ?? DateOnly.MaxValue) >= request.EffectiveFrom,
            cancellationToken))
        {
            throw new ConflictException("The selected section already has an active primary class teacher.");
        }

        entity.ClassSectionId = request.ClassSectionId;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return (await GetStaffProfileAsync(institutionId, staffMemberId, cancellationToken)).ClassTeacherAssignments.Single(item => item.Id == entity.Id);
    }

    public async Task DeleteTeachingAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default)
    {
        var entity = await context.StaffTeachingAssignments.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId,
            cancellationToken) ?? throw new KeyNotFoundException("Teaching assignment was not found.");
        context.StaffTeachingAssignments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteClassTeacherAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default)
    {
        var entity = await context.ClassTeacherAssignments.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId,
            cancellationToken) ?? throw new KeyNotFoundException("Class teacher assignment was not found.");
        context.ClassTeacherAssignments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<TEntity> Query<TEntity>(DbSet<TEntity> set, long institutionId, bool activeOnly)
        where TEntity : InstitutionAcademicEntity
    {
        var query = set.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        return activeOnly ? query.Where(item => item.IsActive).OrderBy(item => item.Name) : query.OrderBy(item => item.Name);
    }

    private async Task<TEntity> FindAsync<TEntity>(DbSet<TEntity> set, long id, long institutionId, CancellationToken cancellationToken)
        where TEntity : InstitutionAcademicEntity
    {
        return await set.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Academic record was not found.");
    }

    private async Task<TEntity> PrepareAsync<TEntity>(DbSet<TEntity> set, AcademicMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken)
        where TEntity : InstitutionAcademicEntity, new()
    {
        TEntity entity;
        if (request.Id.HasValue)
        {
            entity = await FindAsync(set, request.Id.Value, institutionId, cancellationToken);
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new TEntity
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };
            await set.AddAsync(entity, cancellationToken);
        }

        string code = request.Code.Trim().ToUpperInvariant();
        if (await set.AnyAsync(item => item.Id != entity.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted && item.Code == code, cancellationToken))
        {
            throw new ConflictException("An academic record with this code already exists.");
        }
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.IsActive = request.IsActive;
        return entity;
    }

    private async Task<AcademicSession> SaveSessionAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (!request.StartDate.HasValue || !request.EndDate.HasValue)
        {
            throw new ValidationErrorException("Academic session start and end dates are required.");
        }
        ValidateDateRange(request.StartDate.Value, request.EndDate);
        await ValidateCampusAsync(request.CampusId, institutionId, token);
        if (request.ParentId.HasValue)
        {
            if (request.Id == request.ParentId || !await Owns(context.AcademicSessions, request.ParentId.Value, institutionId, token))
            {
                throw new ValidationErrorException("The selected parent academic session is unavailable.");
            }
        }
        var entity = await PrepareAsync(context.AcademicSessions, request, institutionId, actorId, token);
        entity.CampusId = request.CampusId;
        entity.ParentAcademicSessionId = request.ParentId;
        entity.SessionType = request.Type;
        entity.StartDate = request.StartDate.Value;
        entity.EndDate = request.EndDate.Value;
        await context.SaveChangesAsync(token);
        return entity;
    }

    private async Task<AcademicProgramme> SaveProgrammeAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        var entity = await PrepareAsync(context.AcademicProgrammes, request, institutionId, actorId, token);
        entity.Level = string.IsNullOrWhiteSpace(request.Level) ? null : request.Level.Trim();
        await context.SaveChangesAsync(token);
        return entity;
    }

    private async Task<AcademicGroup> SaveGroupAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (!request.AcademicSessionId.HasValue || !await Owns(context.AcademicSessions, request.AcademicSessionId.Value, institutionId, token))
        {
            throw new ValidationErrorException("A valid academic session is required.");
        }
        if (request.AcademicProgrammeId.HasValue && !await Owns(context.AcademicProgrammes, request.AcademicProgrammeId.Value, institutionId, token))
        {
            throw new ValidationErrorException("The selected academic programme is unavailable.");
        }
        if (request.ParentId.HasValue)
        {
            var parent = await context.AcademicGroups.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == request.ParentId.Value && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                token);
            if (request.Id == request.ParentId || parent is null || parent.AcademicSessionId != request.AcademicSessionId.Value)
            {
                throw new ValidationErrorException("The parent academic group must belong to the selected session.");
            }
        }
        var entity = await PrepareAsync(context.AcademicGroups, request, institutionId, actorId, token);
        entity.AcademicSessionId = request.AcademicSessionId.Value;
        entity.AcademicProgrammeId = request.AcademicProgrammeId;
        entity.ParentAcademicGroupId = request.ParentId;
        entity.GroupType = request.Type;
        await context.SaveChangesAsync(token);
        return entity;
    }

    private async Task<ClassSection> SaveSectionAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (!request.AcademicGroupId.HasValue || !await Owns(context.AcademicGroups, request.AcademicGroupId.Value, institutionId, token))
        {
            throw new ValidationErrorException("A valid academic group is required.");
        }
        if (request.Capacity is <= 0)
        {
            throw new ValidationErrorException("Capacity must be greater than zero when provided.");
        }
        await ValidateCampusAsync(request.CampusId, institutionId, token);
        var entity = await PrepareAsync(context.ClassSections, request, institutionId, actorId, token);
        entity.AcademicGroupId = request.AcademicGroupId.Value;
        entity.CampusId = request.CampusId;
        entity.Capacity = request.Capacity;
        await context.SaveChangesAsync(token);
        return entity;
    }

    private async Task<Subject> SaveSubjectAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        var entity = await PrepareAsync(context.Subjects, request, institutionId, actorId, token);
        entity.SubjectType = request.Type;
        await context.SaveChangesAsync(token);
        return entity;
    }

    private async Task<CourseOffering> SaveCourseOfferingAsync(AcademicMasterRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (!request.AcademicSessionId.HasValue || !request.AcademicGroupId.HasValue || !request.SubjectId.HasValue ||
            !await Owns(context.AcademicSessions, request.AcademicSessionId.Value, institutionId, token) ||
            !await Owns(context.AcademicGroups, request.AcademicGroupId.Value, institutionId, token) ||
            !await Owns(context.Subjects, request.SubjectId.Value, institutionId, token))
        {
            throw new ValidationErrorException("Valid session, academic group and subject are required.");
        }
        if (request.RequiredPeriodsPerWeek is <= 0)
        {
            throw new ValidationErrorException("Required periods per week must be greater than zero when provided.");
        }
        var group = await context.AcademicGroups.AsNoTracking().SingleAsync(item => item.Id == request.AcademicGroupId.Value, token);
        if (group.AcademicSessionId != request.AcademicSessionId.Value)
        {
            throw new ValidationErrorException("The academic group does not belong to the selected session.");
        }
        bool duplicate = await context.CourseOfferings.AnyAsync(
            item => item.Id != request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted &&
                item.AcademicSessionId == request.AcademicSessionId.Value &&
                item.AcademicGroupId == request.AcademicGroupId.Value && item.SubjectId == request.SubjectId.Value,
            token);
        if (duplicate)
        {
            throw new ConflictException("This subject is already offered to the selected academic group in this session.");
        }
        var entity = await PrepareAsync(context.CourseOfferings, request, institutionId, actorId, token);
        entity.AcademicSessionId = request.AcademicSessionId.Value;
        entity.AcademicGroupId = request.AcademicGroupId.Value;
        entity.SubjectId = request.SubjectId.Value;
        entity.CurriculumVersion = string.IsNullOrWhiteSpace(request.CurriculumVersion) ? null : request.CurriculumVersion.Trim();
        entity.RequiredPeriodsPerWeek = request.RequiredPeriodsPerWeek;
        await context.SaveChangesAsync(token);
        return entity;
    }

    private static Task<bool> Owns<TEntity>(DbSet<TEntity> set, long id, long institutionId, CancellationToken token)
        where TEntity : InstitutionAcademicEntity
    {
        return set.AnyAsync(item => item.Id == id && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted, token);
    }

    private async Task<bool> HasDependenciesAsync(AcademicMasterType type, long id, long institutionId, CancellationToken token)
    {
        return type switch
        {
            AcademicMasterType.Session => await context.AcademicSessions.AnyAsync(item => item.InstitutionId == institutionId && item.ParentAcademicSessionId == id && !item.IsSoftDeleted, token) || await context.AcademicGroups.AnyAsync(item => item.InstitutionId == institutionId && item.AcademicSessionId == id && !item.IsSoftDeleted, token) || await context.CourseOfferings.AnyAsync(item => item.InstitutionId == institutionId && item.AcademicSessionId == id && !item.IsSoftDeleted, token),
            AcademicMasterType.Programme => await context.AcademicGroups.AnyAsync(item => item.InstitutionId == institutionId && item.AcademicProgrammeId == id && !item.IsSoftDeleted, token),
            AcademicMasterType.Group => await context.AcademicGroups.AnyAsync(item => item.InstitutionId == institutionId && item.ParentAcademicGroupId == id && !item.IsSoftDeleted, token) || await context.ClassSections.AnyAsync(item => item.InstitutionId == institutionId && item.AcademicGroupId == id && !item.IsSoftDeleted, token) || await context.CourseOfferings.AnyAsync(item => item.InstitutionId == institutionId && item.AcademicGroupId == id && !item.IsSoftDeleted, token),
            AcademicMasterType.Section => await context.StaffTeachingAssignments.AnyAsync(item => item.InstitutionId == institutionId && item.ClassSectionId == id, token) || await context.ClassTeacherAssignments.AnyAsync(item => item.InstitutionId == institutionId && item.ClassSectionId == id, token),
            AcademicMasterType.Subject => await context.CourseOfferings.AnyAsync(item => item.InstitutionId == institutionId && item.SubjectId == id && !item.IsSoftDeleted, token),
            AcademicMasterType.CourseOffering => await context.StaffTeachingAssignments.AnyAsync(item => item.InstitutionId == institutionId && item.CourseOfferingId == id, token),
            _ => false
        };
    }

    private async Task<List<AcademicMasterResponseDTO>> ProjectMastersAsync(List<InstitutionAcademicEntity> rows, long institutionId, CancellationToken token)
    {
        var sessions = await context.AcademicSessions.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).ToDictionaryAsync(item => item.Id, item => item.Name, token);
        var programmes = await context.AcademicProgrammes.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).ToDictionaryAsync(item => item.Id, item => item.Name, token);
        var groups = await context.AcademicGroups.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).ToDictionaryAsync(item => item.Id, item => item.Name, token);
        var subjects = await context.Subjects.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).ToDictionaryAsync(item => item.Id, item => item.Name, token);
        return rows.Select(item =>
        {
            var session = item as AcademicSession;
            var programme = item as AcademicProgramme;
            var group = item as AcademicGroup;
            var section = item as ClassSection;
            var subject = item as Subject;
            var offering = item as CourseOffering;
            return new AcademicMasterResponseDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive,
                CampusId = session?.CampusId ?? section?.CampusId,
                ParentId = session?.ParentAcademicSessionId ?? group?.ParentAcademicGroupId,
                AcademicSessionId = group?.AcademicSessionId ?? offering?.AcademicSessionId,
                AcademicSessionName = TryName(sessions, group?.AcademicSessionId ?? offering?.AcademicSessionId),
                AcademicProgrammeId = group?.AcademicProgrammeId,
                AcademicProgrammeName = TryName(programmes, group?.AcademicProgrammeId),
                AcademicGroupId = section?.AcademicGroupId ?? offering?.AcademicGroupId,
                AcademicGroupName = TryName(groups, section?.AcademicGroupId ?? offering?.AcademicGroupId),
                SubjectId = offering?.SubjectId,
                SubjectName = TryName(subjects, offering?.SubjectId),
                Type = session?.SessionType ?? group?.GroupType ?? subject?.SubjectType ?? 0,
                StartDate = session?.StartDate,
                EndDate = session?.EndDate,
                Level = programme?.Level,
                Capacity = section?.Capacity,
                CurriculumVersion = offering?.CurriculumVersion,
                RequiredPeriodsPerWeek = offering?.RequiredPeriodsPerWeek
            };
        }).ToList();
    }

    private static string? TryName(IReadOnlyDictionary<long, string> lookup, long? id) =>
        id.HasValue && lookup.TryGetValue(id.Value, out string? name) ? name : null;

    private static void ValidateDateRange(DateOnly start, DateOnly? end)
    {
        if (end.HasValue && end.Value < start)
        {
            throw new ValidationErrorException("End date cannot be before start date.");
        }
    }

    private async Task ValidateCampusAsync(long? campusId, long institutionId, CancellationToken token)
    {
        if (campusId.HasValue && !await context.Campuss.AsNoTracking().AnyAsync(
            item => item.Id == campusId.Value && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            token))
        {
            throw new ValidationErrorException("The selected campus is unavailable.");
        }
    }

    private async Task EnsureStaffMemberAsync(long staffMemberId, long institutionId, CancellationToken token)
    {
        if (!await context.StaffMembers.AsNoTracking().AnyAsync(
            item => item.Id == staffMemberId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            token))
        {
            throw new ValidationErrorException("The selected StaffMember is unavailable.");
        }
    }
}
