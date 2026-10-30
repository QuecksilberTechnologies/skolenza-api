using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.StudentPortalAuthCmd;

public sealed record StudentPortalLoginCommand(
    string Hostname,
    StudentPortalLoginRequestDTO Request) : IRequest<ApiResponse<StudentPortalSessionResponseDTO>>;

public sealed record StudentPortalRefreshCommand(
    string Hostname,
    StudentPortalRefreshRequestDTO Request) : IRequest<ApiResponse<StudentPortalSessionResponseDTO>>;

public sealed record StudentPortalRevokeCommand(
    long InstitutionId,
    long AccountId,
    StudentPortalRefreshRequestDTO Request) : IRequest<ApiResponse<StudentPortalSessionRevokedResponseDTO>>;

public sealed record GetStudentPortalProfileQuery(
    long InstitutionId,
    long AccountId) : IRequest<ApiResponse<StudentPortalProfileResponseDTO>>;

public sealed record AcceptStudentPortalInvitationCommand(
    string Hostname,
    StudentPortalInvitationAcceptanceRequestDTO Request,
    long? AuthenticatedAccountId) : IRequest<ApiResponse<StudentPortalInvitationAcceptedResponseDTO>>;

public sealed record RequestStudentPortalPasswordResetCommand(
    string Hostname,
    StudentPortalPasswordResetRequestDTO Request) : IRequest<ApiResponse<StudentPortalPasswordResetResponseDTO>>;

public sealed record CompleteStudentPortalPasswordResetCommand(
    string Hostname,
    StudentPortalPasswordResetCompletionRequestDTO Request) : IRequest<ApiResponse<StudentPortalPasswordResetResponseDTO>>;

public sealed class RequestStudentPortalPasswordResetCommandHandler(
    IStudentPortalAuthRepository repository)
    : IRequestHandler<RequestStudentPortalPasswordResetCommand, ApiResponse<StudentPortalPasswordResetResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalPasswordResetResponseDTO>> Handle(
        RequestStudentPortalPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.Email) ||
            command.Request.Email.Length > 254 || !command.Request.Email.Contains('@'))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var institutionId = await repository.ResolveVerifiedInstitutionAsync(
            InstitutionPortalDomainChallenge.NormalizeDomain(command.Hostname), cancellationToken);
        if (institutionId.HasValue)
        {
            await repository.QueuePasswordResetAsync(
                institutionId.Value,
                command.Request.Email.Trim().ToUpperInvariant(),
                DateTime.UtcNow,
                cancellationToken);
        }

        return ApiResponse<StudentPortalPasswordResetResponseDTO>.Success(
            new StudentPortalPasswordResetResponseDTO
            {
                Message = StudentConstants.PortalPasswordResetGenericMessage
            },
            StudentConstants.PortalPasswordResetGenericMessage);
    }
}

public sealed class CompleteStudentPortalPasswordResetCommandHandler(
    IStudentPortalAuthRepository repository,
    IPasswordService passwordService)
    : IRequestHandler<CompleteStudentPortalPasswordResetCommand, ApiResponse<StudentPortalPasswordResetResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalPasswordResetResponseDTO>> Handle(
        CompleteStudentPortalPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.Token) ||
            command.Request.Token.Length > 200 || string.IsNullOrWhiteSpace(command.Request.NewPassword) ||
            command.Request.NewPassword.Length < StudentConstants.PortalPasswordMinimumLength ||
            command.Request.NewPassword.Length > StudentConstants.PortalPasswordMaximumLength)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var institutionId = await repository.ResolveVerifiedInstitutionAsync(
            InstitutionPortalDomainChallenge.NormalizeDomain(command.Hostname), cancellationToken);
        if (!institutionId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        await repository.ResetPasswordAsync(
            institutionId.Value,
            StudentPortalTokenHelper.HashToken(command.Request.Token),
            passwordService.HashPassword(command.Request.NewPassword),
            DateTime.UtcNow,
            cancellationToken);
        return ApiResponse<StudentPortalPasswordResetResponseDTO>.Success(
            new StudentPortalPasswordResetResponseDTO { Message = "Portal password reset succeeded." },
            "Portal password reset succeeded. Sign in with the new password.");
    }
}

public sealed class AcceptStudentPortalInvitationCommandHandler(
    IStudentPortalAuthRepository repository,
    IPasswordService passwordService)
    : IRequestHandler<AcceptStudentPortalInvitationCommand, ApiResponse<StudentPortalInvitationAcceptedResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalInvitationAcceptedResponseDTO>> Handle(
        AcceptStudentPortalInvitationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.Token) ||
            command.Request.Token.Length > 200)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var institutionId = await repository.ResolveVerifiedInstitutionAsync(
            InstitutionPortalDomainChallenge.NormalizeDomain(command.Hostname), cancellationToken);
        if (!institutionId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var password = command.Request.Password;
        if (!command.AuthenticatedAccountId.HasValue &&
            (string.IsNullOrWhiteSpace(password) ||
             password.Length < StudentConstants.PortalPasswordMinimumLength ||
             password.Length > StudentConstants.PortalPasswordMaximumLength))
        {
            throw new ValidationErrorException(
                $"Password must be between {StudentConstants.PortalPasswordMinimumLength} and {StudentConstants.PortalPasswordMaximumLength} characters.");
        }

        var account = await repository.AcceptGuardianInvitationAsync(
            institutionId.Value,
            StudentPortalTokenHelper.HashToken(command.Request.Token),
            command.AuthenticatedAccountId.HasValue ? string.Empty : passwordService.HashPassword(password!),
            command.AuthenticatedAccountId,
            DateTime.UtcNow,
            cancellationToken);
        return ApiResponse<StudentPortalInvitationAcceptedResponseDTO>.Success(
            new StudentPortalInvitationAcceptedResponseDTO
            {
                PortalAccountId = account.Id,
                Email = account.NormalizedEmail,
                RequiresLogin = true
            },
            "Guardian portal invitation accepted. Sign in to continue.");
    }
}

public sealed class GetStudentPortalProfileQueryHandler(IStudentPortalAuthRepository repository)
    : IRequestHandler<GetStudentPortalProfileQuery, ApiResponse<StudentPortalProfileResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalProfileResponseDTO>> Handle(
        GetStudentPortalProfileQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(
            query.InstitutionId, query.AccountId, cancellationToken);
        if (profile is null)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        return ApiResponse<StudentPortalProfileResponseDTO>.Success(
            profile, "Student portal profile retrieved successfully.");
    }
}

public sealed class StudentPortalLoginCommandHandler(
    IStudentPortalAuthRepository repository,
    IPasswordService passwordService,
    ITokenService tokenService)
    : IRequestHandler<StudentPortalLoginCommand, ApiResponse<StudentPortalSessionResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalSessionResponseDTO>> Handle(
        StudentPortalLoginCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.Email) ||
            string.IsNullOrWhiteSpace(command.Request.Password) || command.Request.Email.Length > 254)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var domain = InstitutionPortalDomainChallenge.NormalizeDomain(command.Hostname);
        var institutionId = await repository.ResolveVerifiedInstitutionAsync(domain, cancellationToken);
        if (institutionId is null)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var normalizedEmail = command.Request.Email.Trim().ToUpperInvariant();
        var account = await repository.GetAccountForLoginAsync(institutionId.Value, normalizedEmail, cancellationToken);
        var now = DateTime.UtcNow;
        if (account is null || !account.IsActive || !account.IsEmailVerified ||
            account.LockoutEndDateTime > now || !IsIdentityPasswordHash(account.PasswordHash) ||
            !passwordService.VerifyPassword(account.PasswordHash, command.Request.Password))
        {
            if (account is not null && account.IsActive && account.IsEmailVerified &&
                (!account.LockoutEndDateTime.HasValue || account.LockoutEndDateTime <= now))
            {
                await repository.RegisterFailedLoginAsync(
                    institutionId.Value,
                    account.Id,
                    StudentConstants.PortalLoginFailureLimit,
                    now.AddMinutes(StudentConstants.PortalLoginLockoutMinutes),
                    cancellationToken);
            }

            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        if (!await repository.HasActivePersonaAsync(institutionId.Value, account.Id, cancellationToken))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var rawRefreshToken = StudentPortalTokenHelper.CreateOpaqueToken();
        var refreshExpiry = now.AddDays(StudentConstants.PortalRefreshTokenExpiryDays);
        var accessToken = await tokenService.GenerateStudentPortalToken(new StudentPortalTokenInfoDTO
        {
            AccountId = account.Id,
            InstitutionId = institutionId.Value,
            NormalizedEmail = account.NormalizedEmail
        });
        var accessExpiry = tokenService.GetExpiryFromToken(accessToken)
            ?? throw new ApiException(AppConstants.ErrorMessages.InternalServerError, 500);

        await repository.AddRefreshTokenAsync(new StudentPortalRefreshToken
        {
            InstitutionId = institutionId.Value,
            StudentPortalAccountId = account.Id,
            TokenHash = StudentPortalTokenHelper.HashToken(rawRefreshToken),
            CreatedDateTime = now,
            ExpiresDateTime = refreshExpiry
        }, cancellationToken);
        await repository.ResetLoginFailuresAsync(institutionId.Value, account.Id, now, cancellationToken);

        return ApiResponse<StudentPortalSessionResponseDTO>.Success(
            CreateSession(account, institutionId.Value, accessToken, rawRefreshToken, accessExpiry, refreshExpiry),
            "Student portal login succeeded.");
    }

    internal static bool IsIdentityPasswordHash(string? hash) =>
        !string.IsNullOrWhiteSpace(hash) && hash.StartsWith("AQAAAA", StringComparison.Ordinal);

    internal static StudentPortalSessionResponseDTO CreateSession(
        StudentPortalAccount account,
        long institutionId,
        string accessToken,
        string refreshToken,
        DateTime accessExpiry,
        DateTime refreshExpiry) => new()
    {
        UserType = StudentConstants.PortalUserType,
        InstitutionId = institutionId,
        PortalAccountId = account.Id,
        Email = account.NormalizedEmail,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiresAtUtc = accessExpiry,
        RefreshTokenExpiresAtUtc = refreshExpiry
    };
}

public sealed class StudentPortalRefreshCommandHandler(
    IStudentPortalAuthRepository repository,
    ITokenService tokenService)
    : IRequestHandler<StudentPortalRefreshCommand, ApiResponse<StudentPortalSessionResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalSessionResponseDTO>> Handle(
        StudentPortalRefreshCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.RefreshToken))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var domain = InstitutionPortalDomainChallenge.NormalizeDomain(command.Hostname);
        var institutionId = await repository.ResolveVerifiedInstitutionAsync(domain, cancellationToken);
        var current = await repository.GetRefreshTokenAsync(
            StudentPortalTokenHelper.HashToken(command.Request.RefreshToken), cancellationToken);
        var now = DateTime.UtcNow;
        if (institutionId is null || current is null || current.InstitutionId != institutionId ||
            current.RevokedDateTime.HasValue || current.ExpiresDateTime <= now)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var account = await repository.GetAccountByIdAsync(institutionId.Value, current.StudentPortalAccountId, cancellationToken);
        if (account is null || !account.IsActive || !account.IsEmailVerified ||
            !await repository.HasActivePersonaAsync(institutionId.Value, account.Id, cancellationToken))
        {
            await repository.RevokeRefreshTokenAsync(current, cancellationToken);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var rawReplacement = StudentPortalTokenHelper.CreateOpaqueToken();
        var replacementHash = StudentPortalTokenHelper.HashToken(rawReplacement);
        var replacementExpiry = now.AddDays(StudentConstants.PortalRefreshTokenExpiryDays);
        var accessToken = await tokenService.GenerateStudentPortalToken(new StudentPortalTokenInfoDTO
        {
            AccountId = account.Id,
            InstitutionId = institutionId.Value,
            NormalizedEmail = account.NormalizedEmail
        });
        var accessExpiry = tokenService.GetExpiryFromToken(accessToken)
            ?? throw new ApiException(AppConstants.ErrorMessages.InternalServerError, 500);
        var replacement = new StudentPortalRefreshToken
        {
            InstitutionId = institutionId.Value,
            StudentPortalAccountId = account.Id,
            TokenHash = replacementHash,
            CreatedDateTime = now,
            ExpiresDateTime = replacementExpiry
        };

        if (!await repository.RotateRefreshTokenAsync(current, replacement, cancellationToken))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        return ApiResponse<StudentPortalSessionResponseDTO>.Success(
            StudentPortalLoginCommandHandler.CreateSession(
                account, institutionId.Value, accessToken, rawReplacement, accessExpiry, replacementExpiry),
            "Student portal session refreshed.");
    }
}

public sealed class StudentPortalRevokeCommandHandler(IStudentPortalAuthRepository repository)
    : IRequestHandler<StudentPortalRevokeCommand, ApiResponse<StudentPortalSessionRevokedResponseDTO>>
{
    public async Task<ApiResponse<StudentPortalSessionRevokedResponseDTO>> Handle(
        StudentPortalRevokeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request?.RefreshToken))
        {
            return ApiResponse<StudentPortalSessionRevokedResponseDTO>.Success(
                new StudentPortalSessionRevokedResponseDTO { Revoked = false },
                "No active portal session was found.");
        }

        var token = await repository.GetRefreshTokenAsync(
            StudentPortalTokenHelper.HashToken(command.Request.RefreshToken), cancellationToken);
        if (token is not null && token.InstitutionId == command.InstitutionId &&
            token.StudentPortalAccountId == command.AccountId && !token.RevokedDateTime.HasValue)
        {
            await repository.RevokeRefreshTokenAsync(token, cancellationToken);
        }

        return ApiResponse<StudentPortalSessionRevokedResponseDTO>.Success(
            new StudentPortalSessionRevokedResponseDTO { Revoked = token is not null },
            "Student portal session revoked.");
    }
}
