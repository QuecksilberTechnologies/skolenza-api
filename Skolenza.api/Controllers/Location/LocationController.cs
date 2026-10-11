// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Location operations.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Designation;
using skolenza.application.DTOS.Gender;
using skolenza.application.DTOS.Location;
using skolenza.application.DTOS.Role;
using skolenza.application.Features.DepartmentCmd.Handlers;
using skolenza.application.Features.DesignationCmd.Handlers;
using skolenza.application.Features.GenderCmd.Handlers;
using skolenza.application.Features.LocationCmd.Handlers;
using skolenza.application.Features.OperationCmd.Commands;
using skolenza.application.Features.OperationCmd.Queries;
using skolenza.application.Features.RoleCmd.Handlers;
using skolenza.application.Features.TransportCmd.Commands;
using skolenza.application.Features.TransportCmd.Queries;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Location
{
    /// <summary>
    /// handled-DDL/Option-related-actions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public LocationController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        /// <summary>
        /// Used-In-Angular: retrieves countries.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves country.</para>
        /// <para>Handler flow: GetCountryQuery is processed by GetCountryQueryHandler; operation(s): GetCountryOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetCountryOptionResponseDTO: Id (int), CountryName (string), CountryCode (string), STDCode (string), IsActive (bool?)</para>
        /// <para>Angular function(s): LocationsApi.getCountries (app/core/services/locations-api.ts:63).</para>
        /// <para>Angular purpose: retrieves countries.</para>
        /// <para>Integrated UI page(s): /auth/register-institution; /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); Registration (app/features/authentication/registration/registration.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
        /// </remarks>
        [HttpGet("country/option")]
        public async Task<IActionResult> getCountry([FromQuery] GetCountryOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get Country : {requestDTO.UserStaffMemberId}");

            var command = new GetCountryQuery(requestDTO);
            var result = await _mediator.Send(command);
            return Ok(result);        }

        /// <summary>
        /// Used-In-Angular: retrieves states.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves state.</para>
        /// <para>Handler flow: GetStateQuery is processed by GetStateQueryHandler; operation(s): GetStateOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStateOptionResponseDTO: Id (int), CountryId (int), CountryCode (string), StateName (string), STDCode (string), IsActive (bool?)</para>
        /// <para>Angular function(s): LocationsApi.getStates (app/core/services/locations-api.ts:70).</para>
        /// <para>Angular purpose: retrieves states.</para>
        /// <para>Integrated UI page(s): /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit; /app/campuss</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); StaffMemberContactForm (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-form/staffMember-contact-form.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
        /// </remarks>
        [HttpGet("State/option")]
        public async Task<IActionResult> getState([FromQuery] GetStateOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get State : {requestDTO.UserStaffMemberId}");
            var command = new GetStateQuery(requestDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        /// <summary>
        /// Used-In-Angular: retrieves districts.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves district.</para>
        /// <para>Handler flow: GetDistrictQuery is processed by GetDistrictQueryHandler; operation(s): GetDistrictOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDistrictOptionResponseDTO: Id (int), StateId (int), CountryCode (string), STDCode (string), DistrictName (string), IsActive (bool?)</para>
        /// <para>Angular function(s): LocationsApi.getDistricts (app/core/services/locations-api.ts:77).</para>
        /// <para>Angular purpose: retrieves districts.</para>
        /// <para>Integrated UI page(s): /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit; /app/campuss</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); StaffMemberContactForm (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-form/staffMember-contact-form.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
        /// </remarks>
        [HttpGet("District/option")]
        public async Task<IActionResult> getDistrict([FromQuery] GetDistrictOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get District : {requestDTO.UserStaffMemberId}");
            var command = new GetDistrictQuery(requestDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>Retrieves active city, town, and village options for a district.</summary>
        [HttpGet("Locality/option")]
        public async Task<IActionResult> GetLocality([FromQuery] GetLocalityOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get Locality for District: {requestDTO.DistrictId}");
            var result = await _mediator.Send(new GetLocalityQuery(requestDTO));
            return Ok(result);
        }

        /// <summary>Retrieves the active locality types: City, Town, Village, and Other / Unclassified.</summary>
        [HttpGet("LocalityType/option")]
        public async Task<IActionResult> GetLocalityType([FromQuery] GetLocalityTypeOptionRequestDTO requestDTO)
        {
            _logger.LogInfo("Received request to get Locality Types.");
            var result = await _mediator.Send(new GetLocalityTypeQuery(requestDTO));
            return Ok(result);
        }
    }



}
