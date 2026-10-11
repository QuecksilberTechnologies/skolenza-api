// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for User Role operations.
// ================================================================

using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.UserRoles;
using skolenza.application.Features.RoleCmd.ModuleOperationMappingRepository.Handlers;
using skolenza.application.Features.UserRolesCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.UserRole;

[ApiController]
[Route("api/[controller]")]
public class UserRoleController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerService _logger;  // Logger service ka declaration

    public UserRoleController(IMediator mediator, ILoggerService logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    /// <summary>
    /// Used-In-Angular: assigns or maps roles to user.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: assigns or maps user roles.</para>
    /// <para>Handler flow: AssignUserRolesCommand is processed by AssignUserRolesCommandHandler; operation(s): GetStaffMemberRolesWithDetailsByIdAsync, UpdateRange, AddRangeAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); UserRoleDTO: RoleName (string?), UserRoleId (long), RoleId (int), IsActive (bool), IsPrimaryRole (bool), RoleType (int)</para>
    /// <para>Angular function(s): RolesApi.assignRolesToUser (app/core/services/roles-api.ts:172).</para>
    /// <para>Angular purpose: assigns or maps roles to user.</para>
    /// <para>Integrated UI page(s): /app/staffMembers</para>
    /// <para>Angular UI component(s): StaffMembersStore (app/features/staffMembers/staffMembers.store.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
    /// </remarks>

    [HttpPost("assign-roles-to-user")]

    // [Authorize]
    public async Task<IActionResult> CreatePermission([FromBody] UserRoleListDTO insertRoleModulePermissionsRequestDTO)
    {
        _logger.LogInfo("Received request for update a new role" + insertRoleModulePermissionsRequestDTO.ToString());
        var command = new AssignUserRolesCommand(insertRoleModulePermissionsRequestDTO);
        var result = await _mediator.Send(command);

        return Ok(result);
    }
    /// <summary>
    /// Used-In-Angular: retrieves all user roles.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all user roles.</para>
    /// <para>Handler flow: GetAllUserRolesCommand is processed by GetAllUserRolesCommandHandler; operation(s): GetStaffMemberRolesWithDetailsByIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); UserRoleDTO: RoleName (string?), UserRoleId (long), RoleId (int), IsActive (bool), IsPrimaryRole (bool), RoleType (int)</para>
    /// <para>Angular function(s): RolesApi.getAllUserRoles (app/core/services/roles-api.ts:166).</para>
    /// <para>Angular purpose: retrieves all user roles.</para>
    /// <para>Integrated UI page(s): /app/staffMembers</para>
    /// <para>Angular UI component(s): RolePopup (app/features/staffMembers/role-popup/role-popup.ts); StaffMemberRoleCell (app/features/staffMembers/staffMember-role-cell/staffMember-role-cell.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
    /// </remarks>
    [HttpGet("get-all-user-roles")]
    public async Task<IActionResult> GetInstitutionEnabledOperations([FromQuery] GetUserRoleRequestDTO dTO)
    {
        _logger.LogInfo($"Getting all user roles: {dTO}");

        var query = new GetAllUserRolesCommand(dTO);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}
