// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles Host Super Admin Institution Parent Module status cascades.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Module.InstitutionParentModule;
using skolenza.application.Exceptions;
using skolenza.application.Features.ModuleCmd.InstitutionParent.Queries;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.InstitutionParent.Handlers;

#region Status Command

/// <summary>
/// Represents the Host-managed request to update one Institution Parent Module status cascade.
/// </summary>
public sealed class UpdateInstitutionParentModuleStatusCommand
    : IRequest<ApiResponse<InstitutionParentModuleResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateInstitutionParentModuleStatusCommand"/> class.
    /// </summary>
    /// <param name="moduleId">The global Module identifier exposed by the API.</param>
    /// <param name="requestDTO">The encrypted Institution identifier and requested enabled state.</param>
    public UpdateInstitutionParentModuleStatusCommand(
        int moduleId,
        UpdateInstitutionParentModuleStatusRequestDTO? requestDTO)
    {
        ModuleId = moduleId;
        RequestDTO = requestDTO;
    }

    /// <summary>Gets the global Module identifier exposed by the API.</summary>
    public int ModuleId { get; }

    /// <summary>Gets the encrypted Institution identifier and requested enabled state.</summary>
    public UpdateInstitutionParentModuleStatusRequestDTO? RequestDTO { get; }
}

#endregion

#region Status Command Handler

/// <summary>
/// Updates a Institution-entitled Main Parent or Sub-Parent Header Module and all of its descendants for an authenticated Host Super Admin.
/// </summary>
public sealed class UpdateInstitutionParentModuleStatusCommandHandler
    : InstitutionParentModuleQueryHandlerBase,
      IRequestHandler<UpdateInstitutionParentModuleStatusCommand, ApiResponse<InstitutionParentModuleResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateInstitutionParentModuleStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution entitlement persistence and transaction operations.</param>
    /// <param name="commonRequestService">Validates the current Host Super Admin and Host encryption-key context.</param>
    /// <param name="idEncoderService">Encodes Institution identifiers at the Host API boundary.</param>
    public UpdateInstitutionParentModuleStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
        : base(unitOfWork, commonRequestService, idEncoderService)
    {
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionParentModuleResponseDTO>> Handle(
        UpdateInstitutionParentModuleStatusCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request?.RequestDTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var context = await ResolveInstitutionRequestAsync(dto.InstitutionId, cancellationToken);
        if (request.ModuleId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        await UnitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var module = await UnitOfWork.InstitutionParentModuleRepository.StageStatusCascadeAsync(
                context.InstitutionId,
                request.ModuleId,
                dto.IsActive,
                // Institution entitlement rows use the Institution identifier as their audit actor.
                context.InstitutionId,
                DateTime.UtcNow,
                cancellationToken);

            if (module is null)
            {
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            }

            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<InstitutionParentModuleResponseDTO>.Success(
                MapResponse(module, context.InstitutionEncryptionKey),
                "Institution Parent Module status updated successfully.");
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

#endregion
