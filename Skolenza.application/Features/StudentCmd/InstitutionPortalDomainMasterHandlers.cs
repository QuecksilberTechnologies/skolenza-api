using MediatR;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetInstitutionPortalDomainsQuery(InstitutionPortalDomainFilterDTO DTO)
    : IRequest<ApiResponse<List<InstitutionPortalDomainResponseDTO>>>;

public sealed record CreateInstitutionPortalDomainCommand(InstitutionPortalDomainCreateDTO DTO)
    : IRequest<ApiResponse<InstitutionPortalDomainChallengeResponseDTO>>;

public sealed record RenewInstitutionPortalDomainChallengeCommand(long Id, InstitutionPortalDomainVerifyDTO DTO)
    : IRequest<ApiResponse<InstitutionPortalDomainChallengeResponseDTO>>;

public sealed record VerifyInstitutionPortalDomainCommand(long Id, InstitutionPortalDomainVerifyDTO DTO)
    : IRequest<ApiResponse<InstitutionPortalDomainResponseDTO>>;

public sealed record DeleteInstitutionPortalDomainCommand(long Id, InstitutionPortalDomainDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetInstitutionPortalDomainsQueryHandler(
    IInstitutionPortalDomainMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetInstitutionPortalDomainsQuery, ApiResponse<List<InstitutionPortalDomainResponseDTO>>>
{
    public async Task<ApiResponse<List<InstitutionPortalDomainResponseDTO>>> Handle(
        GetInstitutionPortalDomainsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetPageAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<InstitutionPortalDomainResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Institution portal domains retrieved successfully.");
    }
}

public sealed class CreateInstitutionPortalDomainCommandHandler(
    IInstitutionPortalDomainMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateInstitutionPortalDomainCommand, ApiResponse<InstitutionPortalDomainChallengeResponseDTO>>
{
    public async Task<ApiResponse<InstitutionPortalDomainChallengeResponseDTO>> Handle(
        CreateInstitutionPortalDomainCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var domainName = InstitutionPortalDomainChallenge.NormalizeDomain(request.DTO.DomainName);
        var challengeValue = InstitutionPortalDomainChallenge.CreateChallengeValue();
        var expiresAt = DateTime.UtcNow.AddHours(StudentConstants.PortalDomainChallengeExpiryHours);
        var saved = await repository.CreateAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            domainName,
            InstitutionPortalDomainChallenge.HashChallenge(challengeValue),
            expiresAt,
            cancellationToken);
        return ApiResponse<InstitutionPortalDomainChallengeResponseDTO>.Success(
            new InstitutionPortalDomainChallengeResponseDTO
            {
                Domain = saved,
                DnsTxtRecordName = InstitutionPortalDomainChallenge.CreateRecordName(domainName),
                DnsTxtRecordValue = challengeValue
            },
            "Portal domain added. Publish the returned TXT record, then request verification.");
    }
}

public sealed class RenewInstitutionPortalDomainChallengeCommandHandler(
    IInstitutionPortalDomainMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<RenewInstitutionPortalDomainChallengeCommand, ApiResponse<InstitutionPortalDomainChallengeResponseDTO>>
{
    public async Task<ApiResponse<InstitutionPortalDomainChallengeResponseDTO>> Handle(
        RenewInstitutionPortalDomainChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var challengeValue = InstitutionPortalDomainChallenge.CreateChallengeValue();
        var response = await repository.RenewChallengeAsync(
            request.Id,
            actor.InstitutionId,
            actor.StaffMemberId,
            InstitutionPortalDomainChallenge.HashChallenge(challengeValue),
            DateTime.UtcNow.AddHours(StudentConstants.PortalDomainChallengeExpiryHours),
            challengeValue,
            cancellationToken);
        return ApiResponse<InstitutionPortalDomainChallengeResponseDTO>.Success(
            response, "A new portal-domain TXT challenge was generated.");
    }
}

public sealed class VerifyInstitutionPortalDomainCommandHandler(
    IInstitutionPortalDomainMasterRepository repository,
    ICommonRequestService commonRequestService,
    IDnsTxtResolver dnsTxtResolver)
    : IRequestHandler<VerifyInstitutionPortalDomainCommand, ApiResponse<InstitutionPortalDomainResponseDTO>>
{
    public async Task<ApiResponse<InstitutionPortalDomainResponseDTO>> Handle(
        VerifyInstitutionPortalDomainCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var verification = await repository.GetVerificationDataAsync(
            request.Id, actor.InstitutionId, cancellationToken);

        if (verification.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Verified)
        {
            throw new ConflictException("The portal domain is already verified.");
        }

        var now = DateTime.UtcNow;
        if (verification.ChallengeExpiresAt <= now)
        {
            throw new ConflictException("The portal-domain TXT challenge expired. Generate a new challenge and publish it.");
        }

        IReadOnlyList<string> records;
        try
        {
            records = await dnsTxtResolver.ResolveAsync(
                InstitutionPortalDomainChallenge.CreateRecordName(verification.DomainName), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            throw new ApiException("DNS verification is temporarily unavailable. Please retry shortly.", 503);
        }

        if (!InstitutionPortalDomainChallenge.Matches(verification.ChallengeValueHash, records))
        {
            throw new ConflictException("The expected TXT record was not found. DNS changes may need time to propagate.");
        }

        var marked = await repository.MarkVerifiedAsync(
            request.Id,
            actor.InstitutionId,
            actor.StaffMemberId,
            verification.ChallengeValueHash,
            now,
            cancellationToken);
        if (!marked)
        {
            throw new ConflictException("The portal-domain challenge changed during verification. Retry with the current TXT record.");
        }

        var page = await repository.GetPageAsync(
            actor.InstitutionId,
            new InstitutionPortalDomainFilterDTO { PageNumber = 1, PageSize = 10 },
            cancellationToken);
        var verifiedDomain = page.Data.Single(item => item.Id == request.Id);
        return ApiResponse<InstitutionPortalDomainResponseDTO>.Success(
            verifiedDomain, "Portal domain verified and enabled successfully.");
    }
}

public sealed class DeleteInstitutionPortalDomainCommandHandler(
    IInstitutionPortalDomainMasterRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteInstitutionPortalDomainCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteInstitutionPortalDomainCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.DeleteAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Institution portal domain was deactivated.");
    }
}

#endregion
