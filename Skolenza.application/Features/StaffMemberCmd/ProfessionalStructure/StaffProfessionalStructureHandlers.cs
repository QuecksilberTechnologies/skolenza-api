using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.ProfessionalStructure;

#region Requests

public sealed record GetInstitutionStaffCategoriesQuery(bool ActiveOnly, StaffProfessionalPermissionRequestDTO DTO)
    : IRequest<ApiResponse<List<InstitutionStaffCategoryResponseDTO>>>;

public sealed record SaveInstitutionStaffCategoryCommand(InstitutionStaffCategoryRequestDTO DTO)
    : IRequest<ApiResponse<InstitutionStaffCategoryResponseDTO>>;

public sealed record SetStaffCategoryJobFamiliesCommand(StaffCategoryJobFamilyRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record SetInstitutionStaffCategoryActiveCommand(int Id, bool IsActive, StaffProfessionalPermissionRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record GetStaffProfessionalMastersQuery(
    StaffProfessionalMasterType MasterType,
    bool ActiveOnly,
    StaffProfessionalPermissionRequestDTO DTO)
    : IRequest<ApiResponse<List<StaffProfessionalMasterResponseDTO>>>;

public sealed record SaveStaffProfessionalMasterCommand(
    StaffProfessionalMasterType MasterType,
    StaffProfessionalMasterRequestDTO DTO)
    : IRequest<ApiResponse<StaffProfessionalMasterResponseDTO>>;

public sealed record DeleteStaffProfessionalMasterCommand(
    StaffProfessionalMasterType MasterType,
    StaffProfessionalDeleteRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record GetStaffPositionsQuery(
    bool ActiveOnly,
    int? InstitutionStaffCategoryMasterId,
    StaffProfessionalPermissionRequestDTO DTO)
    : IRequest<ApiResponse<List<StaffPositionMasterResponseDTO>>>;

public sealed record SaveStaffPositionCommand(StaffPositionMasterRequestDTO DTO)
    : IRequest<ApiResponse<StaffPositionMasterResponseDTO>>;

public sealed record DeleteStaffPositionCommand(StaffProfessionalDeleteRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record GetStaffProfessionalProfileQuery(StaffProfessionalProfileRequestDTO DTO)
    : IRequest<ApiResponse<StaffProfessionalProfileResponseDTO?>>;

public sealed record VerifyStaffCapabilityCommand(VerifyStaffCapabilityRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetInstitutionStaffCategoriesQueryHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<GetInstitutionStaffCategoriesQuery, ApiResponse<List<InstitutionStaffCategoryResponseDTO>>>
{
    public async Task<ApiResponse<List<InstitutionStaffCategoryResponseDTO>>> Handle(GetInstitutionStaffCategoriesQuery request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.GetInstitutionCategoriesAsync(
            actor.InstitutionId, request.ActiveOnly, cancellationToken);
        return ApiResponse<List<InstitutionStaffCategoryResponseDTO>>.Success(data, "Institution Staff Categories retrieved successfully.");
    }
}

public sealed class SaveInstitutionStaffCategoryCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<SaveInstitutionStaffCategoryCommand, ApiResponse<InstitutionStaffCategoryResponseDTO>>
{
    public async Task<ApiResponse<InstitutionStaffCategoryResponseDTO>> Handle(SaveInstitutionStaffCategoryCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.SaveInstitutionCategoryAsync(
            request.DTO, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<InstitutionStaffCategoryResponseDTO>.Success(data, "Institution Staff Category saved successfully.");
    }
}

public sealed class SetStaffCategoryJobFamiliesCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<SetStaffCategoryJobFamiliesCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(SetStaffCategoryJobFamiliesCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffProfessionalStructureRepository.SetCategoryJobFamiliesAsync(
            request.DTO, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Staff Category Job Families saved successfully.");
    }
}

public sealed class SetInstitutionStaffCategoryActiveCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<SetInstitutionStaffCategoryActiveCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(SetInstitutionStaffCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffProfessionalStructureRepository.SetInstitutionCategoryActiveAsync(
            request.Id, request.IsActive, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, request.IsActive ? "Staff Category activated successfully." : "Staff Category deactivated successfully.");
    }
}

public sealed class GetStaffProfessionalMastersQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStaffProfessionalMastersQuery, ApiResponse<List<StaffProfessionalMasterResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffProfessionalMasterResponseDTO>>> Handle(
        GetStaffProfessionalMastersQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.GetMastersAsync(
            request.MasterType, actor.InstitutionId, request.ActiveOnly, cancellationToken);
        return ApiResponse<List<StaffProfessionalMasterResponseDTO>>.Success(data, "Professional master records retrieved successfully.");
    }
}

public sealed class SaveStaffProfessionalMasterCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<SaveStaffProfessionalMasterCommand, ApiResponse<StaffProfessionalMasterResponseDTO>>
{
    public async Task<ApiResponse<StaffProfessionalMasterResponseDTO>> Handle(
        SaveStaffProfessionalMasterCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.SaveMasterAsync(
            request.MasterType, request.DTO, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<StaffProfessionalMasterResponseDTO>.Success(data, "Professional master saved successfully.");
    }
}

public sealed class DeleteStaffProfessionalMasterCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStaffProfessionalMasterCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteStaffProfessionalMasterCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffProfessionalStructureRepository.DeleteMasterAsync(
            request.MasterType, request.DTO.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Professional master deactivated successfully.");
    }
}

public sealed class GetStaffPositionsQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStaffPositionsQuery, ApiResponse<List<StaffPositionMasterResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffPositionMasterResponseDTO>>> Handle(GetStaffPositionsQuery request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.GetPositionsAsync(
            actor.InstitutionId, request.ActiveOnly, request.InstitutionStaffCategoryMasterId, cancellationToken);
        return ApiResponse<List<StaffPositionMasterResponseDTO>>.Success(data, "Positions retrieved successfully.");
    }
}

public sealed class SaveStaffPositionCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<SaveStaffPositionCommand, ApiResponse<StaffPositionMasterResponseDTO>>
{
    public async Task<ApiResponse<StaffPositionMasterResponseDTO>> Handle(SaveStaffPositionCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.SavePositionAsync(
            request.DTO, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<StaffPositionMasterResponseDTO>.Success(data, "Position saved successfully.");
    }
}

public sealed class DeleteStaffPositionCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStaffPositionCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteStaffPositionCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffProfessionalStructureRepository.DeletePositionAsync(
            request.DTO.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Position deactivated successfully.");
    }
}

public sealed class GetStaffProfessionalProfileQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<GetStaffProfessionalProfileQuery, ApiResponse<StaffProfessionalProfileResponseDTO?>>
{
    public async Task<ApiResponse<StaffProfessionalProfileResponseDTO?>> Handle(GetStaffProfessionalProfileQuery request, CancellationToken cancellationToken)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }
        long staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
            request.DTO.StaffMemberId,
            validation.Claims.InstitutionEncriptionKey,
            idEncoderService);
        var data = await unitOfWork.StaffProfessionalStructureRepository.GetStaffProfileAsync(
            validation.InstitutionId, staffMemberId, cancellationToken);
        return ApiResponse<StaffProfessionalProfileResponseDTO?>.Success(data, "Professional profile retrieved successfully.");
    }
}

public sealed class VerifyStaffCapabilityCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<VerifyStaffCapabilityCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(VerifyStaffCapabilityCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffProfessionalStructureRepository.VerifyCapabilityAsync(
            actor.InstitutionId, request.DTO.AssignmentId, request.DTO.IsApproved,
            request.DTO.Remark, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, request.DTO.IsApproved ? "Capability verified successfully." : "Capability rejected successfully.");
    }
}

internal static class ProfessionalStructureRequestContext
{
    internal static async Task<(long InstitutionId, long StaffMemberId)> ResolveAsync(ICommonRequestService commonRequestService)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.InstitutionId <= 0 || validation.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }
        return (validation.InstitutionId, validation.LoggedInStaffMemberId);
    }
}

#endregion
