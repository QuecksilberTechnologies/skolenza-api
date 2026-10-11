// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Compliance Rule Command Handler requests.
// ================================================================

using skolenza.application.DTOs.Designation;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Compliances.ComplianceRule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace skolenza.application.Features.ComplianceCmd
{

    #region Command

    /// <summary>
    /// Represents the CreateComplianceRuleCommand application component.
    /// </summary>
    public class CreateComplianceRuleCommand
        : IRequest<ApiResponse<GetComplianceRuleResponseDTO>>
    {
        public CreateComplianceRuleRequestDTO DTO { get; set; }

        public CreateComplianceRuleCommand(CreateComplianceRuleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles CreateComplianceRuleCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateComplianceRuleCommandHandler
        : IRequestHandler<CreateComplianceRuleCommand, ApiResponse<GetComplianceRuleResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateComplianceRuleCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public CreateComplianceRuleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateComplianceRuleCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetComplianceRuleResponseDTO>> Handle(
            CreateComplianceRuleCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request");

                var dto = request.DTO;

                // ===============================
                // 2️⃣ BUSINESS VALIDATION
                // ===============================

                //if (dto.EffectiveTo != null && dto.EffectiveFrom > dto.EffectiveTo)
                //    throw new ValidationErrorException("EffectiveFrom cannot be greater than EffectiveTo");

                bool exists = await _unitOfWork.CompilanceRuleRepository
                    .ExistsAsync(
                        dto.ComplianceTypeId,
                        dto.CountryId,
                        dto.StateId,
                        validation.InstitutionId,
                        dto.EffectiveFrom);

                if (exists)
                    throw new ValidationErrorException("Compliance rule already exists for same date");
               // dto.EffectiveFrom.AddDays(-1);
                // ===============================
                // 3️⃣ TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                //  CLOSE OLD RULE (VERSIONING)
                var existingRule = await _unitOfWork.CompilanceRuleRepository
                    .GetRuleAsync(
                        dto.ComplianceTypeId,
                        dto.CountryId,
                        dto.StateId,
                        validation.InstitutionId,
                        dto.EffectiveFrom);
                ComplianceRule complianceRule = new ComplianceRule();
                // A fallback global/state rule belongs to a different scope and must never
                // be shortened when creating a institution-specific version.
                if (existingRule != null &&
                    existingRule.InstitutionId == validation.InstitutionId &&
                    existingRule.StateId == dto.StateId &&
                    existingRule.EffectiveFrom < dto.EffectiveFrom)
                {
                    existingRule.EffectiveTo = dto.EffectiveFrom.AddDays(-1);
                    existingRule.UpdatedDateTime = DateTime.UtcNow;
                    existingRule.UpdatedById = validation.LoggedInStaffMemberId;

                    await _unitOfWork.CompilanceRuleRepository.UpdateAsync(existingRule);
                }

                //  CREATE NEW RULE
                var entity = new ComplianceRule
                {
                    ComplianceTypeId = dto.ComplianceTypeId,
                    CountryId = dto.CountryId,
                    StateId = dto.StateId,
                    InstitutionId = validation.InstitutionId,

                    RuleJson = JsonSerializer.Serialize(dto.RuleJson),

                    Priority = dto.Priority,
                    EffectiveFrom = dto.EffectiveFrom,
                    EffectiveTo = null,

                    IsActive = true,
                    IsSoftDeleted = false,

                    AddedById = validation.LoggedInStaffMemberId,
                    AddedDateTime = DateTime.UtcNow
                };

                await _unitOfWork.CompilanceRuleRepository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "ComplianceRule created | Type: {Type}, Country: {Country}, Institution: {Institution}",
                    dto.ComplianceTypeId,
                    dto.CountryId,
                    validation.InstitutionId);

                // ===============================
                // 4️⃣ RESPONSE
                // ===============================
                var response = new GetComplianceRuleResponseDTO
                {
                    Id = entity.Id,
                    ComplianceTypeId = entity.ComplianceTypeId,
                    CountryId = entity.CountryId,
                    StateId = entity.StateId,
                    InstitutionId = entity.InstitutionId,
                    RuleJson = entity.RuleJson,
                    Priority = entity.Priority ?? 0,
                    EffectiveFrom = entity.EffectiveFrom,
                    EffectiveTo = entity.EffectiveTo

                };

                return ApiResponse<GetComplianceRuleResponseDTO>
                    .Success(response, "Compliance rule created successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error while creating compliance rule");
                throw;
            }
        }
    }
    #endregion
}

