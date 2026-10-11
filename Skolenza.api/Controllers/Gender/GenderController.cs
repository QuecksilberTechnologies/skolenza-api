// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Gender operations.
// ================================================================

using skolenza.api.Controllers.Leave;
using skolenza.application.DTOs.Gender;
using skolenza.application.DTOs.Leave;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Gender;
using skolenza.application.Features.GenderCmd.Handlers;
using skolenza.application.Features.GenderCmd.Queries;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Features.LeaveCmd.Queries;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.Gender
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GenderController> _logger;

        public GenderController(IMediator mediator, ILogger<GenderController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: retrieves gender options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves gender option.</para>
        /// <para>Handler flow: GetGenderOptionQuery is processed by GetGenderOptionQueryHandler; operation(s): GetOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetGenderOptionResponseDTO: Id (int), GenderName (string)</para>
        /// <para>Angular function(s): UsersApi.getGenderOptions (app/core/services/users-api.ts:47).</para>
        /// <para>Angular purpose: retrieves gender options.</para>
        /// <para>Integrated UI page(s): /auth/register-institution; /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); Registration (app/features/authentication/registration/registration.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
        /// </remarks>
        [HttpGet("option")]
        public async Task<IActionResult> getGender([FromQuery] GetOptionRequestDTO requestDTO)
        {
            _logger.LogInformation($"Received request to get Gender : {requestDTO.UserStaffMemberId}");

            var command = new GetGenderOptionQuery(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        //  ✅ Get All Gender
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all gender.</para>
                /// <para>Handler flow: GetAllGenderQuery is processed by GetAllGenderQueryHandler; operation(s): GetAllAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetGenderResponseDTO: Id (int), GenderName (string)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/gender/get.</para>
                /// </remarks>
                [HttpGet("get")]
                public async Task<IActionResult> GetAllGenderAsync([FromQuery] GetGenderRequestDTO? getGenderRequestDTO)
                {
                    _logger.LogInformation("Fetching all LeavePolicies...");
                    var query = new GetAllGenderQuery(getGenderRequestDTO);
                    var result = await _mediator.Send(query);
                    return Ok(result);
                }


    }
}
