using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;

#region Requests

public sealed record GetAcademicMastersQuery(AcademicMasterType MasterType, bool ActiveOnly, AcademicStructurePagedRequestDTO DTO)
    : IRequest<ApiResponse<List<AcademicMasterResponseDTO>>>;

public sealed record GetTeachingAssignmentTypesQuery(AcademicStructurePermissionRequestDTO DTO)
    : IRequest<ApiResponse<List<TeachingAssignmentTypeOptionDTO>>>;

public sealed record SaveAcademicMasterCommand(AcademicMasterType MasterType, AcademicMasterRequestDTO DTO)
    : IRequest<ApiResponse<AcademicMasterResponseDTO>>;

public sealed record DeleteAcademicMasterCommand(AcademicMasterType MasterType, AcademicMasterDeleteRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record GetStaffAcademicProfileQuery(StaffAcademicProfileRequestDTO DTO)
    : IRequest<ApiResponse<StaffAcademicProfileResponseDTO>>;

public sealed record SaveStaffTeachingAssignmentCommand(StaffTeachingAssignmentRequestDTO DTO)
    : IRequest<ApiResponse<StaffTeachingAssignmentResponseDTO>>;

public sealed record SaveClassTeacherAssignmentCommand(ClassTeacherAssignmentRequestDTO DTO)
    : IRequest<ApiResponse<ClassTeacherAssignmentResponseDTO>>;

public sealed record DeleteStaffTeachingAssignmentCommand(long Id, AcademicStructurePermissionRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record DeleteClassTeacherAssignmentCommand(long Id, AcademicStructurePermissionRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

#endregion

#region Handlers

public sealed class GetTeachingAssignmentTypesQueryHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<GetTeachingAssignmentTypesQuery, ApiResponse<List<TeachingAssignmentTypeOptionDTO>>>
{
    public async Task<ApiResponse<List<TeachingAssignmentTypeOptionDTO>>> Handle(GetTeachingAssignmentTypesQuery request, CancellationToken cancellationToken)
    {
        await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffAcademicStructureRepository.GetTeachingAssignmentTypesAsync(cancellationToken);
        return ApiResponse<List<TeachingAssignmentTypeOptionDTO>>.Success(data, "Teaching assignment types retrieved successfully.");
    }
}

public sealed class GetAcademicMastersQueryHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<GetAcademicMastersQuery, ApiResponse<List<AcademicMasterResponseDTO>>>
{
    public async Task<ApiResponse<List<AcademicMasterResponseDTO>>> Handle(GetAcademicMastersQuery request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var page = await unitOfWork.StaffAcademicStructureRepository.GetMastersAsync(
            request.MasterType,
            actor.InstitutionId,
            request.ActiveOnly,
            request.DTO.PageNumber,
            request.DTO.PageSize,
            cancellationToken);
        return ApiResponse<List<AcademicMasterResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Academic records retrieved successfully.");
    }
}

public sealed class SaveAcademicMasterCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<SaveAcademicMasterCommand, ApiResponse<AcademicMasterResponseDTO>>
{
    public async Task<ApiResponse<AcademicMasterResponseDTO>> Handle(SaveAcademicMasterCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        var data = await unitOfWork.StaffAcademicStructureRepository.SaveMasterAsync(
            request.MasterType,
            request.DTO,
            actor.InstitutionId,
            actor.StaffMemberId,
            cancellationToken);
        return ApiResponse<AcademicMasterResponseDTO>.Success(data, "Academic record saved successfully.");
    }
}

public sealed class DeleteAcademicMasterCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteAcademicMasterCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteAcademicMasterCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffAcademicStructureRepository.DeleteMasterAsync(
            request.MasterType,
            request.DTO.Id,
            actor.InstitutionId,
            actor.StaffMemberId,
            cancellationToken);
        return ApiResponse<bool>.Success(true, "Academic record deleted successfully.");
    }
}

public sealed class GetStaffAcademicProfileQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<GetStaffAcademicProfileQuery, ApiResponse<StaffAcademicProfileResponseDTO>>
{
    public async Task<ApiResponse<StaffAcademicProfileResponseDTO>> Handle(GetStaffAcademicProfileQuery request, CancellationToken cancellationToken)
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
        var data = await unitOfWork.StaffAcademicStructureRepository.GetStaffProfileAsync(
            validation.InstitutionId,
            staffMemberId,
            cancellationToken);
        return ApiResponse<StaffAcademicProfileResponseDTO>.Success(data, "Staff academic profile retrieved successfully.");
    }
}

public sealed class SaveStaffTeachingAssignmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<SaveStaffTeachingAssignmentCommand, ApiResponse<StaffTeachingAssignmentResponseDTO>>
{
    public async Task<ApiResponse<StaffTeachingAssignmentResponseDTO>> Handle(SaveStaffTeachingAssignmentCommand request, CancellationToken cancellationToken)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }
        long staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(request.DTO.StaffMemberId, validation.Claims.InstitutionEncriptionKey, idEncoderService);
        var data = await unitOfWork.StaffAcademicStructureRepository.SaveTeachingAssignmentAsync(
            request.DTO,
            validation.InstitutionId,
            staffMemberId,
            validation.LoggedInStaffMemberId,
            cancellationToken);
        return ApiResponse<StaffTeachingAssignmentResponseDTO>.Success(data, "Teaching assignment saved successfully.");
    }
}

public sealed class SaveClassTeacherAssignmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<SaveClassTeacherAssignmentCommand, ApiResponse<ClassTeacherAssignmentResponseDTO>>
{
    public async Task<ApiResponse<ClassTeacherAssignmentResponseDTO>> Handle(SaveClassTeacherAssignmentCommand request, CancellationToken cancellationToken)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }
        long staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(request.DTO.StaffMemberId, validation.Claims.InstitutionEncriptionKey, idEncoderService);
        var data = await unitOfWork.StaffAcademicStructureRepository.SaveClassTeacherAssignmentAsync(
            request.DTO,
            validation.InstitutionId,
            staffMemberId,
            validation.LoggedInStaffMemberId,
            cancellationToken);
        return ApiResponse<ClassTeacherAssignmentResponseDTO>.Success(data, "Class teacher assignment saved successfully.");
    }
}

public sealed class DeleteStaffTeachingAssignmentCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteStaffTeachingAssignmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteStaffTeachingAssignmentCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffAcademicStructureRepository.DeleteTeachingAssignmentAsync(request.Id, actor.InstitutionId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Teaching assignment deleted successfully.");
    }
}

public sealed class DeleteClassTeacherAssignmentCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteClassTeacherAssignmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteClassTeacherAssignmentCommand request, CancellationToken cancellationToken)
    {
        var actor = await ProfessionalStructureRequestContext.ResolveAsync(commonRequestService);
        await unitOfWork.StaffAcademicStructureRepository.DeleteClassTeacherAssignmentAsync(request.Id, actor.InstitutionId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Class teacher assignment deleted successfully.");
    }
}

#endregion
