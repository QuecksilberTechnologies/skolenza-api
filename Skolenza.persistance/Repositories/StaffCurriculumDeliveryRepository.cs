using Microsoft.EntityFrameworkCore;
using skolenza.application.DTOS.Staff;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories;

public sealed partial class StaffAcademicStructureRepository
{
    public Task<List<AcademicWorkflowStatusOptionDTO>> GetWorkflowStatusesAsync(CancellationToken cancellationToken = default) =>
        context.AcademicWorkflowStatuses.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new AcademicWorkflowStatusOptionDTO { Id = item.Id, Code = item.Code, Name = item.Name })
            .ToListAsync(cancellationToken);

    public async Task<List<CurriculumFrameworkResponseDTO>> GetCurriculumFrameworksAsync(long institutionId, CancellationToken cancellationToken = default) =>
        await ProjectFrameworks(context.CurriculumFrameworks.AsNoTracking().Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted))
            .ToListAsync(cancellationToken);

    /// <summary>Returns one institution-scoped page of curriculum frameworks.</summary>
    public Task<PagedResponseDTO<CurriculumFrameworkResponseDTO>> GetCurriculumFrameworksAsync(
        long institutionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ProjectFrameworks(context.CurriculumFrameworks.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted))
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<CurriculumFrameworkResponseDTO> SaveCurriculumFrameworkAsync(CurriculumFrameworkRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        if (request.AcademicSessionId <= 0 || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.AuthorityName) || string.IsNullOrWhiteSpace(request.Version) || request.EffectiveFrom == default)
            throw new ValidationErrorException("Academic session, code, name, authority, version and effective date are required.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            throw new ValidationErrorException("Effective-to date cannot be before effective-from date.");
        if (!await context.AcademicSessions.AsNoTracking().AnyAsync(item => item.Id == request.AcademicSessionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The selected academic session is unavailable.");

        CurriculumFrameworkMaster entity;
        if (request.Id.HasValue)
        {
            entity = await context.CurriculumFrameworks.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Curriculum framework was not found.");
            await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken);
            entity.UpdatedById = actorId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new CurriculumFrameworkMaster { InstitutionId = institutionId, WorkflowStatusId = await StatusIdAsync("DRAFT", cancellationToken), AddedById = actorId, AddedDateTime = DateTime.UtcNow };
            context.CurriculumFrameworks.Add(entity);
        }
        entity.AcademicSessionId = request.AcademicSessionId;
        entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.AuthorityName = request.AuthorityName.Trim(); entity.Version = request.Version.Trim();
        entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo; entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectFrameworks(context.CurriculumFrameworks.AsNoTracking().Where(item => item.Id == entity.Id)).SingleAsync(cancellationToken);
    }

    public async Task<List<CurriculumUnitResponseDTO>> GetCurriculumUnitsAsync(long frameworkId, long institutionId, CancellationToken cancellationToken = default)
    {
        await EnsureFrameworkAvailableAsync(frameworkId, institutionId, cancellationToken);
        var units = await ProjectUnits(frameworkId, institutionId).ToListAsync(cancellationToken);
        var ids = units.Select(item => item.Id).ToList();
        var topics = await context.CurriculumTopics.AsNoTracking().Where(item => ids.Contains(item.CurriculumUnitId) && !item.IsSoftDeleted).OrderBy(item => item.Sequence).Select(item => Topic(item)).ToListAsync(cancellationToken);
        foreach (var unit in units) unit.Topics = topics.Where(item => item.CurriculumUnitId == unit.Id).ToList();
        return units;
    }

    /// <summary>Returns unit headers only; topics have independent pagination.</summary>
    public async Task<PagedResponseDTO<CurriculumUnitResponseDTO>> GetCurriculumUnitsAsync(
        long frameworkId,
        long institutionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await EnsureFrameworkAvailableAsync(frameworkId, institutionId, cancellationToken);
        return await ProjectUnits(frameworkId, institutionId)
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
    }

    /// <summary>Returns a page of topics from one institution-owned unit.</summary>
    public async Task<PagedResponseDTO<CurriculumTopicResponseDTO>> GetCurriculumTopicsAsync(
        long unitId,
        long institutionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!await context.CurriculumUnits.AsNoTracking().AnyAsync(
                item => item.Id == unitId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken))
        {
            throw new KeyNotFoundException("Curriculum unit was not found.");
        }

        return await context.CurriculumTopics.AsNoTracking()
            .Where(item => item.CurriculumUnitId == unitId && item.InstitutionId == institutionId && !item.IsSoftDeleted)
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new CurriculumTopicResponseDTO
            {
                Id = item.Id,
                CurriculumUnitId = item.CurriculumUnitId,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                Sequence = item.Sequence,
                PlannedPeriods = item.PlannedPeriods,
                LearningOutcome = item.LearningOutcome,
                IsActive = item.IsActive
            })
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
    }

    private async Task EnsureFrameworkAvailableAsync(long frameworkId, long institutionId, CancellationToken token)
    {
        if (!await context.CurriculumFrameworks.AsNoTracking().AnyAsync(
                item => item.Id == frameworkId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                token))
        {
            throw new KeyNotFoundException("Curriculum framework was not found.");
        }
    }

    private IQueryable<CurriculumUnitResponseDTO> ProjectUnits(long frameworkId, long institutionId)
    {
        return from unit in context.CurriculumUnits.AsNoTracking()
               join offering in context.CourseOfferings.AsNoTracking() on unit.CourseOfferingId equals offering.Id
               where unit.InstitutionId == institutionId && unit.CurriculumFrameworkId == frameworkId && !unit.IsSoftDeleted
               orderby unit.Sequence, unit.Name, unit.Id
               select new CurriculumUnitResponseDTO
               {
                   Id = unit.Id,
                   CurriculumFrameworkId = unit.CurriculumFrameworkId,
                   CourseOfferingId = unit.CourseOfferingId,
                   CourseOfferingName = offering.Name,
                   ParentCurriculumUnitId = unit.ParentCurriculumUnitId,
                   Code = unit.Code,
                   Name = unit.Name,
                   Description = unit.Description,
                   Sequence = unit.Sequence,
                   PlannedPeriods = unit.PlannedPeriods,
                   LearningOutcome = unit.LearningOutcome,
                   IsActive = unit.IsActive,
                   Topics = null
               };
    }

    public async Task<CurriculumUnitResponseDTO> SaveCurriculumUnitAsync(CurriculumUnitRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        if (request.CurriculumFrameworkId <= 0 || request.CourseOfferingId <= 0 || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || request.Sequence <= 0 || request.PlannedPeriods <= 0)
            throw new ValidationErrorException("Framework, course offering, code, name, sequence and planned periods are required.");
        if (!await context.CurriculumFrameworks.AsNoTracking().AnyAsync(item => item.Id == request.CurriculumFrameworkId && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ||
            !await context.CourseOfferings.AsNoTracking().AnyAsync(item => item.Id == request.CourseOfferingId && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The selected framework or course offering is unavailable.");
        if (request.ParentCurriculumUnitId.HasValue && !await context.CurriculumUnits.AsNoTracking().AnyAsync(item => item.Id == request.ParentCurriculumUnitId && item.CurriculumFrameworkId == request.CurriculumFrameworkId && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The selected parent unit is unavailable.");
        CurriculumUnit entity;
        if (request.Id.HasValue)
        {
            entity = await context.CurriculumUnits.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Curriculum unit was not found.");
            entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else { entity = new CurriculumUnit { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow }; context.CurriculumUnits.Add(entity); }
        entity.CurriculumFrameworkId = request.CurriculumFrameworkId; entity.CourseOfferingId = request.CourseOfferingId; entity.ParentCurriculumUnitId = request.ParentCurriculumUnitId;
        entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.Description = request.Description?.Trim(); entity.Sequence = request.Sequence; entity.PlannedPeriods = request.PlannedPeriods; entity.LearningOutcome = request.LearningOutcome?.Trim(); entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return (await GetCurriculumUnitsAsync(request.CurriculumFrameworkId, institutionId, cancellationToken)).Single(item => item.Id == entity.Id);
    }

    public async Task<CurriculumFrameworkResponseDTO> ReviewCurriculumFrameworkAsync(
        WorkflowReviewRequestDTO request,
        long institutionId,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.CurriculumFrameworks.SingleOrDefaultAsync(
            item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Curriculum framework was not found.");

        await EnsureWorkflowTransitionAsync(entity.WorkflowStatusId, request.WorkflowStatusId, cancellationToken);
        entity.WorkflowStatusId = request.WorkflowStatusId;
        entity.ReviewedById = actorId;
        entity.ReviewedDateTime = DateTime.UtcNow;
        entity.ReviewRemark = request.Remark?.Trim();
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return await ProjectFrameworks(context.CurriculumFrameworks.AsNoTracking())
            .SingleAsync(item => item.Id == entity.Id, cancellationToken);
    }

    public async Task<CurriculumTopicResponseDTO> SaveCurriculumTopicAsync(CurriculumTopicRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        if (request.CurriculumUnitId <= 0 || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || request.Sequence <= 0 || request.PlannedPeriods <= 0)
            throw new ValidationErrorException("Curriculum unit, code, name, sequence and planned periods are required.");
        if (!await context.CurriculumUnits.AsNoTracking().AnyAsync(item => item.Id == request.CurriculumUnitId && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The selected curriculum unit is unavailable.");
        CurriculumTopic entity;
        if (request.Id.HasValue)
        {
            entity = await context.CurriculumTopics.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Curriculum topic was not found.");
            entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else { entity = new CurriculumTopic { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow }; context.CurriculumTopics.Add(entity); }
        entity.CurriculumUnitId = request.CurriculumUnitId; entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.Description = request.Description?.Trim(); entity.Sequence = request.Sequence; entity.PlannedPeriods = request.PlannedPeriods; entity.LearningOutcome = request.LearningOutcome?.Trim(); entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return Topic(entity);
    }

    public Task<List<LessonPlanResponseDTO>> GetLessonPlansAsync(long institutionId, CancellationToken cancellationToken = default) => ProjectPlans(institutionId).ToListAsync(cancellationToken);

    /// <summary>Returns plans with their complete selected-topic set for one page.</summary>
    public Task<PagedResponseDTO<LessonPlanResponseDTO>> GetLessonPlansAsync(
        long institutionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ProjectPlans(institutionId)
            .OrderByDescending(item => item.PlannedDate)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
    }

    public Task<List<StaffTeachingAssignmentResponseDTO>> GetTeachingAssignmentsAsync(long institutionId, CancellationToken cancellationToken = default) =>
        (from assignment in context.StaffTeachingAssignments.AsNoTracking()
         join staff in context.StaffMembers.AsNoTracking() on assignment.StaffMemberId equals staff.Id
         join offering in context.CourseOfferings.AsNoTracking() on assignment.CourseOfferingId equals offering.Id
         join assignmentType in context.TeachingAssignmentTypes.AsNoTracking() on assignment.AssignmentType equals assignmentType.Id
         join subject in context.Subjects.AsNoTracking() on offering.SubjectId equals subject.Id
         join section in context.ClassSections.AsNoTracking() on assignment.ClassSectionId equals section.Id
         join academicGroup in context.AcademicGroups.AsNoTracking() on section.AcademicGroupId equals academicGroup.Id
         where assignment.InstitutionId == institutionId && assignment.IsActive
         orderby academicGroup.Name, subject.Name
         select new StaffTeachingAssignmentResponseDTO
         {
             Id = assignment.Id,
             StaffMemberName = ((staff.FirstName ?? "") + " " + (staff.LastName ?? "")).Trim(),
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

    public async Task<LessonPlanResponseDTO> SaveLessonPlanAsync(LessonPlanRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        if (request.StaffTeachingAssignmentId <= 0 || request.PlannedDate == default || string.IsNullOrWhiteSpace(request.Objectives) || request.CurriculumTopicIds.Count == 0)
            throw new ValidationErrorException("Teaching assignment, planned date, objectives and at least one curriculum topic are required.");
        var assignment = await context.StaffTeachingAssignments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.StaffTeachingAssignmentId && item.InstitutionId == institutionId && item.IsActive, cancellationToken) ?? throw new ValidationErrorException("The selected teaching assignment is unavailable.");
        var validTopics = await (from topic in context.CurriculumTopics.AsNoTracking() join unit in context.CurriculumUnits.AsNoTracking() on topic.CurriculumUnitId equals unit.Id where topic.InstitutionId == institutionId && unit.CourseOfferingId == assignment.CourseOfferingId && request.CurriculumTopicIds.Contains(topic.Id) && topic.IsActive && !topic.IsSoftDeleted select topic.Id).Distinct().ToListAsync(cancellationToken);
        if (validTopics.Count != request.CurriculumTopicIds.Distinct().Count()) throw new ValidationErrorException("One or more curriculum topics do not belong to the teaching assignment's course offering.");
        LessonPlan entity;
        if (request.Id.HasValue)
        {
            entity = await context.LessonPlans.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Lesson plan was not found.");
            await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
            context.LessonPlanCurriculumTopics.RemoveRange(context.LessonPlanCurriculumTopics.Where(item => item.LessonPlanId == entity.Id));
        }
        else { entity = new LessonPlan { InstitutionId = institutionId, WorkflowStatusId = await StatusIdAsync("DRAFT", cancellationToken), AddedById = actorId, AddedDateTime = DateTime.UtcNow }; context.LessonPlans.Add(entity); }
        entity.StaffTeachingAssignmentId = request.StaffTeachingAssignmentId; entity.PlannedDate = request.PlannedDate; entity.PeriodReference = request.PeriodReference?.Trim(); entity.Objectives = request.Objectives.Trim(); entity.TeachingMethod = request.TeachingMethod?.Trim(); entity.Resources = request.Resources?.Trim(); entity.Activities = request.Activities?.Trim(); entity.Assessment = request.Assessment?.Trim(); entity.Homework = request.Homework?.Trim(); entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        context.LessonPlanCurriculumTopics.AddRange(validTopics.Select(topicId => new LessonPlanCurriculumTopic { LessonPlanId = entity.Id, CurriculumTopicId = topicId }));
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectPlans(institutionId).SingleAsync(item => item.Id == entity.Id, cancellationToken);
    }

    public async Task<LessonPlanResponseDTO> ReviewLessonPlanAsync(WorkflowReviewRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.LessonPlans.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Lesson plan was not found.");
        await EnsureWorkflowTransitionAsync(entity.WorkflowStatusId, request.WorkflowStatusId, cancellationToken); entity.WorkflowStatusId = request.WorkflowStatusId; entity.ReviewedById = actorId; entity.ReviewedDateTime = DateTime.UtcNow; entity.ReviewRemark = request.Remark?.Trim(); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken); return await ProjectPlans(institutionId).SingleAsync(item => item.Id == entity.Id, cancellationToken);
    }

    public async Task<List<LessonDeliveryResponseDTO>> GetLessonDeliveriesAsync(long institutionId, CancellationToken cancellationToken = default)
    {
        var rows = await ProjectDeliveries(institutionId).ToListAsync(cancellationToken);
        await LoadDeliveryCoverageAsync(rows, cancellationToken);
        return rows;
    }

    /// <summary>Pages delivery headers before loading complete coverage for that page.</summary>
    public async Task<PagedResponseDTO<LessonDeliveryResponseDTO>> GetLessonDeliveriesAsync(
        long institutionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var page = await ProjectDeliveries(institutionId)
            .OrderByDescending(item => item.DeliveredDate)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
        await LoadDeliveryCoverageAsync(page.Data, cancellationToken);
        return page;
    }

    private IQueryable<LessonDeliveryResponseDTO> ProjectDeliveries(long institutionId)
    {
        return from log in context.LessonDeliveryLogs.AsNoTracking()
               join status in context.AcademicWorkflowStatuses.AsNoTracking() on log.WorkflowStatusId equals status.Id
               where log.InstitutionId == institutionId && !log.IsSoftDeleted
               orderby log.DeliveredDate descending, log.Id descending
               select new LessonDeliveryResponseDTO
               {
                   Id = log.Id,
                   LessonPlanId = log.LessonPlanId,
                   DeliveredDate = log.DeliveredDate,
                   CoverageNotes = log.CoverageNotes,
                   Homework = log.Homework,
                   EvidenceReference = log.EvidenceReference,
                   ExceptionReason = log.ExceptionReason,
                   ReplanAction = log.ReplanAction,
                   WorkflowStatusId = log.WorkflowStatusId,
                   WorkflowStatusName = status.Name,
                   IsActive = log.IsActive
               };
    }

    private async Task LoadDeliveryCoverageAsync(List<LessonDeliveryResponseDTO> rows, CancellationToken token)
    {
        var ids = rows.Select(item => item.Id).ToList();
        var coverage = await context.LessonDeliveryCoverages.AsNoTracking()
            .Where(item => ids.Contains(item.LessonDeliveryLogId))
            .ToListAsync(token);
        foreach (var row in rows)
        {
            row.Coverage = coverage.Where(item => item.LessonDeliveryLogId == row.Id)
                .Select(item => new LessonDeliveryCoverageRequestDTO
                {
                    CurriculumTopicId = item.CurriculumTopicId,
                    CompletionPercent = item.CompletionPercent
                })
                .ToList();
        }
    }

    public async Task<LessonDeliveryResponseDTO> SaveLessonDeliveryAsync(LessonDeliveryRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        if (request.LessonPlanId <= 0 || request.DeliveredDate == default || string.IsNullOrWhiteSpace(request.CoverageNotes) || request.Coverage.Count == 0 || request.Coverage.Any(item => item.CompletionPercent < 0 || item.CompletionPercent > 100))
            throw new ValidationErrorException("Lesson plan, delivered date, coverage notes and valid topic coverage are required.");
        var approved = await StatusIdAsync("APPROVED", cancellationToken);
        var plan = await context.LessonPlans.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.LessonPlanId && item.InstitutionId == institutionId && item.WorkflowStatusId == approved && !item.IsSoftDeleted, cancellationToken) ?? throw new ValidationErrorException("Delivery can be recorded only for an approved lesson plan.");
        var planTopics = await context.LessonPlanCurriculumTopics.AsNoTracking().Where(item => item.LessonPlanId == plan.Id).Select(item => item.CurriculumTopicId).ToListAsync(cancellationToken);
        if (request.Coverage.Select(item => item.CurriculumTopicId).Distinct().Any(topicId => !planTopics.Contains(topicId))) throw new ValidationErrorException("Coverage contains a topic outside the approved lesson plan.");
        LessonDeliveryLog entity;
        if (request.Id.HasValue)
        {
            entity = await context.LessonDeliveryLogs.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Lesson delivery was not found.");
            await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
            context.LessonDeliveryCoverages.RemoveRange(context.LessonDeliveryCoverages.Where(item => item.LessonDeliveryLogId == entity.Id));
        }
        else { entity = new LessonDeliveryLog { InstitutionId = institutionId, WorkflowStatusId = await StatusIdAsync("DRAFT", cancellationToken), AddedById = actorId, AddedDateTime = DateTime.UtcNow }; context.LessonDeliveryLogs.Add(entity); }
        entity.LessonPlanId = request.LessonPlanId; entity.DeliveredDate = request.DeliveredDate; entity.CoverageNotes = request.CoverageNotes.Trim(); entity.Homework = request.Homework?.Trim(); entity.EvidenceReference = request.EvidenceReference?.Trim(); entity.ExceptionReason = request.ExceptionReason?.Trim(); entity.ReplanAction = request.ReplanAction?.Trim(); entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        context.LessonDeliveryCoverages.AddRange(request.Coverage.GroupBy(item => item.CurriculumTopicId).Select(group => group.Last()).Select(item => new LessonDeliveryCoverage { LessonDeliveryLogId = entity.Id, CurriculumTopicId = item.CurriculumTopicId, CompletionPercent = item.CompletionPercent }));
        await context.SaveChangesAsync(cancellationToken); return (await GetLessonDeliveriesAsync(institutionId, cancellationToken)).Single(item => item.Id == entity.Id);
    }

    public async Task<LessonDeliveryResponseDTO> ReviewLessonDeliveryAsync(WorkflowReviewRequestDTO request, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.LessonDeliveryLogs.SingleOrDefaultAsync(item => item.Id == request.Id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken) ?? throw new KeyNotFoundException("Lesson delivery was not found.");
        await EnsureWorkflowTransitionAsync(entity.WorkflowStatusId, request.WorkflowStatusId, cancellationToken); entity.WorkflowStatusId = request.WorkflowStatusId; entity.ReviewedById = actorId; entity.ReviewedDateTime = DateTime.UtcNow; entity.ReviewRemark = request.Remark?.Trim(); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken); return (await GetLessonDeliveriesAsync(institutionId, cancellationToken)).Single(item => item.Id == entity.Id);
    }

    public async Task<List<SyllabusProgressResponseDTO>> GetSyllabusProgressAsync(long institutionId, long? courseOfferingId, CancellationToken cancellationToken = default)
    {
        var result = await GetSyllabusProgressAsync(institutionId, courseOfferingId,
            PaginationDefaults.PageNumber, int.MaxValue, cancellationToken);
        return result.Data;
    }

    /// <summary>Pages units while retaining every approved coverage input for each returned unit.</summary>
    public async Task<PagedResponseDTO<SyllabusProgressResponseDTO>> GetSyllabusProgressAsync(
        long institutionId,
        long? courseOfferingId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var approved = await StatusIdAsync("APPROVED", cancellationToken);
        var query = from unit in context.CurriculumUnits.AsNoTracking()
                    join framework in context.CurriculumFrameworks.AsNoTracking() on unit.CurriculumFrameworkId equals framework.Id
                    join offering in context.CourseOfferings.AsNoTracking() on unit.CourseOfferingId equals offering.Id
                    where unit.InstitutionId == institutionId && !unit.IsSoftDeleted && !framework.IsSoftDeleted && (!courseOfferingId.HasValue || unit.CourseOfferingId == courseOfferingId)
                    select new { unit, framework, offering };
        var page = await query.OrderBy(item => item.unit.Id)
            .ToPageAsync(pageNumber, pageSize, cancellationToken);
        var units = page.Data;
        var unitIds = units.Select(item => item.unit.Id).ToList();
        var topics = await context.CurriculumTopics.AsNoTracking().Where(item => unitIds.Contains(item.CurriculumUnitId) && !item.IsSoftDeleted && item.IsActive).ToListAsync(cancellationToken);
        var topicIds = topics.Select(item => item.Id).ToList();
        var coverage = await (from item in context.LessonDeliveryCoverages.AsNoTracking() join log in context.LessonDeliveryLogs.AsNoTracking() on item.LessonDeliveryLogId equals log.Id where log.InstitutionId == institutionId && log.WorkflowStatusId == approved && !log.IsSoftDeleted && topicIds.Contains(item.CurriculumTopicId) select item).ToListAsync(cancellationToken);
        var data = units.Select(row =>
        {
            var unitTopics = topics.Where(item => item.CurriculumUnitId == row.unit.Id).ToList();
            var completed = unitTopics.Select(topic => coverage
                .Where(item => item.CurriculumTopicId == topic.Id)
                .Select(item => item.CompletionPercent)
                .DefaultIfEmpty(0)
                .Max()).ToList();
            return new SyllabusProgressResponseDTO
            {
                CurriculumFrameworkId = row.framework.Id,
                CurriculumFrameworkName = row.framework.Name,
                CourseOfferingId = row.offering.Id,
                CourseOfferingName = row.offering.Name,
                CurriculumUnitId = row.unit.Id,
                CurriculumUnitName = row.unit.Name,
                TopicCount = unitTopics.Count,
                CompletionPercent = completed.Count == 0 ? 0 : Math.Round(completed.Average(), 2)
            };
        }).ToList();
        return new PagedResponseDTO<SyllabusProgressResponseDTO>(data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task DeleteCurriculumFrameworkAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.CurriculumFrameworks.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Curriculum framework was not found.");
        await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken);
        if (await context.CurriculumUnits.AnyAsync(item => item.CurriculumFrameworkId == id && !item.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException("Remove the framework's curriculum units before deleting it.");
        }
        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCurriculumUnitAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.CurriculumUnits.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Curriculum unit was not found.");
        var hasDependencies = await context.CurriculumUnits.AnyAsync(item => item.ParentCurriculumUnitId == id && !item.IsSoftDeleted, cancellationToken) ||
            await context.CurriculumTopics.AnyAsync(item => item.CurriculumUnitId == id && !item.IsSoftDeleted, cancellationToken);
        if (hasDependencies)
        {
            throw new ConflictException("Remove child units and topics before deleting this curriculum unit.");
        }
        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCurriculumTopicAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.CurriculumTopics.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Curriculum topic was not found.");
        var hasDependencies = await context.LessonPlanCurriculumTopics.AnyAsync(item => item.CurriculumTopicId == id, cancellationToken) ||
            await context.LessonDeliveryCoverages.AnyAsync(item => item.CurriculumTopicId == id, cancellationToken);
        if (hasDependencies)
        {
            throw new ConflictException("This topic is used by a lesson plan or delivery and cannot be deleted.");
        }
        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLessonPlanAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.LessonPlans.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Lesson plan was not found.");
        await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken);
        if (await context.LessonDeliveryLogs.AnyAsync(item => item.LessonPlanId == id && !item.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException("Remove lesson delivery records before deleting this lesson plan.");
        }
        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLessonDeliveryAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken = default)
    {
        var entity = await context.LessonDeliveryLogs.SingleOrDefaultAsync(
            item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Lesson delivery was not found.");
        await EnsureDraftAsync(entity.WorkflowStatusId, cancellationToken);
        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<CurriculumFrameworkResponseDTO> ProjectFrameworks(IQueryable<CurriculumFrameworkMaster> source) => from item in source join session in context.AcademicSessions.AsNoTracking() on item.AcademicSessionId equals session.Id join status in context.AcademicWorkflowStatuses.AsNoTracking() on item.WorkflowStatusId equals status.Id orderby item.Name select new CurriculumFrameworkResponseDTO { Id = item.Id, AcademicSessionId = item.AcademicSessionId, AcademicSessionName = session.Name, Code = item.Code, Name = item.Name, AuthorityName = item.AuthorityName, Version = item.Version, EffectiveFrom = item.EffectiveFrom, EffectiveTo = item.EffectiveTo, WorkflowStatusId = item.WorkflowStatusId, WorkflowStatusName = status.Name, IsActive = item.IsActive };
    private IQueryable<LessonPlanResponseDTO> ProjectPlans(long institutionId) => from plan in context.LessonPlans.AsNoTracking() join assignment in context.StaffTeachingAssignments.AsNoTracking() on plan.StaffTeachingAssignmentId equals assignment.Id join staff in context.StaffMembers.AsNoTracking() on assignment.StaffMemberId equals staff.Id join offering in context.CourseOfferings.AsNoTracking() on assignment.CourseOfferingId equals offering.Id join status in context.AcademicWorkflowStatuses.AsNoTracking() on plan.WorkflowStatusId equals status.Id where plan.InstitutionId == institutionId && !plan.IsSoftDeleted orderby plan.PlannedDate descending select new LessonPlanResponseDTO { Id = plan.Id, StaffTeachingAssignmentId = plan.StaffTeachingAssignmentId, StaffMemberName = ((staff.FirstName ?? "") + " " + (staff.LastName ?? "")).Trim(), CourseOfferingName = offering.Name, PlannedDate = plan.PlannedDate, PeriodReference = plan.PeriodReference, Objectives = plan.Objectives, TeachingMethod = plan.TeachingMethod, Resources = plan.Resources, Activities = plan.Activities, Assessment = plan.Assessment, Homework = plan.Homework, WorkflowStatusId = plan.WorkflowStatusId, WorkflowStatusName = status.Name, IsActive = plan.IsActive, Topics = (from link in context.LessonPlanCurriculumTopics.AsNoTracking() join topic in context.CurriculumTopics.AsNoTracking() on link.CurriculumTopicId equals topic.Id where link.LessonPlanId == plan.Id select Topic(topic)).ToList() };
    private static CurriculumTopicResponseDTO Topic(CurriculumTopic item) => new() { Id = item.Id, CurriculumUnitId = item.CurriculumUnitId, Code = item.Code, Name = item.Name, Description = item.Description, Sequence = item.Sequence, PlannedPeriods = item.PlannedPeriods, LearningOutcome = item.LearningOutcome, IsActive = item.IsActive };
    private async Task<short> StatusIdAsync(string code, CancellationToken token) => (await context.AcademicWorkflowStatuses.AsNoTracking().SingleOrDefaultAsync(item => item.Code == code && item.IsActive, token))?.Id ?? throw new InvalidOperationException($"Academic workflow status '{code}' is not configured.");
    private async Task EnsureDraftAsync(short statusId, CancellationToken token) { if (statusId != await StatusIdAsync("DRAFT", token) && statusId != await StatusIdAsync("REJECTED", token)) throw new ConflictException("Only draft or rejected records can be edited."); }
    private async Task EnsureWorkflowTransitionAsync(short currentStatusId, short targetStatusId, CancellationToken token)
    {
        var statusCodes = await context.AcademicWorkflowStatuses.AsNoTracking()
            .Where(item => item.IsActive && (item.Id == currentStatusId || item.Id == targetStatusId))
            .ToDictionaryAsync(item => item.Id, item => item.Code, token);

        if (!statusCodes.TryGetValue(currentStatusId, out var currentCode) ||
            !statusCodes.TryGetValue(targetStatusId, out var targetCode))
        {
            throw new ValidationErrorException("Select a valid workflow status.");
        }

        var isAllowed =
            ((currentCode == "DRAFT" || currentCode == "REJECTED") && targetCode == "SUBMITTED") ||
            (currentCode == "SUBMITTED" && (targetCode == "APPROVED" || targetCode == "REJECTED"));

        if (!isAllowed)
        {
            throw new ConflictException($"Workflow status cannot change from {currentCode} to {targetCode}.");
        }
    }
}
