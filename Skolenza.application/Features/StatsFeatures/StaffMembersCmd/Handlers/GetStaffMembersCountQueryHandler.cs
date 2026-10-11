// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles GetStaffMembersCountQueryHandler requests using authenticated institution context.
// ================================================================

using skolenza.application.DTOS.StoreProcedures.DashboardSummeries;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StatsFeatures.StaffMembersCmd.Handlers
{


    public class GetStaffMemberCountsQuery : IRequest<ApiResponse<StaffMemberCountResponseStatsSp>>
    {
        public StaffMemberCountRequestStatsSp DTO { get; }

        public GetStaffMemberCountsQuery(StaffMemberCountRequestStatsSp dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
    public class GetStaffMembersCountQueryHandler: IRequestHandler<GetStaffMemberCountsQuery, ApiResponse<StaffMemberCountResponseStatsSp>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStaffMembersCountQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public GetStaffMembersCountQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStaffMembersCountQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<StaffMemberCountResponseStatsSp>> Handle(
    GetStaffMemberCountsQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 GetStaffMemberCounts started");

                // ===============================
                // 1️⃣ VALIDATION (AUTH)
                // ===============================
                #region Institution Request Validation
                var validation = await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();
                #endregion

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ PERMISSION CHECK (RBAC)
                // ===============================
                //var hasAccess = await _permissionService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.StaffMember,
                //    Operations.View);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("Access denied.");

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

                // ===============================
                // 4️⃣ FETCH SP DATA
                // ===============================
                var result = await _unitOfWork
                    .StoreProcedureRepository
                    .GetStaffMemberCountsAsync(validation.InstitutionId);

                if (result == null)
                {
                    _logger.LogInformation("⚠️ No staffMember stats found for InstitutionId {InstitutionId}", validation.InstitutionId);

                    return ApiResponse<StaffMemberCountResponseStatsSp>
                        .Success(null, "No data found.");
                }

                _logger.LogInformation("✅ StaffMember stats retrieved successfully");

                // ===============================
                // 5️⃣ SUCCESS
                // ===============================
                return ApiResponse<StaffMemberCountResponseStatsSp>
                    .Success(result, "StaffMember statistics retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GetStaffMemberCounts failed");

                throw; // ✅ CRITICAL
            }
        }
    }








}
