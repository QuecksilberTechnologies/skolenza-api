using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;

namespace skolenza.application.Features.InstitutionEmailTemplateCmd;

public interface IInstitutionEmailTemplatePermissionRequest
{
    PermissionRequestDTO? PermissionRequest { get; }
}

public sealed class InstitutionEmailTemplatePermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const string ModuleCode = "INSTITUTION_EMAIL_TEMPLATE";

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IInstitutionEmailTemplatePermissionRequest protectedRequest)
        {
            return await next();
        }

        var permission = protectedRequest.PermissionRequest
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (permission.ModuleId <= 0 || permission.OperationId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var moduleCode = await commonRequestService.GetModuleCodeAsync(permission.ModuleId);
        if (!string.Equals(moduleCode, ModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0 || actor.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var result = await unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(
            actor.InstitutionId, actor.LoggedInStaffMemberId, actor.RoleId,
            permission.ModuleId, permission.OperationId, cancellationToken);
        InstitutionRuntimePermissionValidator.EnsureAllowed(result);
        return await next();
    }
}
