using Microsoft.EntityFrameworkCore;
using skolenza.application.DTOS.Staff;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories;

public sealed partial class StaffAcademicStructureRepository
{
    public async Task<StaffOperationsOptionsResponseDTO> GetStaffOperationsOptionsAsync(
        long institutionId,
        CancellationToken token)
    {
        return new StaffOperationsOptionsResponseDTO
        {
            StaffMembers = await context.StaffMembers.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && !item.IsSoftDeleted)
                .OrderBy(item => item.FirstName)
                .ThenBy(item => item.LastName)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = (item.FirstName + " " + item.LastName).Trim()
                }).ToListAsync(token),
            Campuses = await context.Campuss.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && !item.IsSoftDeleted)
                .OrderBy(item => item.LocationName)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = item.LocationName
                }).ToListAsync(token),
            Departments = await context.Departments.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && !item.IsSoftDeleted)
                .OrderBy(item => item.DepartmentName)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = item.DepartmentName
                }).ToListAsync(token),
            StaffCategories = await context.InstitutionStaffCategoryMasters.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && !item.IsSoftDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = item.Name
                }).ToListAsync(token),
            AccessRoles = await context.Roles.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && item.IsSoftDeleted != true)
                .OrderBy(item => item.RoleName)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = item.RoleName ?? string.Empty
                }).ToListAsync(token),
            StaffPositions = await context.StaffPositionMasters.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId
                    && item.IsActive
                    && !item.IsSoftDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new StaffOperationsOptionDTO
                {
                    Id = item.Id,
                    Name = item.Name
                }).ToListAsync(token)
        };
    }

    public async Task<List<TimetablePeriodResponseDTO>> GetTimetablePeriodsAsync(long institutionId, CancellationToken token) =>
        await (from period in context.TimetablePeriodMasters.AsNoTracking()
               join session in context.AcademicSessions.AsNoTracking() on period.AcademicSessionId equals session.Id
               where period.InstitutionId == institutionId && !period.IsSoftDeleted
               orderby period.Sequence
               select new TimetablePeriodResponseDTO
               {
                   Id = period.Id, AcademicSessionId = period.AcademicSessionId, AcademicSessionName = session.Name,
                   CampusId = period.CampusId, Code = period.Code, Name = period.Name, Description = period.Description,
                   Sequence = period.Sequence, StartTime = period.StartTime, EndTime = period.EndTime, IsActive = period.IsActive
               }).ToListAsync(token);

    public async Task<TimetablePeriodResponseDTO> SaveTimetablePeriodAsync(TimetablePeriodRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || request.EndTime <= request.StartTime)
            throw new ValidationErrorException("Code, name and a valid time range are required.");
        var session = await context.AcademicSessions.SingleOrDefaultAsync(x => x.Id == request.AcademicSessionId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, token)
            ?? throw new ValidationErrorException("The selected academic session is unavailable.");
        var entity = request.Id.HasValue
            ? await context.TimetablePeriodMasters.SingleOrDefaultAsync(x => x.Id == request.Id && x.InstitutionId == institutionId, token) ?? throw new KeyNotFoundException("Timetable period was not found.")
            : new TimetablePeriodMaster { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        var duplicate = await context.TimetablePeriodMasters.AnyAsync(x => x.InstitutionId == institutionId && x.Code.ToLower() == request.Code.Trim().ToLower() && !x.IsSoftDeleted && x.Id != entity.Id, token);
        if (duplicate) throw new ConflictException("A timetable period with this code already exists.");
        entity.AcademicSessionId = request.AcademicSessionId; entity.CampusId = request.CampusId; entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim(); entity.Sequence = request.Sequence; entity.StartTime = request.StartTime; entity.EndTime = request.EndTime; entity.IsActive = request.IsActive;
        if (entity.Id == 0) context.TimetablePeriodMasters.Add(entity); else { entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; }
        await context.SaveChangesAsync(token);
        return new TimetablePeriodResponseDTO { Id = entity.Id, AcademicSessionId = entity.AcademicSessionId, AcademicSessionName = session.Name, CampusId = entity.CampusId, Code = entity.Code, Name = entity.Name, Description = entity.Description, Sequence = entity.Sequence, StartTime = entity.StartTime, EndTime = entity.EndTime, IsActive = entity.IsActive };
    }

    public async Task<List<TimetableEntryResponseDTO>> GetTimetableEntriesAsync(long institutionId, CancellationToken token) =>
        await (from entry in context.TimetableEntries.AsNoTracking()
               join section in context.ClassSections.AsNoTracking() on entry.ClassSectionId equals section.Id
               join offering in context.CourseOfferings.AsNoTracking() on entry.CourseOfferingId equals offering.Id
               join period in context.TimetablePeriodMasters.AsNoTracking() on entry.TimetablePeriodMasterId equals period.Id
               join assignment in context.StaffTeachingAssignments.AsNoTracking() on entry.StaffTeachingAssignmentId equals assignment.Id
               join staff in context.StaffMembers.AsNoTracking() on assignment.StaffMemberId equals staff.Id
               where entry.InstitutionId == institutionId && !entry.IsSoftDeleted
               orderby entry.DayOfWeek, period.Sequence
               select new TimetableEntryResponseDTO { Id = entry.Id, AcademicSessionId = entry.AcademicSessionId, ClassSectionId = entry.ClassSectionId, CourseOfferingId = entry.CourseOfferingId, TimetablePeriodMasterId = entry.TimetablePeriodMasterId, StaffTeachingAssignmentId = entry.StaffTeachingAssignmentId, DayOfWeek = entry.DayOfWeek, RoomReference = entry.RoomReference, EffectiveFrom = entry.EffectiveFrom, EffectiveTo = entry.EffectiveTo, IsActive = entry.IsActive, SectionName = section.Name, CourseOfferingName = offering.Name, PeriodName = period.Name, TeacherName = (staff.FirstName + " " + staff.LastName).Trim() }).ToListAsync(token);

    public async Task<TimetableEntryResponseDTO> SaveTimetableEntryAsync(TimetableEntryRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (request.DayOfWeek is < 1 or > 7 || request.EffectiveTo < request.EffectiveFrom) throw new ValidationErrorException("A valid weekday and effective date range are required.");
        var assignment = await context.StaffTeachingAssignments.SingleOrDefaultAsync(x => x.Id == request.StaffTeachingAssignmentId && x.InstitutionId == institutionId && x.IsActive, token) ?? throw new ValidationErrorException("The selected teaching assignment is unavailable.");
        if (assignment.ClassSectionId != request.ClassSectionId || assignment.CourseOfferingId != request.CourseOfferingId) throw new ValidationErrorException("The timetable scope must match the teaching assignment.");
        var period = await context.TimetablePeriodMasters.SingleOrDefaultAsync(x => x.Id == request.TimetablePeriodMasterId && x.InstitutionId == institutionId && x.IsActive, token) ?? throw new ValidationErrorException("The selected period is unavailable.");
        var overlap = await context.TimetableEntries.AnyAsync(x => x.InstitutionId == institutionId && x.Id != request.Id && x.IsActive && !x.IsSoftDeleted && x.DayOfWeek == request.DayOfWeek && x.TimetablePeriodMasterId == request.TimetablePeriodMasterId && (x.StaffTeachingAssignmentId == request.StaffTeachingAssignmentId || x.ClassSectionId == request.ClassSectionId) && (x.EffectiveTo == null || x.EffectiveTo >= request.EffectiveFrom) && (request.EffectiveTo == null || x.EffectiveFrom <= request.EffectiveTo), token);
        if (overlap) throw new ConflictException("The teacher or section already has a timetable entry in this period.");
        var entity = request.Id.HasValue ? await context.TimetableEntries.SingleOrDefaultAsync(x => x.Id == request.Id && x.InstitutionId == institutionId, token) ?? throw new KeyNotFoundException("Timetable entry was not found.") : new TimetableEntry { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        entity.AcademicSessionId = request.AcademicSessionId; entity.ClassSectionId = request.ClassSectionId; entity.CourseOfferingId = request.CourseOfferingId; entity.TimetablePeriodMasterId = request.TimetablePeriodMasterId; entity.StaffTeachingAssignmentId = request.StaffTeachingAssignmentId; entity.DayOfWeek = request.DayOfWeek; entity.RoomReference = request.RoomReference?.Trim(); entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo; entity.IsActive = request.IsActive;
        if (entity.Id == 0) context.TimetableEntries.Add(entity); else { entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; }
        await context.SaveChangesAsync(token);
        return (await GetTimetableEntriesAsync(institutionId, token)).Single(x => x.Id == entity.Id);
    }

    public async Task<List<SubstituteWorkflowResponseDTO>> GetSubstituteWorkflowsAsync(long institutionId, CancellationToken token)
    {
        var rows = await context.SubstituteWorkflowMasters.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted).OrderBy(x => x.Name).ToListAsync(token);
        var steps = await context.SubstituteWorkflowSteps.AsNoTracking().Where(x => x.InstitutionId == institutionId).OrderBy(x => x.Sequence).ToListAsync(token);
        return rows.Select(x => new SubstituteWorkflowResponseDTO { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, RequiresSubstituteAcceptance = x.RequiresSubstituteAcceptance, AllowSelfNomination = x.AllowSelfNomination, AutoApproveWhenNoSteps = x.AutoApproveWhenNoSteps, IsActive = x.IsActive, Steps = steps.Where(s => s.SubstituteWorkflowMasterId == x.Id).Select(s => new SubstituteWorkflowStepDTO { Id = s.Id, Sequence = s.Sequence, ApproverTypeCode = s.ApproverTypeCode, AccessRoleId = s.AccessRoleId, StaffPositionMasterId = s.StaffPositionMasterId, StaffMemberId = s.StaffMemberId, IsActive = s.IsActive }).ToList() }).ToList();
    }

    public async Task<SubstituteWorkflowResponseDTO> SaveSubstituteWorkflowAsync(SubstituteWorkflowRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name)) throw new ValidationErrorException("Workflow code and name are required.");
        var invalid = request.Steps.Any(x => x.Sequence <= 0 || !new[] { "MANAGER", "ACCESS_ROLE", "POSITION", "STAFF_MEMBER" }.Contains(x.ApproverTypeCode));
        if (invalid || request.Steps.GroupBy(x => x.Sequence).Any(x => x.Count() > 1)) throw new ValidationErrorException("Workflow steps require unique positive sequences and supported approver types.");
        var entity = request.Id.HasValue ? await context.SubstituteWorkflowMasters.SingleOrDefaultAsync(x => x.Id == request.Id && x.InstitutionId == institutionId, token) ?? throw new KeyNotFoundException("Substitute workflow was not found.") : new SubstituteWorkflowMaster { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        entity.Code = request.Code.Trim(); entity.Name = request.Name.Trim(); entity.Description = request.Description?.Trim(); entity.RequiresSubstituteAcceptance = request.RequiresSubstituteAcceptance; entity.AllowSelfNomination = request.AllowSelfNomination; entity.AutoApproveWhenNoSteps = request.AutoApproveWhenNoSteps; entity.IsActive = request.IsActive;
        if (entity.Id == 0) context.SubstituteWorkflowMasters.Add(entity); else { entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; context.SubstituteWorkflowSteps.RemoveRange(context.SubstituteWorkflowSteps.Where(x => x.SubstituteWorkflowMasterId == entity.Id)); }
        await context.SaveChangesAsync(token);
        context.SubstituteWorkflowSteps.AddRange(request.Steps.Select(x => new SubstituteWorkflowStep { InstitutionId = institutionId, SubstituteWorkflowMasterId = entity.Id, Sequence = x.Sequence, ApproverTypeCode = x.ApproverTypeCode, AccessRoleId = x.AccessRoleId, StaffPositionMasterId = x.StaffPositionMasterId, StaffMemberId = x.StaffMemberId, IsActive = x.IsActive, AddedById = actorId, AddedDateTime = DateTime.UtcNow }));
        await context.SaveChangesAsync(token);
        return (await GetSubstituteWorkflowsAsync(institutionId, token)).Single(x => x.Id == entity.Id);
    }

    public async Task<CommunicationSettingDTO> GetCommunicationSettingAsync(long institutionId, CancellationToken token)
    {
        var x = await context.InstitutionCommunicationSettings.AsNoTracking().SingleOrDefaultAsync(x => x.InstitutionId == institutionId, token);
        return x == null ? new CommunicationSettingDTO() : new CommunicationSettingDTO { EnableInApp = x.EnableInApp, EnableEmail = x.EnableEmail, EnableSms = x.EnableSms, EnablePush = x.EnablePush, RetentionDays = x.RetentionDays };
    }

    public async Task<CommunicationSettingDTO> SaveCommunicationSettingAsync(CommunicationSettingDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (request.RetentionDays is < 1 or > 3650) throw new ValidationErrorException("Retention days must be between 1 and 3650.");
        var x = await context.InstitutionCommunicationSettings.SingleOrDefaultAsync(x => x.InstitutionId == institutionId, token) ?? new InstitutionCommunicationSetting { InstitutionId = institutionId, AddedById = actorId, AddedDateTime = DateTime.UtcNow };
        x.EnableInApp = request.EnableInApp; x.EnableEmail = request.EnableEmail; x.EnableSms = request.EnableSms; x.EnablePush = request.EnablePush; x.RetentionDays = request.RetentionDays; x.IsActive = true;
        if (x.Id == 0) context.InstitutionCommunicationSettings.Add(x); else { x.UpdatedById = actorId; x.UpdatedDateTime = DateTime.UtcNow; }
        await context.SaveChangesAsync(token); return request;
    }

    public async Task<List<StaffQueryCategoryResponseDTO>> GetQueryCategoriesAsync(
        long institutionId,
        bool activeOnly,
        CancellationToken token)
    {
        return await context.StaffQueryCategoryMasters
            .AsNoTracking()
            .Where(item => item.InstitutionId == institutionId
                && !item.IsSoftDeleted
                && (!activeOnly || item.IsActive))
            .OrderBy(item => item.Name)
            .Select(item => new StaffQueryCategoryResponseDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive
            })
            .ToListAsync(token);
    }

    public async Task<StaffQueryCategoryResponseDTO> SaveQueryCategoryAsync(
        StaffQueryCategoryRequestDTO request,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationErrorException("Query category code and name are required.");
        }

        var entity = request.Id.HasValue
            ? await context.StaffQueryCategoryMasters.SingleOrDefaultAsync(
                item => item.Id == request.Id
                    && item.InstitutionId == institutionId
                    && !item.IsSoftDeleted,
                token)
                ?? throw new KeyNotFoundException("The query category was not found.")
            : new StaffQueryCategoryMaster
            {
                InstitutionId = institutionId,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            };

        var duplicate = await context.StaffQueryCategoryMasters.AnyAsync(
            item => item.InstitutionId == institutionId
                && !item.IsSoftDeleted
                && item.Id != entity.Id
                && item.Code.ToLower() == code.ToLower(),
            token);
        if (duplicate)
        {
            throw new ConflictException("A query category with this code already exists.");
        }

        entity.Code = code;
        entity.Name = name;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        if (entity.Id == 0)
        {
            context.StaffQueryCategoryMasters.Add(entity);
        }
        else
        {
            entity.UpdatedById = actorId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(token);
        return new StaffQueryCategoryResponseDTO
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task DeactivateQueryCategoryAsync(
        long id,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var entity = await context.StaffQueryCategoryMasters.SingleOrDefaultAsync(
            item => item.Id == id
                && item.InstitutionId == institutionId
                && !item.IsSoftDeleted,
            token)
            ?? throw new KeyNotFoundException("The query category was not found.");
        entity.IsActive = false;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
    }

    public async Task<List<StaffConversationResponseDTO>> GetConversationsAsync(long institutionId, long actorId, CancellationToken token) =>
        await context.StaffConversations.AsNoTracking().Where(x => x.InstitutionId == institutionId && (x.CreatedByStaffMemberId == actorId || context.StaffConversationParticipants.Any(p => p.StaffConversationId == x.Id && p.StaffMemberId == actorId && p.IsActive))).OrderByDescending(x => x.CreatedDateTime).Select(x => new StaffConversationResponseDTO { Id = x.Id, ConversationTypeCode = x.ConversationTypeCode, Subject = x.Subject, StatusCode = x.StatusCode, CreatedByStaffMemberId = x.CreatedByStaffMemberId, CreatedDateTime = x.CreatedDateTime, ParticipantCount = context.StaffConversationParticipants.Count(p => p.StaffConversationId == x.Id && p.IsActive), MessageCount = context.StaffConversationMessages.Count(m => m.StaffConversationId == x.Id && !m.IsSoftDeleted), IsClosed = x.IsClosed }).ToListAsync(token);

    public async Task<StaffConversationResponseDTO> CreateConversationAsync(
        StaffConversationRequestDTO request,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var type = request.ConversationTypeCode.Trim().ToUpperInvariant();
        if (!new[] { "DIRECT", "QUERY", "BROADCAST" }.Contains(type)
            || string.IsNullOrWhiteSpace(request.Subject)
            || string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            throw new ValidationErrorException(
                "Conversation type, subject and initial message are required.");
        }

        if (type == "QUERY")
        {
            var categoryExists = request.StaffQueryCategoryMasterId.HasValue
                && await context.StaffQueryCategoryMasters.AnyAsync(
                    item => item.Id == request.StaffQueryCategoryMasterId
                        && item.InstitutionId == institutionId
                        && item.IsActive
                        && !item.IsSoftDeleted,
                    token);
            if (!categoryExists)
            {
                throw new ValidationErrorException(
                    "Select an active query category owned by this institution.");
            }
        }

        var participantIds = request.ParticipantStaffMemberIds.ToHashSet();
        if (type == "BROADCAST")
        {
            if (request.Audiences.Count == 0)
            {
                throw new ValidationErrorException("Select at least one broadcast audience.");
            }

            participantIds.UnionWith(await ResolveAudienceRecipientsAsync(
                request.Audiences,
                institutionId,
                token));
        }
        else if (participantIds.Count == 0)
        {
            throw new ValidationErrorException(
                "Select at least one institution StaffMember for this conversation.");
        }

        participantIds.Add(actorId);
        var validCount = await context.StaffMembers.CountAsync(
            item => item.InstitutionId == institutionId
                && participantIds.Contains(item.Id)
                && item.IsActive
                && !item.IsSoftDeleted,
            token);
        if (validCount != participantIds.Count)
        {
            throw new ValidationErrorException(
                "Every participant must be an active StaffMember of this institution.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var conversation = new StaffConversation
        {
            InstitutionId = institutionId,
            ConversationTypeCode = type,
            StaffQueryCategoryMasterId = type == "QUERY"
                ? request.StaffQueryCategoryMasterId
                : null,
            Subject = request.Subject.Trim(),
            StatusCode = "OPEN",
            CreatedByStaffMemberId = actorId,
            CreatedDateTime = DateTime.UtcNow
        };
        context.StaffConversations.Add(conversation);
        await context.SaveChangesAsync(token);

        context.StaffConversationParticipants.AddRange(participantIds.Select(id =>
            new StaffConversationParticipant
            {
                InstitutionId = institutionId,
                StaffConversationId = conversation.Id,
                StaffMemberId = id,
                ParticipantRoleCode = id == actorId ? "OWNER" : "PARTICIPANT",
                LastReadDateTime = id == actorId ? DateTime.UtcNow : null
            }));
        context.StaffConversationMessages.Add(new StaffConversationMessage
        {
            InstitutionId = institutionId,
            StaffConversationId = conversation.Id,
            SenderStaffMemberId = actorId,
            Body = request.InitialMessage.Trim(),
            SentDateTime = DateTime.UtcNow
        });
        context.StaffBroadcastAudiences.AddRange(request.Audiences.Select(audience =>
            new StaffBroadcastAudience
            {
                InstitutionId = institutionId,
                StaffConversationId = conversation.Id,
                AudienceTypeCode = audience.AudienceTypeCode.Trim().ToUpperInvariant(),
                TargetId = audience.TargetId
            }));
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return (await GetConversationsAsync(institutionId, actorId, token))
            .Single(item => item.Id == conversation.Id);
    }

    public async Task<StaffConversationResponseDTO> AddMessageAsync(long conversationId, StaffMessageRequestDTO request, long institutionId, long actorId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Body)) throw new ValidationErrorException("Message text is required.");
        var allowed = await context.StaffConversations.AnyAsync(x => x.Id == conversationId && x.InstitutionId == institutionId && !x.IsClosed && (x.CreatedByStaffMemberId == actorId || context.StaffConversationParticipants.Any(p => p.StaffConversationId == x.Id && p.StaffMemberId == actorId && p.IsActive)), token);
        if (!allowed) throw new KeyNotFoundException("An open conversation was not found for this StaffMember.");
        context.StaffConversationMessages.Add(new StaffConversationMessage { InstitutionId = institutionId, StaffConversationId = conversationId, SenderStaffMemberId = actorId, Body = request.Body.Trim(), SentDateTime = DateTime.UtcNow });
        await context.SaveChangesAsync(token);
        return (await GetConversationsAsync(institutionId, actorId, token)).Single(x => x.Id == conversationId);
    }

    public async Task<StaffConversationDetailResponseDTO> GetConversationAsync(
        long conversationId,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var conversation = await (from row in context.StaffConversations.AsNoTracking()
                                  join category in context.StaffQueryCategoryMasters.AsNoTracking()
                                      on row.StaffQueryCategoryMasterId equals category.Id into categoryRows
                                  from category in categoryRows.DefaultIfEmpty()
                                  where row.Id == conversationId
                                     && row.InstitutionId == institutionId
                                     && context.StaffConversationParticipants.Any(participant =>
                                         participant.StaffConversationId == row.Id
                                         && participant.StaffMemberId == actorId
                                         && participant.IsActive)
                                  select new StaffConversationDetailResponseDTO
                                  {
                                      Id = row.Id,
                                      ConversationTypeCode = row.ConversationTypeCode,
                                      Subject = row.Subject,
                                      StatusCode = row.StatusCode,
                                      CreatedByStaffMemberId = row.CreatedByStaffMemberId,
                                      CreatedDateTime = row.CreatedDateTime,
                                      ParticipantCount = context.StaffConversationParticipants.Count(
                                          participant => participant.StaffConversationId == row.Id
                                              && participant.IsActive),
                                      MessageCount = context.StaffConversationMessages.Count(
                                          message => message.StaffConversationId == row.Id
                                              && !message.IsSoftDeleted),
                                      IsClosed = row.IsClosed,
                                      QueryCategoryName = category == null ? null : category.Name
                                  }).SingleOrDefaultAsync(token)
            ?? throw new KeyNotFoundException("The conversation was not found for this StaffMember.");

        conversation.Messages = await (from message in context.StaffConversationMessages.AsNoTracking()
                                       join sender in context.StaffMembers.AsNoTracking()
                                           on message.SenderStaffMemberId equals sender.Id
                                       where message.StaffConversationId == conversationId
                                          && message.InstitutionId == institutionId
                                          && !message.IsSoftDeleted
                                       orderby message.SentDateTime
                                       select new StaffConversationMessageResponseDTO
                                       {
                                           Id = message.Id,
                                           SenderStaffMemberId = message.SenderStaffMemberId,
                                           SenderName = (sender.FirstName + " " + sender.LastName).Trim(),
                                           Body = message.Body,
                                           SentDateTime = message.SentDateTime
                                       }).ToListAsync(token);
        return conversation;
    }

    public async Task<StaffConversationDetailResponseDTO> MarkConversationReadAsync(
        long conversationId,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var participant = await context.StaffConversationParticipants.SingleOrDefaultAsync(
            item => item.StaffConversationId == conversationId
                && item.InstitutionId == institutionId
                && item.StaffMemberId == actorId
                && item.IsActive,
            token)
            ?? throw new KeyNotFoundException("The conversation was not found for this StaffMember.");
        participant.LastReadDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return await GetConversationAsync(conversationId, institutionId, actorId, token);
    }

    public async Task<StaffConversationDetailResponseDTO> CloseConversationAsync(
        long conversationId,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var conversation = await context.StaffConversations.SingleOrDefaultAsync(
            item => item.Id == conversationId
                && item.InstitutionId == institutionId
                && item.CreatedByStaffMemberId == actorId,
            token)
            ?? throw new KeyNotFoundException(
                "Only the conversation owner can close this conversation.");
        conversation.IsClosed = true;
        conversation.StatusCode = "CLOSED";
        conversation.ClosedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return await GetConversationAsync(conversationId, institutionId, actorId, token);
    }

    private async Task<HashSet<long>> ResolveAudienceRecipientsAsync(
        IReadOnlyCollection<StaffAudienceDTO> audiences,
        long institutionId,
        CancellationToken token)
    {
        var allowedTypes = new[]
        {
            "CAMPUS", "DEPARTMENT", "STAFF_CATEGORY", "ACCESS_ROLE", "STAFF_MEMBER"
        };
        var normalized = audiences
            .Select(item => new StaffAudienceDTO
            {
                AudienceTypeCode = item.AudienceTypeCode.Trim().ToUpperInvariant(),
                TargetId = item.TargetId
            })
            .ToList();
        if (normalized.Any(item => !allowedTypes.Contains(item.AudienceTypeCode)
            || item.TargetId <= 0))
        {
            throw new ValidationErrorException("A broadcast audience type or target is invalid.");
        }

        var recipients = new HashSet<long>();
        foreach (var audience in normalized)
        {
            IQueryable<long> query = audience.AudienceTypeCode switch
            {
                "CAMPUS" => context.StaffMemberLocationAssignments
                    .Where(item => item.InstitutionId == institutionId
                        && item.CampusId == audience.TargetId
                        && item.IsActive
                        && !item.IsSoftDeleted)
                    .Select(item => item.StaffMemberId),
                "DEPARTMENT" => context.StaffMembers
                    .Where(item => item.InstitutionId == institutionId
                        && item.DepartmentId == audience.TargetId
                        && item.IsActive
                        && !item.IsSoftDeleted)
                    .Select(item => item.Id),
                "STAFF_CATEGORY" => context.StaffMembers
                    .Where(item => item.InstitutionId == institutionId
                        && item.InstitutionStaffCategoryMasterId == audience.TargetId
                        && item.IsActive
                        && !item.IsSoftDeleted)
                    .Select(item => item.Id),
                "ACCESS_ROLE" => from role in context.UserRoles
                                 join staff in context.StaffMembers on role.StaffMemberId equals staff.Id
                                 where role.RoleId == audience.TargetId
                                    && role.IsActive
                                    && role.IsSoftDeleted != true
                                    && staff.InstitutionId == institutionId
                                    && staff.IsActive
                                    && !staff.IsSoftDeleted
                                 select staff.Id,
                "STAFF_MEMBER" => context.StaffMembers
                    .Where(item => item.InstitutionId == institutionId
                        && item.Id == audience.TargetId
                        && item.IsActive
                        && !item.IsSoftDeleted)
                    .Select(item => item.Id),
                _ => Enumerable.Empty<long>().AsQueryable()
            };
            var matches = await query.Distinct().ToListAsync(token);
            if (matches.Count == 0)
            {
                throw new ValidationErrorException(
                    $"Audience {audience.AudienceTypeCode}:{audience.TargetId} has no active institution recipients.");
            }

            recipients.UnionWith(matches);
        }

        return recipients;
    }

    public async Task<List<StaffAbsenceImpactResponseDTO>> GetAbsenceImpactsAsync(
        long institutionId,
        CancellationToken token)
    {
        return await (from impact in context.StaffAbsenceImpacts.AsNoTracking()
                      join absent in context.StaffMembers.AsNoTracking()
                          on impact.AbsentStaffMemberId equals absent.Id
                      join entry in context.TimetableEntries.AsNoTracking()
                          on impact.TimetableEntryId equals entry.Id
                      join section in context.ClassSections.AsNoTracking()
                          on entry.ClassSectionId equals section.Id
                      join offering in context.CourseOfferings.AsNoTracking()
                          on entry.CourseOfferingId equals offering.Id
                      join period in context.TimetablePeriodMasters.AsNoTracking()
                          on entry.TimetablePeriodMasterId equals period.Id
                      join assignment in context.SubstituteAssignments.AsNoTracking()
                          on impact.Id equals assignment.StaffAbsenceImpactId into assignmentRows
                      from assignment in assignmentRows.DefaultIfEmpty()
                      join substitute in context.StaffMembers.AsNoTracking()
                          on assignment.SubstituteStaffMemberId equals substitute.Id into substituteRows
                      from substitute in substituteRows.DefaultIfEmpty()
                      where impact.InstitutionId == institutionId
                      orderby impact.ImpactDate, period.Sequence
                      select new StaffAbsenceImpactResponseDTO
                      {
                          Id = impact.Id,
                          LeaveRequestId = impact.LeaveRequestId,
                          AbsentStaffMemberId = impact.AbsentStaffMemberId,
                          AbsentStaffMemberName = (absent.FirstName + " " + absent.LastName).Trim(),
                          TimetableEntryId = impact.TimetableEntryId,
                          ImpactDate = impact.ImpactDate,
                          SectionName = section.Name,
                          CourseOfferingName = offering.Name,
                          PeriodName = period.Name,
                          StatusCode = impact.StatusCode,
                          SubstituteAssignmentId = assignment == null ? null : assignment.Id,
                          SubstituteStaffMemberId = assignment == null ? null : assignment.SubstituteStaffMemberId,
                          SubstituteStaffMemberName = substitute == null
                              ? null
                              : (substitute.FirstName + " " + substitute.LastName).Trim(),
                          SubstituteStatusCode = assignment == null ? null : assignment.StatusCode
                      }).ToListAsync(token);
    }

    public async Task<List<StaffAbsenceImpactResponseDTO>> SynchronizeLeaveImpactsAsync(
        long leaveRequestId,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var leave = await context.LeaveRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == leaveRequestId
                    && item.InstitutionId == institutionId
                    && item.ApprovedById.HasValue
                    && !item.CancellationDate.HasValue,
                token)
            ?? throw new ValidationErrorException("Only an approved, active leave request can create timetable impacts.");

        var entries = await (from entry in context.TimetableEntries.AsNoTracking()
                             join assignment in context.StaffTeachingAssignments.AsNoTracking()
                                 on entry.StaffTeachingAssignmentId equals assignment.Id
                             where entry.InstitutionId == institutionId
                                && assignment.InstitutionId == institutionId
                                && assignment.StaffMemberId == leave.StaffMemberId
                                && entry.IsActive
                                && !entry.IsSoftDeleted
                                && assignment.IsActive
                                && entry.EffectiveFrom <= leave.ToDate
                                && (entry.EffectiveTo == null || entry.EffectiveTo >= leave.FromDate)
                             select entry).ToListAsync(token);

        var existing = await context.StaffAbsenceImpacts
            .Where(item => item.InstitutionId == institutionId && item.LeaveRequestId == leaveRequestId)
            .ToListAsync(token);
        var existingKeys = existing.Select(item => (item.TimetableEntryId, item.ImpactDate)).ToHashSet();
        var additions = new List<StaffAbsenceImpact>();

        for (var date = leave.FromDate; date <= leave.ToDate; date = date.AddDays(1))
        {
            var weekday = date.DayOfWeek == DayOfWeek.Sunday ? (short)7 : (short)date.DayOfWeek;
            foreach (var entry in entries.Where(item => item.DayOfWeek == weekday
                && item.EffectiveFrom <= date
                && (item.EffectiveTo == null || item.EffectiveTo >= date)))
            {
                if (existingKeys.Contains((entry.Id, date)))
                {
                    continue;
                }

                additions.Add(new StaffAbsenceImpact
                {
                    InstitutionId = institutionId,
                    LeaveRequestId = leave.Id,
                    AbsentStaffMemberId = leave.StaffMemberId,
                    TimetableEntryId = entry.Id,
                    ImpactDate = date,
                    StatusCode = "UNCOVERED",
                    AddedById = actorId,
                    AddedDateTime = DateTime.UtcNow
                });
            }
        }

        context.StaffAbsenceImpacts.AddRange(additions);
        await context.SaveChangesAsync(token);
        return (await GetAbsenceImpactsAsync(institutionId, token))
            .Where(item => item.LeaveRequestId == leaveRequestId)
            .ToList();
    }

    public async Task<StaffAbsenceImpactResponseDTO> AssignSubstituteAsync(
        SubstituteAssignmentRequestDTO request,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var impact = await context.StaffAbsenceImpacts
            .SingleOrDefaultAsync(item => item.Id == request.StaffAbsenceImpactId && item.InstitutionId == institutionId, token)
            ?? throw new KeyNotFoundException("The absence impact was not found.");
        var workflow = await context.SubstituteWorkflowMasters
            .SingleOrDefaultAsync(item => item.Id == request.SubstituteWorkflowMasterId
                && item.InstitutionId == institutionId
                && item.IsActive
                && !item.IsSoftDeleted, token)
            ?? throw new ValidationErrorException("The selected substitute workflow is unavailable.");
        var substituteIsActive = await context.StaffMembers.AnyAsync(
            item => item.Id == request.SubstituteStaffMemberId
                && item.InstitutionId == institutionId
                && item.IsActive
                && !item.IsSoftDeleted,
            token);
        if (!substituteIsActive || request.SubstituteStaffMemberId == impact.AbsentStaffMemberId)
        {
            throw new ValidationErrorException("Select another active StaffMember from this institution as substitute.");
        }

        var impactedEntry = await context.TimetableEntries.AsNoTracking()
            .SingleAsync(item => item.Id == impact.TimetableEntryId && item.InstitutionId == institutionId, token);
        var conflict = await (from entry in context.TimetableEntries.AsNoTracking()
                              join assignment in context.StaffTeachingAssignments.AsNoTracking()
                                  on entry.StaffTeachingAssignmentId equals assignment.Id
                              where entry.InstitutionId == institutionId
                                 && assignment.StaffMemberId == request.SubstituteStaffMemberId
                                 && entry.IsActive
                                 && !entry.IsSoftDeleted
                                 && entry.DayOfWeek == impactedEntry.DayOfWeek
                                 && entry.TimetablePeriodMasterId == impactedEntry.TimetablePeriodMasterId
                                 && entry.EffectiveFrom <= impact.ImpactDate
                                 && (entry.EffectiveTo == null || entry.EffectiveTo >= impact.ImpactDate)
                              select entry.Id).AnyAsync(token);
        if (conflict)
        {
            throw new ConflictException("The selected substitute already has a timetable entry in this period.");
        }

        if (await context.SubstituteAssignments.AnyAsync(
            item => item.StaffAbsenceImpactId == impact.Id && item.InstitutionId == institutionId,
            token))
        {
            throw new ConflictException("A substitute assignment already exists for this absence impact.");
        }

        var firstStep = await context.SubstituteWorkflowSteps
            .Where(item => item.SubstituteWorkflowMasterId == workflow.Id && item.IsActive)
            .OrderBy(item => item.Sequence)
            .Select(item => (short?)item.Sequence)
            .FirstOrDefaultAsync(token);
        var status = firstStep.HasValue
            ? "PENDING_APPROVAL"
            : workflow.RequiresSubstituteAcceptance ? "PENDING_ACCEPTANCE" : "APPROVED";
        var assignmentEntity = new SubstituteAssignment
        {
            InstitutionId = institutionId,
            StaffAbsenceImpactId = impact.Id,
            SubstituteWorkflowMasterId = workflow.Id,
            SubstituteStaffMemberId = request.SubstituteStaffMemberId,
            StatusCode = status,
            CurrentApprovalSequence = firstStep ?? 0,
            Remark = request.Remark?.Trim(),
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
        context.SubstituteAssignments.Add(assignmentEntity);
        impact.StatusCode = status == "APPROVED" ? "COVERED" : "SUBSTITUTE_PENDING";
        impact.UpdatedById = actorId;
        impact.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return (await GetAbsenceImpactsAsync(institutionId, token)).Single(item => item.Id == impact.Id);
    }

    public async Task<StaffAbsenceImpactResponseDTO> ActOnSubstituteAsync(
        long assignmentId,
        SubstituteActionRequestDTO request,
        long institutionId,
        long actorId,
        CancellationToken token)
    {
        var assignment = await context.SubstituteAssignments
            .SingleOrDefaultAsync(item => item.Id == assignmentId && item.InstitutionId == institutionId, token)
            ?? throw new KeyNotFoundException("The substitute assignment was not found.");
        var impact = await context.StaffAbsenceImpacts
            .SingleAsync(item => item.Id == assignment.StaffAbsenceImpactId && item.InstitutionId == institutionId, token);
        var workflow = await context.SubstituteWorkflowMasters
            .SingleAsync(item => item.Id == assignment.SubstituteWorkflowMasterId && item.InstitutionId == institutionId, token);
        var action = request.ActionCode.Trim().ToUpperInvariant();

        if (action is "ACCEPT" or "DECLINE")
        {
            if (assignment.SubstituteStaffMemberId != actorId || assignment.StatusCode != "PENDING_ACCEPTANCE")
            {
                throw new ValidationErrorException("Only the nominated substitute can respond to this assignment.");
            }

            assignment.StatusCode = action == "ACCEPT" ? "ACCEPTED" : "DECLINED";
            assignment.AcceptedDateTime = action == "ACCEPT" ? DateTime.UtcNow : null;
            impact.StatusCode = action == "ACCEPT" ? "COVERED" : "UNCOVERED";
        }
        else if (action is "APPROVE" or "REJECT")
        {
            if (assignment.StatusCode != "PENDING_APPROVAL")
            {
                throw new ValidationErrorException("This assignment is not awaiting approval.");
            }

            var step = await context.SubstituteWorkflowSteps.SingleAsync(
                item => item.SubstituteWorkflowMasterId == workflow.Id
                    && item.Sequence == assignment.CurrentApprovalSequence
                    && item.IsActive,
                token);
            if (!await IsConfiguredApproverAsync(step, impact, actorId, institutionId, token))
            {
                throw new ValidationErrorException("The current StaffMember is not configured for this approval step.");
            }

            if (action == "REJECT")
            {
                assignment.StatusCode = "REJECTED";
                impact.StatusCode = "UNCOVERED";
            }
            else
            {
                var nextStep = await context.SubstituteWorkflowSteps
                    .Where(item => item.SubstituteWorkflowMasterId == workflow.Id
                        && item.Sequence > step.Sequence
                        && item.IsActive)
                    .OrderBy(item => item.Sequence)
                    .Select(item => (short?)item.Sequence)
                    .FirstOrDefaultAsync(token);
                if (nextStep.HasValue)
                {
                    assignment.CurrentApprovalSequence = nextStep.Value;
                }
                else
                {
                    assignment.StatusCode = workflow.RequiresSubstituteAcceptance
                        ? "PENDING_ACCEPTANCE"
                        : "APPROVED";
                    impact.StatusCode = workflow.RequiresSubstituteAcceptance
                        ? "SUBSTITUTE_PENDING"
                        : "COVERED";
                }
            }
        }
        else
        {
            throw new ValidationErrorException("Action must be APPROVE, REJECT, ACCEPT or DECLINE.");
        }

        context.SubstituteApprovalHistories.Add(new SubstituteApprovalHistory
        {
            InstitutionId = institutionId,
            SubstituteAssignmentId = assignment.Id,
            Sequence = assignment.CurrentApprovalSequence,
            ActionByStaffMemberId = actorId,
            ActionCode = action,
            Remark = request.Remark?.Trim(),
            ActionDateTime = DateTime.UtcNow
        });
        assignment.UpdatedById = actorId;
        assignment.UpdatedDateTime = DateTime.UtcNow;
        impact.UpdatedById = actorId;
        impact.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return (await GetAbsenceImpactsAsync(institutionId, token)).Single(item => item.Id == impact.Id);
    }

    private async Task<bool> IsConfiguredApproverAsync(
        SubstituteWorkflowStep step,
        StaffAbsenceImpact impact,
        long actorId,
        long institutionId,
        CancellationToken token)
    {
        return step.ApproverTypeCode switch
        {
            "STAFF_MEMBER" => step.StaffMemberId == actorId,
            "POSITION" => await context.StaffPositionAssignments.AnyAsync(
                item => item.InstitutionId == institutionId
                    && item.StaffMemberId == actorId
                    && item.StaffPositionMasterId == step.StaffPositionMasterId
                    && item.IsActive,
                token),
            "ACCESS_ROLE" => await context.UserRoles.AnyAsync(
                item => item.StaffMemberId == actorId
                    && item.RoleId == step.AccessRoleId
                    && item.IsActive
                    && item.IsSoftDeleted != true,
                token),
            "MANAGER" => await context.StaffMemberManagerMappings.AnyAsync(
                item => item.InstitutionId == institutionId
                    && item.StaffMemberId == impact.AbsentStaffMemberId
                    && item.ManagerId == actorId
                    && item.IsActive
                    && item.IsSoftDeleted != true
                    && item.EffectiveFrom.Date <= impact.ImpactDate.ToDateTime(TimeOnly.MinValue).Date
                    && (!item.EffectiveTo.HasValue
                        || item.EffectiveTo.Value.Date >= impact.ImpactDate.ToDateTime(TimeOnly.MinValue).Date),
                token),
            _ => false
        };
    }
}
