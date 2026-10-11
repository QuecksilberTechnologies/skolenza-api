// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles Parent Module status changes and cascades active and visible state through descendant modules and their operation mappings.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System.Linq;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to change a Parent/Header Module active state in a required scope.
    /// </summary>
    public class UpdateParentModuleStatusCommand : IRequest<ApiResponse<GetParentModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateParentModuleStatusCommand"/> class.
        /// </summary>
        /// <param name="id">The Header Module identifier.</param>
        /// <param name="dto">The target active state and required module scope.</param>
        public UpdateParentModuleStatusCommand(int id, UpdateParentModuleStatusRequestDTO? dto)
        {
            Id = id;
            DTO = dto;
        }

        /// <summary>Gets the Header Module identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the target active state and required module scope.</summary>
        public UpdateParentModuleStatusRequestDTO? DTO { get; }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles Host Super Admin-authorized Parent/Header Module status changes and cascades the requested state downward through every descendant module and applicable operation mapping.
    /// </summary>
    public class UpdateParentModuleStatusCommandHandler : IRequestHandler<UpdateParentModuleStatusCommand, ApiResponse<GetParentModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateParentModuleStatusCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the module configuration repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        public UpdateParentModuleStatusCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Updates a Parent/Header Module's active and visible state and cascades the same values downward to every descendant module and applicable operation mapping.
        /// </summary>
        /// <param name="request">The status change request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The Parent Module response after the complete cascade succeeds.</returns>
        /// <exception cref="ValidationErrorException">Thrown when the request or module scope is invalid.</exception>
        /// <exception cref="NotFoundException">Thrown when the requested identifier does not resolve to a Parent Module in the requested scope.</exception>
        public async Task<ApiResponse<GetParentModuleResponseDTO>> Handle(
            UpdateParentModuleStatusCommand request,
            CancellationToken cancellationToken)
        {
            // Resolve the authenticated Host Super Admin actor before processing client-provided data.
            var hostContext = await _commonRequestService.ValidateHostSuperAdminRequestAsync();
            var hostUserId = hostContext.HostUserId;
            cancellationToken.ThrowIfCancellationRequested();

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

                // Root and nested Header Modules are valid cascade targets; leaf modules remain excluded.
                var parentModule = await _unitOfWork.ModuleRepository.GetHeaderModuleForStatusUpdateAsync(
                    request.Id,
                    request.DTO.ModuleScope,
                    cancellationToken);

                if (parentModule == null)
                {
                    throw new NotFoundException(AppConstants.ErrorMessages.ParentModuleNotFound);
                }

                // Traverse every level below the selected Header Module in its scope.
                var descendantModules = await _unitOfWork.ModuleRepository.GetDescendantModulesForStatusUpdateAsync(
                    parentModule.Id,
                    parentModule.ModuleScope,
                    cancellationToken);

                var affectedModuleIds = descendantModules
                    .Select(module => module.Id)
                    .Append(parentModule.Id)
                    .ToList();
                var operationMappings = await _unitOfWork.ModuleRepository.GetModuleOperationMappingsForStatusUpdateAsync(
                    affectedModuleIds,
                    cancellationToken);
                var childModuleIds = descendantModules
                    .Select(module => module.Id)
                    .ToList();
                var hostRolePermissions = await _unitOfWork.HostRolePermissionRepository
                    .GetNonDeletedByModuleIdsAsync(childModuleIds, cancellationToken);

                var updatedDateTime = DateTime.UtcNow;

                // Apply the requested active state while preserving module hierarchy and creation data.
                if (parentModule.IsActive != dto.IsActive ||
                    parentModule.IsModuleDisplayInUI != dto.IsActive)
                {
                    parentModule.IsActive = dto.IsActive;
                    parentModule.IsModuleDisplayInUI = dto.IsActive;
                    parentModule.UpdatedById = hostUserId;
                    parentModule.UpdatedDateTime = updatedDateTime;
                }

                // Apply the requested active and visible state only to descendants, including nested headers and leaf modules.
                foreach (var descendantModule in descendantModules)
                {
                    if (descendantModule.IsActive != dto.IsActive ||
                        descendantModule.IsModuleDisplayInUI != dto.IsActive)
                    {
                        descendantModule.IsActive = dto.IsActive;
                        descendantModule.IsModuleDisplayInUI = dto.IsActive;
                        descendantModule.UpdatedById = hostUserId;
                        descendantModule.UpdatedDateTime = updatedDateTime;
                    }
                }

                // Module-operation mappings are directly linked to modules, so only mappings for the affected tree are updated.
                foreach (var operationMapping in operationMappings)
                {
                    if (operationMapping.IsActive != dto.IsActive)
                    {
                        operationMapping.IsActive = dto.IsActive;
                        operationMapping.UpdatedById = hostUserId;
                        operationMapping.UpdatedDateTime = updatedDateTime;
                    }
                }

                // Host-role permissions are assigned to operational child Modules, not necessarily the Header itself.
                foreach (var hostRolePermission in hostRolePermissions)
                {
                    if (hostRolePermission.IsActive != dto.IsActive)
                    {
                        hostRolePermission.IsActive = dto.IsActive;
                        hostRolePermission.UpdatedById = hostUserId;
                        hostRolePermission.UpdatedDateTime = updatedDateTime;
                    }
                }

                await _unitOfWork.ModuleRepository.SaveModuleStatusCascadeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ApiResponse<GetParentModuleResponseDTO>.Success(
                    ToResponse(parentModule),
                    AppConstants.SuccessMessages.ParentModuleStatusUpdatedSuccessfully);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
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
        /// Maps a persisted Header Module to the CRUD response.
        /// </summary>
        /// <param name="module">The persisted Header Module.</param>
        /// <returns>The response model.</returns>
        private static GetParentModuleResponseDTO ToResponse(Module module)
        {
            return new GetParentModuleResponseDTO
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
                UpdatedDateTime = module.UpdatedDateTime
            };
        }

        #endregion
    }

    #endregion
}
