using skolenza.application.Constants;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers;

#region Request and Response

/// <summary>Configures the five operational sections for all current and future institution staffMembers.</summary>
public sealed class UpdateStaffMemberSectionDefaultsRequestDTO : PermissionRequestDTO
{
    public bool IsEditAllowed { get; set; }
}

public sealed class StaffMemberSectionDefaultResponseDTO
{
    public string ModuleCode { get; set; } = string.Empty;
    public bool IsEditAllowed { get; set; }
}

public sealed class UpdateStaffMemberSectionDefaultsCommand(UpdateStaffMemberSectionDefaultsRequestDTO dto)
    : IRequest<ApiResponse<List<StaffMemberSectionDefaultResponseDTO>>>
{
    public UpdateStaffMemberSectionDefaultsRequestDTO DTO { get; } = dto;
}

#endregion

#region Handler

/// <summary>Uses the StaffMember permission pipeline and trusted institution context to update defaults atomically.</summary>
public sealed class UpdateStaffMemberSectionDefaultsCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStaffMemberSectionDefaultsCommand, ApiResponse<List<StaffMemberSectionDefaultResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffMemberSectionDefaultResponseDTO>>> Handle(
        UpdateStaffMemberSectionDefaultsCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin)
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

        var operation = await unitOfWork.OperationRepository.GetOperationByIdAsync(request.DTO.OperationId);
        if (operation?.IsActive != true || operation.OperationType != (int)OperationType.Update)
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var result = new List<StaffMemberSectionDefaultResponseDTO>();
            foreach (var moduleCode in StaffMemberOperationalSections.ModuleCodes)
            {
                await unitOfWork.StaffMembers.SetOperationalSectionDefaultAsync(
                    validation.InstitutionId,
                    moduleCode,
                    request.DTO.IsEditAllowed,
                    validation.LoggedInStaffMemberId,
                    cancellationToken);
                result.Add(new StaffMemberSectionDefaultResponseDTO
                {
                    ModuleCode = moduleCode,
                    IsEditAllowed = request.DTO.IsEditAllowed
                });
            }

            await unitOfWork.CommitTransactionAsync();
            return ApiResponse<List<StaffMemberSectionDefaultResponseDTO>>.Success(
                result, "StaffMember operational section defaults updated.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}

#endregion
