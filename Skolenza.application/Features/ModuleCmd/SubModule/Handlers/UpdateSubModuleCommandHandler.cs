// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates direct SubModules for authenticated Host users.
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
    /// Represents the request to update a direct child SubModule.
    /// </summary>
    public class UpdateSubModuleCommand : IRequest<ApiResponse<GetSubModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubModuleCommand"/> class.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="dto">The editable SubModule values and current scope.</param>
        public UpdateSubModuleCommand(int id, UpdateSubModuleRequestDTO? dto)
        {
            Id = id;
            DTO = dto;
        }

        /// <summary>Gets the SubModule identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the editable SubModule values and current scope.</summary>
        public UpdateSubModuleRequestDTO? DTO { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized direct SubModule updates without changing inherited institution ownership or scope.
    /// </summary>
    public class UpdateSubModuleCommandHandler : IRequestHandler<UpdateSubModuleCommand, ApiResponse<GetSubModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateSubModuleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubModuleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public UpdateSubModuleCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateSubModuleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Updates editable direct-child values and permits a move only to a compatible Header Module.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The updated direct child response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped child or target Header Module does not exist.</exception>
        public async Task<ApiResponse<GetSubModuleResponseDTO>> Handle(
            UpdateSubModuleCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request == null || request.Id <= 0 || request.DTO == null || request.DTO.ParentModuleId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            var dto = request.DTO;
            if (string.IsNullOrWhiteSpace(dto.ModuleCode) || string.IsNullOrWhiteSpace(dto.ModuleName))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            if (!IsSupportedModuleScope(dto.ModuleScope))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            try
            {
                var entity = await _unitOfWork.ModuleRepository.GetSubModuleForUpdateAsync(
                    request.Id,
                    dto.ModuleScope,
                    cancellationToken);

                if (entity == null)
                {
                    throw new KeyNotFoundException("Child module was not found in the requested ModuleScope.");
                }

                var parentModule = await _unitOfWork.ModuleRepository.GetParentModuleForSubModuleAsync(
                    dto.ParentModuleId,
                    dto.ModuleScope,
                    cancellationToken);

                if (parentModule == null)
                {
                    throw new KeyNotFoundException("Parent header was not found in the requested ModuleScope.");
                }

                if (parentModule.InstitutionId != entity.InstitutionId)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                if (dto.IsActive && !parentModule.IsActive)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                var moduleCode = dto.ModuleCode.Trim();
                var duplicateExists = await _unitOfWork.ModuleRepository.ExistsSubModuleCodeAsync(
                    moduleCode,
                    entity.InstitutionId,
                    entity.ModuleScope,
                    entity.Id,
                    cancellationToken);

                if (duplicateExists)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                entity.ModuleCode = moduleCode;
                entity.ModuleName = dto.ModuleName.Trim();
                entity.DisplayName = dto.DisplayName?.Trim();
                entity.Urlpath = dto.URLPath?.Trim();
                entity.ParentModuleId = parentModule.Id;
                entity.IsLeafNode = true;
                entity.IsModuleDisplayInUI = dto.IsModuleDisplayInUI;
                entity.IsCommonMenu = dto.IsCommonMenu;
                entity.IsActive = dto.IsActive;
                entity.ImageIconWeb = dto.ImageIconWeb?.Trim();
                entity.ImageIconMobile = dto.ImageIconMobile?.Trim();
                entity.ItemPriority = dto.ItemPriority;
                entity.Remark = dto.Remark?.Trim();
                var utcNow = DateTime.UtcNow;
                entity.UpdatedById = hostUserId;
                entity.UpdatedDateTime = utcNow;

                // ModuleScope and InstitutionId remain immutable because they are inherited from the existing module design.
                var updated = await _unitOfWork.ModuleRepository.UpdateSubModuleAsync(entity, cancellationToken);

                return ApiResponse<GetSubModuleResponseDTO>.Success(
                    ToResponse(updated, parentModule),
                    "Child module updated successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to update Child module {ModuleId} in ModuleScope {ModuleScope}.", request.Id, dto.ModuleScope);
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
        /// Maps a persisted direct child and its validated Header Module to the CRUD response.
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
