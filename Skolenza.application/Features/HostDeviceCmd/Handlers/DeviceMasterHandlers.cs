// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles authenticated Host administration of the DeviceMaster catalog.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostDeviceCmd.Handlers;

#region Command

/// <summary>Creates a Host-managed device model.</summary>
public sealed class CreateDeviceMasterCommand(CreateDeviceMasterRequestDTO dto) : IRequest<ApiResponse<DeviceMasterResponseDTO>> { public CreateDeviceMasterRequestDTO DTO { get; } = dto; }
/// <summary>Updates a Host-managed device model.</summary>
public sealed class UpdateDeviceMasterCommand(UpdateDeviceMasterRequestDTO dto) : IRequest<ApiResponse<DeviceMasterResponseDTO>> { public UpdateDeviceMasterRequestDTO DTO { get; } = dto; }
/// <summary>Changes a Host-managed device model active state.</summary>
public sealed class UpdateDeviceMasterStatusCommand(UpdateDeviceMasterStatusRequestDTO dto) : IRequest<ApiResponse<DeviceMasterResponseDTO>> { public UpdateDeviceMasterStatusRequestDTO DTO { get; } = dto; }
/// <summary>Soft deletes a Host-managed device model.</summary>
public sealed class DeleteDeviceMasterCommand(long id) : IRequest<ApiResponse<bool>> { public long Id { get; } = id; }

#endregion

#region Query

/// <summary>Retrieves one Host-managed device model.</summary>
public sealed class GetDeviceMasterByIdQuery(long id) : IRequest<ApiResponse<DeviceMasterResponseDTO>> { public long Id { get; } = id; }
/// <summary>Retrieves one Host-managed device model by its manufacturer serial number.</summary>
public sealed class GetDeviceMasterInfoBySNoQuery(string sNo) : IRequest<ApiResponse<DeviceMasterResponseDTO>> { public string SNo { get; } = sNo; }
/// <summary>Retrieves a database-paged Host-managed device model list.</summary>
public sealed class GetAllDeviceMastersQuery(GetDeviceMasterListRequestDTO filter) : IRequest<ApiResponse<List<DeviceMasterResponseDTO>>> { public GetDeviceMasterListRequestDTO Filter { get; } = filter; }

#endregion

#region Handler

/// <summary>Handles creation of Host-managed device models.</summary>
public sealed class CreateDeviceMasterCommandHandler : IRequestHandler<CreateDeviceMasterCommand, ApiResponse<DeviceMasterResponseDTO>>
{
    #region Fields
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService; private readonly ILogger<CreateDeviceMasterCommandHandler> _logger;
    #endregion
    #region Constructor
    /// <summary>Initializes handler dependencies.</summary>
    public CreateDeviceMasterCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<CreateDeviceMasterCommandHandler> logger) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; _logger = logger; }
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceMasterResponseDTO>> Handle(CreateDeviceMasterCommand request, CancellationToken cancellationToken)
    {
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync(); Validate(request.DTO);
        var dto = request.DTO;
        if (await _unitOfWork.DeviceMasterRepository.DuplicateExistsAsync(dto.DeviceCode.Trim(), dto.CompanyName.Trim(), dto.ModelNo.Trim(), null, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateDeviceMaster);
        var entity = _mapper.Map<DeviceMaster>(dto); entity.SNo = dto.SNo.Trim(); entity.DeviceCode = dto.DeviceCode.Trim(); entity.DeviceName = dto.DeviceName.Trim(); entity.CompanyName = dto.CompanyName.Trim(); entity.ModelNo = dto.ModelNo.Trim(); entity.IsOccupied = false; entity.IsSoftDeleted = false; entity.AddedById = hostUserId; entity.AddedDateTime = DateTime.UtcNow;
        await _unitOfWork.DeviceMasterRepository.AddAsync(entity, cancellationToken); await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created DeviceMaster {DeviceMasterId} by HostUser {HostUserId}.", entity.Id, hostUserId);
        return ApiResponse<DeviceMasterResponseDTO>.Success(_mapper.Map<DeviceMasterResponseDTO>(entity), AppConstants.SuccessMessages.DeviceMasterCreated);
    }
    #endregion
    /// <summary>Shared manual-create and bulk-import required-field validation.</summary>
    public static void Validate(CreateDeviceMasterRequestDTO? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.SNo) ||
            string.IsNullOrWhiteSpace(dto.DeviceCode) || string.IsNullOrWhiteSpace(dto.DeviceName) ||
            string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.ModelNo) ||
            !Enum.IsDefined(dto.DeviceType))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
    }
}

/// <summary>Handles updates to Host-managed device models.</summary>
public sealed class UpdateDeviceMasterCommandHandler : IRequestHandler<UpdateDeviceMasterCommand, ApiResponse<DeviceMasterResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService; private readonly ILogger<UpdateDeviceMasterCommandHandler> _logger;
    /// <summary>Initializes handler dependencies.</summary>
    public UpdateDeviceMasterCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<UpdateDeviceMasterCommandHandler> logger) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; _logger = logger; }
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceMasterResponseDTO>> Handle(UpdateDeviceMasterCommand request, CancellationToken cancellationToken)
    {
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync(); if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); Validate(request.DTO);
        var dto = request.DTO; var entity = await _unitOfWork.DeviceMasterRepository.GetForUpdateAsync(dto.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.DeviceMasterNotFound);
        if (entity.IsOccupied) throw new ConflictException(AppConstants.ErrorMessages.DeviceMasterAlreadyRegisteredWithInstitution);
        if (await _unitOfWork.DeviceMasterRepository.DuplicateExistsAsync(dto.DeviceCode.Trim(), dto.CompanyName.Trim(), dto.ModelNo.Trim(), entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateDeviceMaster);
        _mapper.Map(dto, entity); entity.SNo = dto.SNo.Trim(); entity.DeviceCode = dto.DeviceCode.Trim(); entity.DeviceName = dto.DeviceName.Trim(); entity.CompanyName = dto.CompanyName.Trim(); entity.ModelNo = dto.ModelNo.Trim(); entity.UpdatedById = hostUserId; entity.UpdatedDateTime = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated DeviceMaster {DeviceMasterId} by HostUser {HostUserId}.", entity.Id, hostUserId);
        return ApiResponse<DeviceMasterResponseDTO>.Success(_mapper.Map<DeviceMasterResponseDTO>(entity), AppConstants.SuccessMessages.DeviceMasterUpdated);
    }
    private static void Validate(UpdateDeviceMasterRequestDTO dto) { if (string.IsNullOrWhiteSpace(dto.SNo) || string.IsNullOrWhiteSpace(dto.DeviceCode) || string.IsNullOrWhiteSpace(dto.DeviceName) || string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.ModelNo) || !Enum.IsDefined(dto.DeviceType)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest); }
}

/// <summary>Handles DeviceMaster active-state changes.</summary>
public sealed class UpdateDeviceMasterStatusCommandHandler : IRequestHandler<UpdateDeviceMasterStatusCommand, ApiResponse<DeviceMasterResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService; private readonly ILogger<UpdateDeviceMasterStatusCommandHandler> _logger;
    /// <summary>Initializes handler dependencies.</summary>
    public UpdateDeviceMasterStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<UpdateDeviceMasterStatusCommandHandler> logger) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; _logger = logger; }
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceMasterResponseDTO>> Handle(UpdateDeviceMasterStatusCommand request, CancellationToken cancellationToken)
    {
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync(); if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var entity = await _unitOfWork.DeviceMasterRepository.GetForUpdateAsync(request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.DeviceMasterNotFound);
        if (entity.IsOccupied
            || await _unitOfWork.DeviceMasterRepository.HasActiveInstitutionDevicesAsync(entity.Id, cancellationToken)
            || await _unitOfWork.DeviceMasterRepository.HasInstitutionDeviceConfigurationsAsync(entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DeviceMasterAlreadyRegisteredWithInstitution);
        }
        entity.IsActive = request.DTO.IsActive; entity.UpdatedById = hostUserId; entity.UpdatedDateTime = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Changed DeviceMaster {DeviceMasterId} status to {IsActive} by HostUser {HostUserId}.", entity.Id, entity.IsActive, hostUserId);
        return ApiResponse<DeviceMasterResponseDTO>.Success(_mapper.Map<DeviceMasterResponseDTO>(entity), AppConstants.SuccessMessages.DeviceMasterStatusUpdated);
    }
}

/// <summary>Handles DeviceMaster soft deletion.</summary>
public sealed class DeleteDeviceMasterCommandHandler : IRequestHandler<DeleteDeviceMasterCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly ICommonRequestService _commonRequestService; private readonly ILogger<DeleteDeviceMasterCommandHandler> _logger;
    /// <summary>Initializes handler dependencies.</summary>
    public DeleteDeviceMasterCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, ILogger<DeleteDeviceMasterCommandHandler> logger) { _unitOfWork = unitOfWork; _commonRequestService = commonRequestService; _logger = logger; }
    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(DeleteDeviceMasterCommand request, CancellationToken cancellationToken)
    {
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync(); var entity = await _unitOfWork.DeviceMasterRepository.GetForUpdateAsync(request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.DeviceMasterNotFound);
        if (entity.IsOccupied
            || await _unitOfWork.DeviceMasterRepository.HasInstitutionDevicesAsync(entity.Id, cancellationToken)
            || await _unitOfWork.DeviceMasterRepository.HasInstitutionDeviceConfigurationsAsync(entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DeviceMasterAlreadyRegisteredWithInstitution);
        }
        entity.IsSoftDeleted = true; entity.IsActive = false; entity.SoftDeletedById = hostUserId; entity.SoftDeletedDateTime = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Soft deleted DeviceMaster {DeviceMasterId} by HostUser {HostUserId}.", entity.Id, hostUserId);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.DeviceMasterDeleted);
    }
}

/// <summary>Handles DeviceMaster retrieval by identifier.</summary>
public sealed class GetDeviceMasterByIdQueryHandler : IRequestHandler<GetDeviceMasterByIdQuery, ApiResponse<DeviceMasterResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService;
    /// <summary>Initializes handler dependencies.</summary>
    public GetDeviceMasterByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; }
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceMasterResponseDTO>> Handle(GetDeviceMasterByIdQuery request, CancellationToken cancellationToken)
    { await _commonRequestService.ValidateHostUserRequestAsync(); var entity = await _unitOfWork.DeviceMasterRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.DeviceMasterNotFound); return ApiResponse<DeviceMasterResponseDTO>.Success(_mapper.Map<DeviceMasterResponseDTO>(entity), AppConstants.SuccessMessages.DeviceMasterRetrieved); }
}

/// <summary>Handles DeviceMaster retrieval by manufacturer serial number.</summary>
public sealed class GetDeviceMasterInfoBySNoQueryHandler : IRequestHandler<GetDeviceMasterInfoBySNoQuery, ApiResponse<DeviceMasterResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService;
    /// <summary>Initializes handler dependencies.</summary>
    public GetDeviceMasterInfoBySNoQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; }
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceMasterResponseDTO>> Handle(GetDeviceMasterInfoBySNoQuery request, CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostUserRequestAsync();
        if (string.IsNullOrWhiteSpace(request.SNo)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var entity = await _unitOfWork.DeviceMasterRepository.GetBySNoAsync(request.SNo, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.DeviceMasterNotFound);
        return ApiResponse<DeviceMasterResponseDTO>.Success(_mapper.Map<DeviceMasterResponseDTO>(entity), AppConstants.SuccessMessages.DeviceMasterRetrieved);
    }
}

/// <summary>Handles database-paged DeviceMaster retrieval.</summary>
public sealed class GetAllDeviceMastersQueryHandler : IRequestHandler<GetAllDeviceMastersQuery, ApiResponse<List<DeviceMasterResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ICommonRequestService _commonRequestService;
    /// <summary>Initializes handler dependencies.</summary>
    public GetAllDeviceMastersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService) { _unitOfWork = unitOfWork; _mapper = mapper; _commonRequestService = commonRequestService; }
    /// <inheritdoc />
    public async Task<ApiResponse<List<DeviceMasterResponseDTO>>> Handle(GetAllDeviceMastersQuery request, CancellationToken cancellationToken)
    { await _commonRequestService.ValidateHostUserRequestAsync(); var page = await _unitOfWork.DeviceMasterRepository.GetPagedAsync(request.Filter ?? new GetDeviceMasterListRequestDTO(), cancellationToken); return ApiResponse<List<DeviceMasterResponseDTO>>.SuccessPaginated(page.Data.Select(x => _mapper.Map<DeviceMasterResponseDTO>(x)).ToList(), page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages, AppConstants.SuccessMessages.DeviceMasterRetrieved); }
}

#endregion
