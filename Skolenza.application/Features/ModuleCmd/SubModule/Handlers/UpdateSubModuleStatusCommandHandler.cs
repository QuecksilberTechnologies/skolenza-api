// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates direct SubModule status for authenticated Host users.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using skolenza.application.Features.ModuleCmd.SubModule.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ModuleCmd.SubModule.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to change a direct SubModule active state in a required scope.
    /// </summary>
    public class UpdateSubModuleStatusCommand : IRequest<ApiResponse<GetSubModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubModuleStatusCommand"/> class.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="dto">The target active state and required module scope.</param>
        public UpdateSubModuleStatusCommand(int id, UpdateSubModuleStatusRequestDTO? dto)
        {
            Id = id;
            DTO = dto;
        }

        /// <summary>Gets the SubModule identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the target active state and required module scope.</summary>
        public UpdateSubModuleStatusRequestDTO? DTO { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized non-destructive direct SubModule status changes.
    /// </summary>
    public class UpdateSubModuleStatusCommandHandler : IRequestHandler<UpdateSubModuleStatusCommand, ApiResponse<GetSubModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateSubModuleStatusCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubModuleStatusCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public UpdateSubModuleStatusCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateSubModuleStatusCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Changes a direct child status, cascades the state to its linked operation mappings, and only activates under an active ancestor hierarchy.
        /// </summary>
        /// <param name="request">The status change request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The direct-child response after its status changes.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped direct child or Header Module does not exist.</exception>
        public async Task<ApiResponse<GetSubModuleResponseDTO>> Handle(
            UpdateSubModuleStatusCommand request,
            CancellationToken cancellationToken)
        {
            var hostContext =
                await _commonRequestService.ValidateHostSuperAdminRequestAsync();
            var hostUserId = hostContext.HostUserId;

            if (request == null || request.Id <= 0 || request.DTO == null)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            var dto = request.DTO;
            if (!IsSupportedModuleScope(dto.ModuleScope))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var entity = await _unitOfWork.ModuleRepository.GetSubModuleForUpdateAsync(
                    request.Id,
                    dto.ModuleScope,
                    cancellationToken);

                if (entity == null)
                {
                    throw new KeyNotFoundException("Child module was not found in the requested ModuleScope.");
                }

                if (dto.IsActive)
                {
                    // A SubModule and its linked operation mappings cannot be activated while any ancestor is inactive.
                    var hierarchy = await _unitOfWork.ModuleRepository
                        .GetModuleHierarchyForOperationActivationAsync(entity.Id, cancellationToken);

                    if (hierarchy == null || hierarchy.Count < 2)
                    {
                        throw new ConflictException(
                            "The sub-module cannot be activated because its parent module is deleted or unavailable.");
                    }

                    for (var hierarchyIndex = 1; hierarchyIndex < hierarchy.Count; hierarchyIndex++)
                    {
                        var ancestor = hierarchy[hierarchyIndex];
                        if (!ancestor.IsActive)
                        {
                            throw new ConflictException(
                                $"The sub-module cannot be activated because its parent module '{ancestor.ModuleName}' is inactive.");
                        }
                    }
                }

                var parentModule = await _unitOfWork.ModuleRepository.GetParentModuleForSubModuleAsync(
                    entity.ParentModuleId!.Value,
                    entity.ModuleScope,
                    cancellationToken);

                if (parentModule == null)
                {
                    throw new KeyNotFoundException("Parent header was not found in the requested ModuleScope.");
                }

                var utcNow = DateTime.UtcNow;
                var operationMappings = await _unitOfWork.ModuleRepository
                    .GetModuleOperationMappingsForStatusUpdateAsync(new[] { entity.Id }, cancellationToken);
                var hostRolePermissions = await _unitOfWork.HostRolePermissionRepository
                    .GetNonDeletedByModuleIdsAsync(new[] { entity.Id }, cancellationToken);

                if (entity.IsActive != dto.IsActive)
                {
                    entity.ParentModuleId = parentModule.Id;
                    entity.IsLeafNode = true;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedById = hostUserId;
                    entity.UpdatedDateTime = utcNow;
                }

                // Only mappings belonging to this direct SubModule receive the requested state.
                foreach (var operationMapping in operationMappings)
                {
                    if (operationMapping.IsActive != dto.IsActive)
                    {
                        operationMapping.IsActive = dto.IsActive;
                        operationMapping.UpdatedById = hostUserId;
                        operationMapping.UpdatedDateTime = utcNow;
                    }
                }

                foreach (var hostRolePermission in hostRolePermissions)
                {
                    if (hostRolePermission.IsActive != dto.IsActive)
                    {
                        hostRolePermission.IsActive = dto.IsActive;
                        hostRolePermission.UpdatedById = hostUserId;
                        hostRolePermission.UpdatedDateTime = utcNow;
                    }
                }

                await _unitOfWork.ModuleRepository.SaveModuleStatusCascadeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ApiResponse<GetSubModuleResponseDTO>.Success(
                    ToResponse(entity, parentModule),
                    "Child module status updated successfully.");
            }
            catch (Exception exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _logger.LogError(exception, "Unable to update Child module {ModuleId} status in ModuleScope {ModuleScope}.", request.Id, dto.ModuleScope);
                throw;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Determines whether the requested scope is one of the two supported application module scopes.
        /// </summary>
        /// <param name="moduleScope">The requested module scope.</param>
        /// <returns><see langword="true"/> when the scope is supported.</returns>
        private static bool IsSupportedModuleScope(short moduleScope)
        {
            return moduleScope == AppConstants.InstitutionModuleScope ||
                   moduleScope == AppConstants.HostModuleScope;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Maps a persisted direct child and its Header Module to the CRUD response.
        /// </summary>
        /// <param name="module">The persisted direct child.</param>
        /// <param name="parentModule">The validated Header Module.</param>
        /// <returns>The direct-child response.</returns>
        private static GetSubModuleResponseDTO ToResponse(Module module, Module parentModule)
        {
            return new GetSubModuleResponseDTO
            {
                Id = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                DisplayName = module.DisplayName,
                URLPath = module.Urlpath,
                ParentModuleId = module.ParentModuleId,
                IsLeafNode = module.IsLeafNode,
                IsModuleDisplayInUI = module.IsModuleDisplayInUI,
                IsCommonMenu = module.IsCommonMenu,
                ModuleScope = module.ModuleScope,
                IsActive = module.IsActive,
                ImageIconWeb = module.ImageIconWeb,
                ImageIconMobile = module.ImageIconMobile,
                ItemPriority = module.ItemPriority,
                Remark = module.Remark,
                AddedById = module.AddedById,
                AddedDateTime = module.AddedDateTime,
                UpdatedById = module.UpdatedById,
                UpdatedDateTime = module.UpdatedDateTime,
                ParentModule = new ParentModuleSummaryDTO
                {
                    Id = parentModule.Id,
                    ModuleCode = parentModule.ModuleCode,
                    ModuleName = parentModule.ModuleName,
                    DisplayName = parentModule.DisplayName,
                    ModuleScope = parentModule.ModuleScope,
                    IsActive = parentModule.IsActive
                }
            };
        }

        #endregion
    }
}
