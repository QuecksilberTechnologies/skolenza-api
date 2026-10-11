// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Stat Data operations.
// ================================================================

using skolenza.application.DTOS.StoreProcedures.DashboardSummeries;
using skolenza.application.Features.StatsFeatures.StaffMembersCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace skolenza.api.Controllers.Stats
{
    /// <summary>
    /// Controller to manage all Asset Category related operations
    /// for Institution Admins (Add, Update, Delete, GetAll).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatDataController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public StatDataController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: retrieves staff member dashboard statistics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves staff member counts.</para>
        /// <para>Handler flow: GetStaffMemberCountsQuery is processed by GetStaffMembersCountQueryHandler; operation(s): GetStaffMemberCountsAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.getStaffMemberStatistics (app/core/services/staff-member-api.ts).</para>
        /// <para>Angular purpose: retrieves staff member statistics.</para>
        /// <para>Integrated UI page(s): /app/admin-dashboard; /app/staff</para>
        /// <para>Angular UI component(s): DashboardAdmin; StaffMembersStore; StaffMembers.</para>
        /// </remarks>
        [HttpGet("Dashboard/StaffMembers/Statistics")]
        public async Task<IActionResult> GetStaffMemberDashboardSummaryAsync(
            [FromQuery] StaffMemberCountRequestStatsSp dto)
        {
                var command = new GetStaffMemberCountsQuery(dto);
                var result = await _mediator.Send(command);
               _logger.LogInfo("Staff member statistics retrieved successfully.");
                return Ok(result);

        }



        /// <summary>
        /// Used-In-Angular: retrieves dashboard statistics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: performs the Angular function dashboard.</para>
        /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
        /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
        /// <para>Angular function(s): DashboardApi.getDashboardStatistics (app/core/services/dashboard-api.ts:59).</para>
        /// <para>Angular purpose: retrieves dashboard statistics.</para>
        /// <para>Integrated UI page(s): /app/admin-dashboard; /app/setting/billing; /app/assets/list; /app/staffMembers</para>
        /// <para>Angular UI component(s): DashboardAdmin (app/features/dashboard/dashboard-admin/dashboard-admin.ts); SubscriptionStore (app/features/subscription/subscription.store.ts); Subscription (app/features/subscription/subscription.ts); CdkCloseMenuOnScroll (app/shared/directives/cdk-close-menu-on-scroll.ts); AssetsManagement (app/features/assets-management/assets-management.ts); StaffMemberRoleCell (app/features/staffMembers/staffMember-role-cell/staffMember-role-cell.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [HttpGet("Manager/Statistics/Dashboard/get")]
        public async Task<IActionResult> dashboard([FromQuery] GetSummaryRequestDTO request)
        {
            StaffMemberStats staffMember = new StaffMemberStats()
            {
                TotalStaffMembers = 4,
                NewHiresThisMonth = 1,
                OpenPositions = 10,
                PendingApprovals = 3
            };

                return Ok(staffMember);

        }


        /// <summary>
        /// Used-In-Angular: retrieves asset statistics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: performs the Angular function asset.</para>
        /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
        /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
        /// <para>Angular function(s): DashboardApi.getAssetStatistics (app/core/services/dashboard-api.ts:66).</para>
        /// <para>Angular purpose: retrieves asset statistics.</para>
        /// <para>Integrated UI page(s): /app/assets/list</para>
        /// <para>Angular UI component(s): AssetsManagementStore (app/features/assets-management/assets-management.store.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpGet("Manager/Statistic/Asset")]
        public async Task<IActionResult> Asset([FromQuery] GetSummaryRequestDTO request)
        {
            AssetStats assetStats = new AssetStats()
            {
                TotalAssets = 12,
                AssignedAssets = 0,
                AvailableAssets = 18,
                UnderMaintenance = 9
            };


                return Ok(assetStats);

        }








    }
}
