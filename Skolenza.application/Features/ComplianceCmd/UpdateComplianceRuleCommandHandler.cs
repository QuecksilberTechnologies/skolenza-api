using skolenza.application.DTOS.Compliances.ComplianceRule;
using skolenza.application.Exceptions;
using skolenza.application.Features.ComplianceCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Constants;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Features.ComplianceCmd
{
    public class UpdateComplianceRuleCommand : IRequest<ApiResponse<UpdateComplianceRuleReponseDTO>>
    {
        public required UpdateComplianceRuleRequestDTO DTO { get; set; }
    }
}
    public class UpdateComplianceRuleCommandHandler: IRequestHandler<UpdateComplianceRuleCommand, ApiResponse<UpdateComplianceRuleReponseDTO>>
    {
    //    private readonly IUnitOfWork _unitOfWork;
    //    private readonly ILogger<UpdateComplianceRuleCommandHandler> _logger;
    //    private readonly ICommonRequestService _commonRequestService;

    //    public UpdateComplianceRuleCommandHandler(
    //        IUnitOfWork unitOfWork,
    //        ILogger<UpdateComplianceRuleCommandHandler> logger,
    //        ICommonRequestService commonRequestService)
    //    {
    //        _unitOfWork = unitOfWork;
    //        _logger = logger;
    //        _commonRequestService = commonRequestService;
    //    }

    //    public async Task<ApiResponse<UpdateComplianceRuleReponseDTO>> Handle(
    //        UpdateComplianceRuleCommand request,
    //        CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
    //            if (!validation.Success)
    //                throw new UnauthorizedAccessException(validation.ErrorMessage);

    //            if (request?.DTO == null)
    //                throw new ValidationErrorException("Invalid request");

              

    //            var entity = await _unitOfWork.CompilanceRuleRepository
    //                .GetByIdAsync(id);

    //            if (entity == null)
    //                throw new NotFoundException("Compliance rule not found");

    //            // 🔥 Update fields
    //            entity.ComplianceTypeId = dto.ComplianceTypeId;
    //            entity.CountryId = dto.CountryId;
    //            entity.StateId = dto.StateId;
    //            entity.RuleJson = dto.RuleJson;
    //            entity.Priority = dto.Priority;
    //            entity.InstitutionId = dto.InstitutionId;
    //            entity.EffectiveFrom = dto.EffectiveFrom;
    //            entity.EffectiveTo = dto.EffectiveTo;
    //            entity.IsActive = dto.IsActive;

    //            entity.UpdatedById = validation.UserStaffMemberId;
    //            entity.UpdatedDateTime = DateTime.UtcNow;

    //            await _unitOfWork.ComplianceRuleRepository.UpdateAsync(entity);
    //            await _unitOfWork.SaveChangesAsync();

    //            var response = new UpdateComplianceRuleResponseDTO
    //            {
    //                Id = dto.Id,
    //                ComplianceTypeId = entity.ComplianceTypeId,
    //                CountryId = entity.CountryId,
    //                StateId = entity.StateId,
    //                RuleJson = entity.RuleJson,
    //                Priority = entity.Priority,
    //                InstitutionId = entity.InstitutionId,
    //                EffectiveFrom = entity.EffectiveFrom,
    //                EffectiveTo = entity.EffectiveTo,
    //                IsActive = entity.IsActive
    //            };

    //            return ApiResponse<UpdateComplianceRuleResponseDTO>.Success(response);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error updating ComplianceRule");
    //            throw;
    //        }
    //    }
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateComplianceRuleCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateComplianceRuleCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateComplianceRuleCommandHandler> logger, ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<UpdateComplianceRuleReponseDTO>> Handle(UpdateComplianceRuleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success) throw new UnauthorizedAccessException(validation.ErrorMessage);
            if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin)
                throw new UnauthorizedAccessException("Only institution administrators can update compliance rules");
            if (request?.DTO == null || request.DTO.Id <= 0) throw new ValidationErrorException("Invalid compliance rule request");
            if (request.DTO.EffectiveTo.HasValue && request.DTO.EffectiveFrom > request.DTO.EffectiveTo.Value)
                throw new ValidationErrorException("EffectiveFrom cannot be greater than EffectiveTo");

            var entity = await _unitOfWork.CompilanceRuleRepository.GetByIdAsync(request.DTO.Id);
            if (entity == null || (entity.InstitutionId.HasValue && entity.InstitutionId != validation.InstitutionId))
                throw new NotFoundException("Compliance rule not found");

            entity.ComplianceTypeId = request.DTO.ComplianceTypeId;
            entity.CountryId = request.DTO.CountryId;
            entity.StateId = request.DTO.StateId;
            entity.RuleJson = System.Text.Json.JsonSerializer.Serialize(request.DTO.RuleJson);
            entity.Priority = request.DTO.Priority;
            entity.EffectiveFrom = request.DTO.EffectiveFrom;
            entity.EffectiveTo = request.DTO.EffectiveTo;
            entity.IsActive = request.DTO.IsActive;
            entity.UpdatedById = validation.LoggedInStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;

            await _unitOfWork.CompilanceRuleRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<UpdateComplianceRuleReponseDTO>.Success(new UpdateComplianceRuleReponseDTO
            {
                Id = entity.Id, ComplianceTypeId = entity.ComplianceTypeId, CountryId = entity.CountryId,
                StateId = entity.StateId, RuleJson = entity.RuleJson, Priority = entity.Priority ?? 0,
                InstitutionId = entity.InstitutionId, EffectiveFrom = entity.EffectiveFrom, EffectiveTo = entity.EffectiveTo,
                IsActive = entity.IsActive
            }, "Compliance rule updated successfully");
        }
    }

