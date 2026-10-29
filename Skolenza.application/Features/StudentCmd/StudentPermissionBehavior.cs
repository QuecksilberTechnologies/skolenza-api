using System.Reflection;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

public sealed class StudentPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith("skolenza.application.Features.StudentCmd", StringComparison.Ordinal) != true)
        {
            return await next();
        }

        var permission = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.GetValue(request))
            .OfType<PermissionRequestDTO>()
            .FirstOrDefault()
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);

        if (permission.ModuleId <= 0 || permission.OperationId <= 0)
        {
            throw new ValidationErrorException("ModuleId and OperationId are required.");
        }

        var requiredPermission = ResolvePermission(request);
        var moduleCode = await commonRequestService.GetModuleCodeAsync(permission.ModuleId);
        if (!string.Equals(moduleCode, requiredPermission.ModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var operation = (await unitOfWork.OperationRepository.GetAllOperationAsync())
            .FirstOrDefault(item => item.Id == permission.OperationId && item.IsActive);
        if (!string.Equals(operation?.OperationName?.Trim(), requiredPermission.OperationName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0 || actor.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var allowed = await unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(
            actor.InstitutionId,
            actor.LoggedInStaffMemberId,
            actor.RoleId,
            permission.ModuleId,
            permission.OperationId,
            cancellationToken);
        InstitutionRuntimePermissionValidator.EnsureAllowed(allowed);
        return await next();
    }

    private static (string ModuleCode, string OperationName) ResolvePermission(TRequest request) => request switch
    {
        GetStudentDocumentRequirementsQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentAdmissionDocumentsQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentAdmissionDocumentDownloadQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateStudentDocumentRequirementCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UploadStudentAdmissionDocumentCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateStudentDocumentRequirementCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        ReviewStudentAdmissionDocumentCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        ReviewStudentRecordDocumentCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        DeleteStudentDocumentRequirementCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        GetStudentFeeFrequenciesQuery => (StudentConstants.FeesModuleCode, "View"),
        CreateStudentFeeFrequencyCommand => (StudentConstants.FeesModuleCode, "Add"),
        UpdateStudentFeeFrequencyCommand => (StudentConstants.FeesModuleCode, "Update"),
        DeleteStudentFeeFrequencyCommand => (StudentConstants.FeesModuleCode, "Delete"),
        GetStudentFeeComponentsQuery => (StudentConstants.FeesModuleCode, "View"),
        CreateStudentFeeComponentCommand => (StudentConstants.FeesModuleCode, "Add"),
        UpdateStudentFeeComponentCommand => (StudentConstants.FeesModuleCode, "Update"),
        DeleteStudentFeeComponentCommand => (StudentConstants.FeesModuleCode, "Delete"),
        GetStudentFeeStructuresQuery => (StudentConstants.FeesModuleCode, "View"),
        GetStudentFeeQuotesQuery => (StudentConstants.FeesModuleCode, "View"),
        GetStudentAdmissionFeeBalanceQuery => (StudentConstants.FeesModuleCode, "View"),
        GetStudentFeeReceiptsQuery => (StudentConstants.FeesModuleCode, "View"),
        GetStudentPaymentGatewayConfigurationQuery => (StudentConstants.FeesModuleCode, "View"),
        ReconcileStudentCashfreePaymentCommand => (StudentConstants.FeesModuleCode, "Update"),
        CreateStudentFeeStructureCommand => (StudentConstants.FeesModuleCode, "Add"),
        CreateStudentAdmissionFeeQuoteCommand => (StudentConstants.FeesModuleCode, "Add"),
        RecordStudentOfflineFeePaymentCommand => (StudentConstants.FeesModuleCode, "Add"),
        CreateStudentPaymentGatewayConfigurationCommand => (StudentConstants.FeesModuleCode, "Add"),
        CreateStudentCashfreeCheckoutCommand => (StudentConstants.FeesModuleCode, "Add"),
        UpdateStudentPaymentGatewayConfigurationCommand => (StudentConstants.FeesModuleCode, "Update"),
        UpdateStudentFeeStructureCommand => (StudentConstants.FeesModuleCode, "Update"),
        DeactivateStudentFeeStructureCommand => (StudentConstants.FeesModuleCode, "Delete"),
        GetStudentBenefitSchemesQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateStudentBenefitSchemeCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateStudentBenefitSchemeCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        DeleteStudentBenefitSchemeCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        GetStudentEducationIdentifierMastersQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateStudentEducationIdentifierMasterCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateStudentEducationIdentifierMasterCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        DeleteStudentEducationIdentifierMasterCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        GetStudentEducationIdentifiersQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateStudentEducationIdentifierCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateStudentEducationIdentifierCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        SetStudentEducationIdentifierVerificationCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        GetStudentsQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentExitHistoryQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentOptionsQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetInstitutionPortalDomainsQuery => (StudentConstants.PortalDomainsModuleCode, "View"),
        CreateInstitutionPortalDomainCommand => (StudentConstants.PortalDomainsModuleCode, "Add"),
        RenewInstitutionPortalDomainChallengeCommand => (StudentConstants.PortalDomainsModuleCode, "Update"),
        VerifyInstitutionPortalDomainCommand => (StudentConstants.PortalDomainsModuleCode, "Update"),
        DeleteInstitutionPortalDomainCommand => (StudentConstants.PortalDomainsModuleCode, "Delete"),
        GetGuardiansQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentGuardiansQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetAdmissionGuardiansQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        LinkGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateGuardianLinkCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        RemoveGuardianLinkCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        AddAdmissionGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateAdmissionGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        RemoveAdmissionGuardianCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        GetStudentAdmissionsQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentAdmissionQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        GetStudentAdmissionStatusHistoryQuery => (StudentConstants.AdmissionsModuleCode, "View"),
        CreateStudentAdmissionCommand => (StudentConstants.AdmissionsModuleCode, "Add"),
        UpdateStudentAdmissionCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        ReviewStudentAdmissionCommand => (StudentConstants.AdmissionsModuleCode, "Update"),
        ExitStudentCommand => (StudentConstants.AdmissionsModuleCode, "Delete"),
        _ => throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied)
    };
}
