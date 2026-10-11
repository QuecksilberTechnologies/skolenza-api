using System.Globalization;
using System.Text.RegularExpressions;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Configuration.StaffCodeCmd.Handlers;

/// <summary>Previews or confirms a pattern insertion using the existing configuration permission pipeline.</summary>
public sealed record CreateStaffMemberCodePatternCommand(SaveStaffMemberCodePatternRequestDTO DTO)
    : IRequest<ApiResponse<SaveStaffMemberCodePatternResponseDTO>>;

/// <summary>Previews or confirms a pattern change and current staffMember recoding.</summary>
public sealed record UpdateStaffMemberCodePatternCommand(SaveStaffMemberCodePatternRequestDTO DTO)
    : IRequest<ApiResponse<SaveStaffMemberCodePatternResponseDTO>>;

/// <summary>Coordinates pattern validation and transactional repository changes.</summary>
public sealed class SaveStaffMemberCodePatternCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStaffMemberCodePatternCommand, ApiResponse<SaveStaffMemberCodePatternResponseDTO>>,
      IRequestHandler<UpdateStaffMemberCodePatternCommand, ApiResponse<SaveStaffMemberCodePatternResponseDTO>>
{
    #region Command Handlers

    public Task<ApiResponse<SaveStaffMemberCodePatternResponseDTO>> Handle(
        CreateStaffMemberCodePatternCommand request, CancellationToken cancellationToken)
    {
        return SaveAsync(request.DTO, true, cancellationToken);
    }

    public Task<ApiResponse<SaveStaffMemberCodePatternResponseDTO>> Handle(
        UpdateStaffMemberCodePatternCommand request, CancellationToken cancellationToken)
    {
        return SaveAsync(request.DTO, false, cancellationToken);
    }

    #endregion

    #region Validation And Persistence

    private async Task<ApiResponse<SaveStaffMemberCodePatternResponseDTO>> SaveAsync(
        SaveStaffMemberCodePatternRequestDTO dto, bool create, CancellationToken cancellationToken)
    {
        var context = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!context.Success || context.InstitutionId <= 0 || context.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException("A institution staffMember session is required.");
        }

        var operation = await unitOfWork.OperationRepository.GetOperationByIdAsync(dto.OperationId);
        var expected = create ? OperationType.Add : OperationType.Update;
        if (operation?.IsActive != true || operation.OperationType != (int)expected)
        {
            throw new ForbiddenAccessException("The requested action requires its corresponding Add or Update permission.");
        }

        var input = dto.Pattern ?? throw new ValidationErrorException("Pattern is required.");
        var prefix = input.Prefix?.Trim().ToUpperInvariant() ?? string.Empty;
        var separator = input.Separator?.Trim() ?? string.Empty;
        // Preserve the established institution-create/update validation contract.
        if (prefix.Length is < 1 or > 10 || !Regex.IsMatch(prefix, "^[A-Z]+$") ||
            separator is not ("_" or "/" or "-") ||
            !int.TryParse(input.RunningNumberLength, NumberStyles.None, CultureInfo.InvariantCulture, out var digits) ||
            digits is < 3 or > 7)
        {
            throw new ValidationErrorException("Use a 1–10 letter prefix, separator _, / or -, and running number length 3–7.");
        }

        var pattern = new StaffMemberCodePattern
        {
            Prefix = prefix,
            Separator = separator,
            RunningNumberLength = digits,
            IncludeYear = input.IncludeYear,
            IncludeMonth = input.IncludeMonth,
            IncludeDepartment = input.IncludeDepartment
        };
        var result = await unitOfWork.InstitutionStaffCodePatternRepository.SaveWithStaffMemberCodesAsync(
            context.InstitutionId, context.LoggedInStaffMemberId, pattern,
            create, dto.Confirm, dto.PreviewHash, cancellationToken);
        return new ApiResponse<SaveStaffMemberCodePatternResponseDTO>
        {
            IsSucceeded = true,
            Data = result,
            Message = result.Applied
                ? "Pattern and staffMember codes updated."
                : "Review all proposed staffMember codes. Nothing has been changed."
        };
    }

    #endregion
}
