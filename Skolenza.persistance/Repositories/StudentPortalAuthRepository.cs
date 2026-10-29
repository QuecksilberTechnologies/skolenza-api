using Microsoft.EntityFrameworkCore;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

public sealed class StudentPortalAuthRepository(
    SkolenzaDbContext context,
    IEmailQueueRepository emailQueueRepository) : IStudentPortalAuthRepository
{
    public Task<long?> ResolveVerifiedInstitutionAsync(string hostname, CancellationToken cancellationToken) =>
        context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.DomainName == hostname && item.IsActive && !item.IsSoftDeleted &&
                item.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Verified)
            .Select(item => (long?)item.InstitutionId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<StudentPortalAccount?> GetAccountForLoginAsync(
        long institutionId,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        context.StudentPortalAccounts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
                item.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<StudentPortalAccount?> GetAccountByIdAsync(
        long institutionId,
        long accountId,
        CancellationToken cancellationToken) =>
        context.StudentPortalAccounts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId && item.Id == accountId, cancellationToken);

    public async Task<bool> HasActivePersonaAsync(long institutionId, long accountId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var guardianPersona =
            from accountGuardian in context.StudentPortalAccountGuardians.AsNoTracking()
            join guardian in context.Guardians.AsNoTracking()
                on accountGuardian.GuardianId equals guardian.Id
            join link in context.StudentGuardians.AsNoTracking()
                on guardian.Id equals link.GuardianId
            join student in context.Students.AsNoTracking()
                on link.StudentId equals student.Id
            where accountGuardian.InstitutionId == institutionId &&
                accountGuardian.StudentPortalAccountId == accountId && accountGuardian.IsActive &&
                guardian.InstitutionId == institutionId && guardian.IsActive && !guardian.IsSoftDeleted &&
                link.InstitutionId == institutionId && link.IsActive && link.HasPortalAccess &&
                link.EffectiveFrom <= today && (!link.EffectiveTo.HasValue || link.EffectiveTo.Value >= today) &&
                student.InstitutionId == institutionId && student.IsActive && !student.IsSoftDeleted
            select link.Id;

        var studentPersona =
            from accountStudent in context.StudentPortalAccountStudents.AsNoTracking()
            join student in context.Students.AsNoTracking()
                on accountStudent.StudentId equals student.Id
            where accountStudent.InstitutionId == institutionId &&
                accountStudent.StudentPortalAccountId == accountId && accountStudent.IsActive &&
                student.InstitutionId == institutionId && student.IsActive && !student.IsSoftDeleted
            select student.Id;

        if (await guardianPersona.AnyAsync(cancellationToken))
        {
            return true;
        }

        return await studentPersona.AnyAsync(cancellationToken);
    }

    public async Task<StudentPortalProfileResponseDTO?> GetProfileAsync(
        long institutionId,
        long accountId,
        CancellationToken cancellationToken)
    {
        var account = await context.StudentPortalAccounts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
                item.Id == accountId && item.IsActive && item.IsEmailVerified, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var guardians = await (
            from accountGuardian in context.StudentPortalAccountGuardians.AsNoTracking()
            join guardian in context.Guardians.AsNoTracking()
                on accountGuardian.GuardianId equals guardian.Id
            where accountGuardian.InstitutionId == institutionId &&
                accountGuardian.StudentPortalAccountId == accountId && accountGuardian.IsActive &&
                guardian.InstitutionId == institutionId && guardian.IsActive && !guardian.IsSoftDeleted
            orderby guardian.Id
            select new { guardian.Id, guardian.FirstName, guardian.MiddleName, guardian.LastName })
            .ToListAsync(cancellationToken);

        var guardianPersonas = new List<StudentPortalPersonaResponseDTO>();
        foreach (var guardian in guardians)
        {
            var linkedStudents = await (
                from link in context.StudentGuardians.AsNoTracking()
                join student in context.Students.AsNoTracking()
                    on link.StudentId equals student.Id
                where link.InstitutionId == institutionId && link.GuardianId == guardian.Id &&
                    link.IsActive && link.HasPortalAccess && link.EffectiveFrom <= today &&
                    (!link.EffectiveTo.HasValue || link.EffectiveTo.Value >= today) &&
                    student.InstitutionId == institutionId && student.IsActive && !student.IsSoftDeleted
                orderby student.StudentCode, student.Id
                select new StudentPortalLinkedStudentResponseDTO
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    DisplayName = (student.FirstName + " " + student.MiddleName + " " + student.LastName).Trim()
                })
                .ToListAsync(cancellationToken);

            if (linkedStudents.Count == 0)
            {
                continue;
            }

            guardianPersonas.Add(new StudentPortalPersonaResponseDTO
            {
                Type = StudentPortalPersonaType.Guardian,
                Id = guardian.Id,
                DisplayName = string.Join(" ", new[] { guardian.FirstName, guardian.MiddleName, guardian.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                LinkedStudents = linkedStudents
            });
        }

        var students = await (
            from accountStudent in context.StudentPortalAccountStudents.AsNoTracking()
            join student in context.Students.AsNoTracking()
                on accountStudent.StudentId equals student.Id
            where accountStudent.InstitutionId == institutionId &&
                accountStudent.StudentPortalAccountId == accountId && accountStudent.IsActive &&
                student.InstitutionId == institutionId && student.IsActive && !student.IsSoftDeleted
            orderby student.StudentCode, student.Id
            select new StudentPortalPersonaResponseDTO
            {
                Type = StudentPortalPersonaType.Student,
                Id = student.Id,
                StudentCode = student.StudentCode,
                DisplayName = (student.FirstName + " " + student.MiddleName + " " + student.LastName).Trim()
            })
            .ToListAsync(cancellationToken);

        var personas = guardianPersonas.Concat(students).ToArray();
        if (personas.Length == 0)
        {
            return null;
        }

        return new StudentPortalProfileResponseDTO
        {
            PortalAccountId = account.Id,
            InstitutionId = account.InstitutionId,
            Email = account.NormalizedEmail,
            Personas = personas
        };
    }

    public async Task QueueGuardianInvitationAsync(
        long institutionId,
        long guardianId,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken)
    {
        var guardian = await context.Guardians.SingleOrDefaultAsync(item =>
                item.InstitutionId == institutionId && item.Id == guardianId &&
                item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("The primary Guardian was not found.");
        if (string.IsNullOrWhiteSpace(guardian.Email))
        {
            throw new ConflictException("The primary Guardian must have an email address before admission approval.");
        }

        var domain = await context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted &&
                item.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Verified)
            .Select(item => item.DomainName)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ConflictException("Verify an institution portal domain before approving an admission that sends a Guardian invitation.");
        }

        var rawToken = StudentPortalTokenHelper.CreateOpaqueToken();
        var expiresAtUtc = issuedAtUtc.AddMinutes(StudentConstants.PortalOneTimeTokenExpiryMinutes);
        context.StudentPortalOneTimeTokens.Add(new StudentPortalOneTimeToken
        {
            InstitutionId = institutionId,
            GuardianId = guardianId,
            Purpose = (short)StudentPortalOneTimeTokenPurpose.Invitation,
            TokenHash = StudentPortalTokenHelper.HashToken(rawToken),
            CreatedDateTime = issuedAtUtc,
            ExpiresDateTime = expiresAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);

        var invitationUrl = $"https://{domain}{StudentConstants.PortalInvitationPath}?token={Uri.EscapeDataString(rawToken)}";
        var placeholdersJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["GuardianName"] = string.Join(" ", new[] { guardian.FirstName, guardian.MiddleName, guardian.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            ["GuardianEmail"] = guardian.Email.Trim(),
            ["InvitationUrl"] = invitationUrl,
            ["PortalDomain"] = domain,
            ["ExpiresAtUtc"] = expiresAtUtc.ToString("O")
        });

        var queuedEmailId = await emailQueueRepository.EnqueueAsync(
            StudentConstants.PortalInvitationTemplateCode,
            guardian.Email,
            institutionId,
            placeholdersJson,
            cancellationToken);
        if (!queuedEmailId.HasValue)
        {
            throw new ConflictException(
                $"Configure an active {StudentConstants.PortalInvitationTemplateCode} email template before approving an admission.");
        }
    }

    public async Task<StudentPortalAccount> AcceptGuardianInvitationAsync(
        long institutionId,
        string tokenHash,
        string passwordHash,
        long? authenticatedAccountId,
        DateTime acceptedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var invitation = await context.StudentPortalOneTimeTokens
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentPortalOneTimeToken\" WHERE \"TokenHash\" = {tokenHash} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (invitation is null || invitation.Purpose != (short)StudentPortalOneTimeTokenPurpose.Invitation ||
            invitation.ConsumedDateTime.HasValue || invitation.ExpiresDateTime <= acceptedAtUtc ||
            !invitation.GuardianId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var guardian = await context.Guardians.SingleOrDefaultAsync(item =>
                item.InstitutionId == institutionId && item.Id == invitation.GuardianId.Value &&
                item.IsActive && !item.IsSoftDeleted && !string.IsNullOrWhiteSpace(item.Email),
            cancellationToken) ?? throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        var hasActivePortalStudent = await (
            from link in context.StudentGuardians.AsNoTracking()
            join student in context.Students.AsNoTracking() on link.StudentId equals student.Id
            where link.InstitutionId == institutionId && link.GuardianId == guardian.Id &&
                link.IsActive && link.HasPortalAccess &&
                link.EffectiveFrom <= DateOnly.FromDateTime(acceptedAtUtc) &&
                (!link.EffectiveTo.HasValue || link.EffectiveTo.Value >= DateOnly.FromDateTime(acceptedAtUtc)) &&
                student.InstitutionId == institutionId && student.IsActive && !student.IsSoftDeleted
            select link.Id).AnyAsync(cancellationToken);
        if (!hasActivePortalStudent)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var normalizedEmail = guardian.Email!.Trim().ToUpperInvariant();
        var account = await context.StudentPortalAccounts.SingleOrDefaultAsync(item =>
            item.InstitutionId == institutionId && item.NormalizedEmail == normalizedEmail,
            cancellationToken);
        if (account is null)
        {
            if (authenticatedAccountId.HasValue || string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            account = new StudentPortalAccount
            {
                InstitutionId = institutionId,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHash,
                IsEmailVerified = true,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                AddedDateTime = acceptedAtUtc
            };
            context.StudentPortalAccounts.Add(account);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (!authenticatedAccountId.HasValue || account.Id != authenticatedAccountId.Value ||
                 !account.IsActive || !account.IsEmailVerified)
        {
            throw new ConflictException("This email already has a portal account. Sign in to that account to accept the Guardian invitation.");
        }

        var existingGuardianAccount = await context.StudentPortalAccountGuardians
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
                item.GuardianId == guardian.Id, cancellationToken);
        if (existingGuardianAccount is not null && existingGuardianAccount.StudentPortalAccountId != account.Id)
        {
            throw new ConflictException("This Guardian is already linked to another portal account.");
        }

        if (existingGuardianAccount is null)
        {
            context.StudentPortalAccountGuardians.Add(new StudentPortalAccountGuardian
            {
                InstitutionId = institutionId,
                StudentPortalAccountId = account.Id,
                GuardianId = guardian.Id,
                LinkedDateTime = acceptedAtUtc,
                IsActive = true
            });
        }
        else
        {
            existingGuardianAccount.IsActive = true;
            existingGuardianAccount.LinkedDateTime = acceptedAtUtc;
        }

        invitation.ConsumedDateTime = acceptedAtUtc;
        account.UpdatedDateTime = acceptedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return account;
    }

    public async Task QueuePasswordResetAsync(
        long institutionId,
        string normalizedEmail,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var account = await context.StudentPortalAccounts.SingleOrDefaultAsync(item =>
            item.InstitutionId == institutionId && item.NormalizedEmail == normalizedEmail &&
            item.IsActive && item.IsEmailVerified, cancellationToken);
        if (account is null || !await HasActivePersonaAsync(institutionId, account.Id, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var domain = await context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted &&
                item.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Verified)
            .Select(item => item.DomainName)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(domain))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var rawToken = StudentPortalTokenHelper.CreateOpaqueToken();
        var expiresAtUtc = issuedAtUtc.AddMinutes(StudentConstants.PortalOneTimeTokenExpiryMinutes);
        context.StudentPortalOneTimeTokens.Add(new StudentPortalOneTimeToken
        {
            InstitutionId = institutionId,
            StudentPortalAccountId = account.Id,
            Purpose = (short)StudentPortalOneTimeTokenPurpose.PasswordReset,
            TokenHash = StudentPortalTokenHelper.HashToken(rawToken),
            CreatedDateTime = issuedAtUtc,
            ExpiresDateTime = expiresAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);

        var resetUrl = $"https://{domain}{StudentConstants.PortalPasswordResetPath}?token={Uri.EscapeDataString(rawToken)}";
        var queuedEmailId = await emailQueueRepository.EnqueueAsync(
            StudentConstants.PortalPasswordResetTemplateCode,
            normalizedEmail,
            institutionId,
            System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["Email"] = normalizedEmail,
                ["PasswordResetUrl"] = resetUrl,
                ["PortalDomain"] = domain,
                ["ExpiresAtUtc"] = expiresAtUtc.ToString("O")
            }),
            cancellationToken);
        if (!queuedEmailId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        long institutionId,
        string tokenHash,
        string passwordHash,
        DateTime resetAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var resetToken = await context.StudentPortalOneTimeTokens
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentPortalOneTimeToken\" WHERE \"TokenHash\" = {tokenHash} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (resetToken is null || resetToken.Purpose != (short)StudentPortalOneTimeTokenPurpose.PasswordReset ||
            resetToken.ConsumedDateTime.HasValue || resetToken.ExpiresDateTime <= resetAtUtc ||
            !resetToken.StudentPortalAccountId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var account = await context.StudentPortalAccounts
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentPortalAccount\" WHERE \"Id\" = {resetToken.StudentPortalAccountId.Value} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || !account.IsActive || !account.IsEmailVerified ||
            !await HasActivePersonaAsync(institutionId, account.Id, cancellationToken))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        account.PasswordHash = passwordHash;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.FailedLoginCount = 0;
        account.LockoutEndDateTime = null;
        account.UpdatedDateTime = resetAtUtc;
        resetToken.ConsumedDateTime = resetAtUtc;
        await context.StudentPortalRefreshTokens
            .Where(item => item.InstitutionId == institutionId &&
                item.StudentPortalAccountId == account.Id && !item.RevokedDateTime.HasValue)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedDateTime, resetAtUtc), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RegisterFailedLoginAsync(
        long institutionId,
        long accountId,
        short failureLimit,
        DateTime lockoutUntilUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var account = await context.StudentPortalAccounts
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentPortalAccount\" WHERE \"Id\" = {accountId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        account.FailedLoginCount = (short)Math.Min(short.MaxValue, account.FailedLoginCount + 1);
        if (account.FailedLoginCount >= failureLimit)
        {
            account.LockoutEndDateTime = lockoutUntilUtc;
            account.FailedLoginCount = 0;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetLoginFailuresAsync(
        long institutionId,
        long accountId,
        DateTime loginUtc,
        CancellationToken cancellationToken)
    {
        await context.StudentPortalAccounts
            .Where(item => item.InstitutionId == institutionId && item.Id == accountId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.FailedLoginCount, (short)0)
                .SetProperty(item => item.LockoutEndDateTime, (DateTime?)null)
                .SetProperty(item => item.LastLoginDateTime, loginUtc), cancellationToken);
    }

    public async Task AddRefreshTokenAsync(StudentPortalRefreshToken token, CancellationToken cancellationToken)
    {
        context.StudentPortalRefreshTokens.Add(token);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<StudentPortalRefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.StudentPortalRefreshTokens.SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> RotateRefreshTokenAsync(
        StudentPortalRefreshToken current,
        StudentPortalRefreshToken replacement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await context.StudentPortalRefreshTokens
            .Where(item => item.Id == current.Id && item.InstitutionId == current.InstitutionId &&
                item.StudentPortalAccountId == current.StudentPortalAccountId &&
                item.RevokedDateTime == null && item.ExpiresDateTime > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.RevokedDateTime, now)
                .SetProperty(item => item.ReplacedByTokenHash, replacement.TokenHash), cancellationToken);
        if (updated != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        context.StudentPortalRefreshTokens.Add(replacement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeRefreshTokenAsync(
        StudentPortalRefreshToken token,
        CancellationToken cancellationToken)
    {
        await context.StudentPortalRefreshTokens
            .Where(item => item.Id == token.Id && item.RevokedDateTime == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedDateTime, DateTime.UtcNow), cancellationToken);
    }
}
