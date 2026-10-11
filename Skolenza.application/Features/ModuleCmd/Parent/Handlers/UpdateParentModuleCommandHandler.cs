// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates Parent/Header Modules for authenticated Host users.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to update an existing Parent/Header Module.
    /// </summary>
    public class UpdateParentModuleCommand : IRequest<ApiResponse<GetParentModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateParentModuleCommand"/> class.
        /// </summary>
        /// <param name="id">The Header Module identifier.</param>
        /// <param name="dto">The editable Header Module values and target scope.</param>
        public UpdateParentModuleCommand(int id, UpdateParentModuleRequestDTO? dto)
        {
            Id = id;
            DTO = dto;
        }

        /// <summary>Gets the Header Module identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the editable Header Module values and target scope.</summary>
        public UpdateParentModuleRequestDTO? DTO { get; }
    }

    #endregion

    /// <summary>
    /// Handles Host-authorized Parent/Header Module updates without changing their structural role or scope.
    /// </summary>
    public class UpdateParentModuleCommandHandler : IRequestHandler<UpdateParentModuleCommand, ApiResponse<GetParentModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateParentModuleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateParentModuleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public UpdateParentModuleCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateParentModuleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Updates client-editable Header Module values within the requested existing scope.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The updated Header Module response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped Header Module does not exist.</exception>
        public async Task<ApiResponse<GetParentModuleResponseDTO>> Handle(
            UpdateParentModuleCommand request,
            CancellationToken cancellationToken)
        {
            var hostContext = await _commonRequestService.ValidateHostSuperAdminRequestAsync();
            var hostUserId = hostContext.HostUserId;

            if (request == null || request.Id <= 0 || request.DTO == null)
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
                var entity = await _unitOfWork.ModuleRepository.GetParentModuleForUpdateAsync(
                    request.Id,
                    dto.ModuleScope,
                    cancellationToken);

                if (entity == null)
                {
                    throw new KeyNotFoundException("Parent Module was not found in the requested ModuleScope.");
                }

                var moduleCode = dto.ModuleCode.Trim();
                var duplicateExists = await _unitOfWork.ModuleRepository.ExistsParentModuleCodeAsync(
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
                entity.ParentModuleId = null;
                entity.IsLeafNode = false;
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

                // ModuleScope and InstitutionId stay unchanged because moving an existing Header can invalidate children and permissions.
                var updated = await _unitOfWork.ModuleRepository.UpdateParentModuleAsync(entity, cancellationToken);

                return ApiResponse<GetParentModuleResponseDTO>.Success(
                    ToResponse(updated),
                    "Parent Module updated successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to update Parent Module {ModuleId} in ModuleScope {ModuleScope}.", request.Id, dto.ModuleScope);
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
}
