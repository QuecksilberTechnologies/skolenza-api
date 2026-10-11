// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates direct SubModules for authenticated Host users.
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
    /// Represents the request to create a direct child SubModule.
    /// </summary>
    public class CreateSubModuleCommand : IRequest<ApiResponse<GetSubModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSubModuleCommand"/> class.
        /// </summary>
        /// <param name="dto">The requested direct child Module values.</param>
        public CreateSubModuleCommand(CreateSubModuleRequestDTO? dto)
        {
            DTO = dto;
        }

        /// <summary>Gets the requested direct child Module values.</summary>
        public CreateSubModuleRequestDTO? DTO { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized creation of a direct child SubModule.
    /// </summary>
    public class CreateSubModuleCommandHandler : IRequestHandler<CreateSubModuleCommand, ApiResponse<GetSubModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateSubModuleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSubModuleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public CreateSubModuleCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<CreateSubModuleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Creates a direct child only under a compatible Header Module in the requested scope.
        /// </summary>
        /// <param name="request">The creation request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The created direct child response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped Header Module does not exist.</exception>
        public async Task<ApiResponse<GetSubModuleResponseDTO>> Handle(
            CreateSubModuleCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request?.DTO == null || request.DTO.ParentModuleId <= 0)
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

            var utcNow = DateTime.UtcNow;
            try
            {
                var parentModule = await _unitOfWork.ModuleRepository.GetParentModuleForSubModuleAsync(
                    dto.ParentModuleId,
                    dto.ModuleScope,
                    cancellationToken);

                if (parentModule == null)
                {
                    throw new KeyNotFoundException("Parent header was not found in the requested ModuleScope.");
                }

                if (dto.IsActive && !parentModule.IsActive)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                var moduleCode = dto.ModuleCode.Trim();
                var duplicateExists = await _unitOfWork.ModuleRepository.ExistsSubModuleCodeAsync(
                    moduleCode,
                    parentModule.InstitutionId,
                    dto.ModuleScope,
                    null,
                    cancellationToken);

                if (duplicateExists)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                var entity = new Module
                {
                    InstitutionId = parentModule.InstitutionId,
                    ModuleScope = dto.ModuleScope,
                    ModuleCode = moduleCode,
                    ModuleName = dto.ModuleName.Trim(),
                    DisplayName = dto.DisplayName?.Trim(),
                    Urlpath = dto.URLPath?.Trim(),
                    ParentModuleId = parentModule.Id,
                    IsLeafNode = true,
                    IsModuleDisplayInUI = dto.IsModuleDisplayInUI,
                    IsCommonMenu = dto.IsCommonMenu,
                    IsActive = dto.IsActive,
                    ImageIconWeb = dto.ImageIconWeb?.Trim(),
                    ImageIconMobile = dto.ImageIconMobile?.Trim(),
                    ItemPriority = dto.ItemPriority,
                    Remark = dto.Remark?.Trim(),
                    AddedById = hostUserId,
                    AddedDateTime = utcNow,
                    UpdatedById = null,
                    UpdatedDateTime = null
                };

                var created = await _unitOfWork.ModuleRepository.AddSubModuleAsync(entity, cancellationToken);

                return ApiResponse<GetSubModuleResponseDTO>.Success(
                    ToResponse(created, parentModule),
                    "Child module created successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to create Child module for ParentHeaderId {ParentHeaderId} and ModuleScope {ModuleScope}.",
                    dto.ParentModuleId,
                    dto.ModuleScope);
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
