// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates Parent/Header Modules for authenticated Host users.
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
    /// Represents the request to create a Parent/Header Module.
    /// </summary>
    public class CreateParentModuleCommand : IRequest<ApiResponse<GetParentModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateParentModuleCommand"/> class.
        /// </summary>
        /// <param name="dto">The requested Header Module values.</param>
        public CreateParentModuleCommand(CreateParentModuleRequestDTO? dto)
        {
            DTO = dto;
        }

        /// <summary>
        /// Gets the requested Header Module values.
        /// </summary>
        public CreateParentModuleRequestDTO? DTO { get; }
    }

    #endregion

    /// <summary>
    /// Handles Host-authorized Parent/Header Module creation.
    /// </summary>
    public class CreateParentModuleCommandHandler : IRequestHandler<CreateParentModuleCommand, ApiResponse<GetParentModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateParentModuleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateParentModuleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="commonRequestService">Validates the authenticated Host request and resolves its trusted actor.</param>
        /// <param name="logger">Records processing failures.</param>
        public CreateParentModuleCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<CreateParentModuleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Creates a Header Module after Host authorization and requested-scope validation.
        /// </summary>
        /// <param name="request">The creation request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The created Header Module response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        public async Task<ApiResponse<GetParentModuleResponseDTO>> Handle(
            CreateParentModuleCommand request,
            CancellationToken cancellationToken)
        {
            var hostContext = await _commonRequestService.ValidateHostSuperAdminRequestAsync();
            var hostUserId = hostContext.HostUserId;

            if (request?.DTO == null)
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
                var institutionId = ResolveInstitutionId(dto);
                var moduleCode = dto.ModuleCode.Trim();
                var duplicateExists = await _unitOfWork.ModuleRepository.ExistsParentModuleCodeAsync(
                    moduleCode,
                    institutionId,
                    dto.ModuleScope,
                    null,
                    cancellationToken);

                if (duplicateExists)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
                }

                var entity = new Module
                {
                    InstitutionId = institutionId,
                    ModuleScope = dto.ModuleScope,
                    ModuleCode = moduleCode,
                    ModuleName = dto.ModuleName.Trim(),
                    DisplayName = dto.DisplayName?.Trim(),
                    Urlpath = dto.URLPath?.Trim(),
                    ParentModuleId = null,
                    IsLeafNode = false,
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

                var created = await _unitOfWork.ModuleRepository.AddParentModuleAsync(entity, cancellationToken);

                return ApiResponse<GetParentModuleResponseDTO>.Success(
                    ToResponse(created),
                    "Parent Module created successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to create Parent Module for HostUserId {HostUserId} and ModuleScope {ModuleScope}.",
                    hostUserId,
                    dto.ModuleScope);
                throw;
            }
        }

        #endregion

        #region Private Methods

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

        /// <summary>
        /// Resolves institution ownership using the existing create-request value only for Institution-scope modules.
        /// </summary>
        /// <param name="dto">The validated create request.</param>
        /// <returns>The target institution identifier, or <see langword="null"/> for Host or institution master modules without a target institution.</returns>
        private static long? ResolveInstitutionId(CreateParentModuleRequestDTO dto)
        {
            if (dto.ModuleScope == AppConstants.HostModuleScope)
            {
                return null;
            }

            return dto.InstitutionId > 0 ? dto.InstitutionId : null;
        }

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
