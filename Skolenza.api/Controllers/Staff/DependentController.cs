// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Dependent operations.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.Features.StaffMemberCmd.DependentInfo.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all StaffMember-Dependent related operations like create, update, delete, and view.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class DependentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public DependentController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Validates IMEI number. Must be 15 digits and numeric only.
        /// </summary>


        /// <summary>
        /// Used-In-Angular: creates staffMember dependent.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates dependent.</para>
        /// <para>Handler flow: CreateDependentCommand is processed by CreateDependentCommandHandler; operation(s): CreateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDependentResponseDTO: Id (long), StaffMemberId (string?), DependentName (string?), Relation (int?), RelationType (string?), DateOfBirth (DateTime?), IsCoveredInPolicy (bool?), IsMarried (bool?), Remark (string?), Description (string?), IsActive (bool?), HasProofUploaded (bool), HasUploadedAll (bool), CompletionPercentage (double), FilePath (string?), InfoVerifiedById (string?), IsInfoVerified (bool?), IsEditAllowed (bool?), InfoVerifiedDateTime (DateTime?)</para>
        /// <para>Angular function(s): StaffMemberDependentApi.createStaffMemberDependent (app/core/services/staffMember-dependent-api.ts:72).</para>
        /// <para>Angular purpose: creates staffMember dependent.</para>
        /// <para>Integrated UI page(s): /app/profile/dependent-info</para>
        /// <para>Angular UI component(s): StaffMemberDependentForm (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-form/staffMember-dependent-form.ts); StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDependentInfo([FromForm] CreateDependentRequestDTO Dto)
        {

                // ✅ IMEI validation
                if (Dto == null)
                {
                    _logger.LogInfo($"Invalid IMEI: {Dto}");
                }

                _logger.LogInfo("Creating new empolyee Dependent process started.");

                var command = new CreateDependentCommand(Dto);
                var result = await _mediator.Send(command);
                _logger.LogInfo("StaffMember-Dependent created successfully.");
                return Ok(result);


        }


        /// <summary>
        /// Used-In-Angular: retrieves staffMember dependents.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves dependent info.</para>
        /// <para>Handler flow: GetDependentInfoQuery is processed by GetDependentInfoQueryHandler; operation(s): GetInfo.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDependentResponseDTO: Id (long), StaffMemberId (string?), DependentName (string?), Relation (int?), RelationType (string?), DateOfBirth (DateTime?), IsCoveredInPolicy (bool?), IsMarried (bool?), Remark (string?), Description (string?), IsActive (bool?), HasProofUploaded (bool), HasUploadedAll (bool), CompletionPercentage (double), FilePath (string?), InfoVerifiedById (string?), IsInfoVerified (bool?), IsEditAllowed (bool?), InfoVerifiedDateTime (DateTime?)</para>
        /// <para>Angular function(s): StaffMemberDependentApi.getStaffMemberDependents (app/core/services/staffMember-dependent-api.ts:79).</para>
        /// <para>Angular purpose: retrieves staffMember dependents.</para>
        /// <para>Integrated UI page(s): /app/profile/dependent-info</para>
        /// <para>Angular UI component(s): StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> Getinfo([FromQuery] GetDependentRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all  .");

                var command = new GetDependentInfoQuery(requestDto);
                var result = await _mediator.Send(command);

                return Ok(result);

        }
        /// <summary>
        /// Used-In-Angular: retrieves staffMember dependents detail.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves dependent counts.</para>
        /// <para>Handler flow: GetDependentCountsQuery is processed by GetDependentCountsQueryHandler; operation(s): GetDetailInfo.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDependentsDetailResponseDTO: TotalDependents (int), TotalChilds (int), TotalSpouses (int), TotalParents (int), TotalInLaws (int), Dependents (List&lt;GetDependentResponseDTO&gt;)</para>
        /// <para>Angular function(s): StaffMemberDependentApi.getStaffMemberDependentsDetail (app/core/services/staffMember-dependent-api.ts:88).</para>
        /// <para>Angular purpose: retrieves staffMember dependents detail.</para>
        /// <para>Integrated UI page(s): /app/profile/insurance-info</para>
        /// <para>Angular UI component(s): StaffMemberInsuranceForm (app/features/user-menu/staffMember-profile/staffMember-insurance-info/staffMember-insurance-form/staffMember-insurance-form.ts); StaffMemberInsuranceInfo (app/features/user-menu/staffMember-profile/staffMember-insurance-info/staffMember-insurance-info.ts)</para>
        /// </remarks>
        [HttpGet("get-in-detail")]
        public async Task<IActionResult> GetInDetail([FromQuery] GetDependentRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all .");

                var command = new GetDependentCountsQuery(requestDto);
                var result = await _mediator.Send(command);

                return Ok(result);

        }


      /// <summary>
      /// Used-In-Angular: deletes staffMember dependent.
      /// </summary>
      /// <remarks>
      /// <para>Angular usage status: Used-In-Angular.</para>
      /// <para>API endpoint purpose: deletes contact.</para>
      /// <para>Handler flow: DeleteContactQuery is processed by DeleteContactInfoQueryHandler; operation(s): GetSingleRecordAsync, DeleteAsync.</para>
      /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
      /// <para>Angular function(s): StaffMemberDependentApi.deleteStaffMemberDependent (app/core/services/staffMember-dependent-api.ts:103).</para>
      /// <para>Angular purpose: deletes staffMember dependent.</para>
      /// <para>Integrated UI page(s): /app/profile/dependent-info</para>
      /// <para>Angular UI component(s): StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts)</para>
      /// </remarks>
      [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] DeleteRequestDTO dto)
        {

                _logger.LogInfo($"Deleting staffMember with Id: {dto.Id}");

                var command = new DeleteContactQuery(dto);
                var result = await _mediator.Send(command);



                _logger.LogInfo("Dependent deleted successfully.");
                return Ok(result);


        }


        /// <summary>
        /// Used-In-Angular: updates staffMember dependent.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates dependent.</para>
        /// <para>Handler flow: UpdateDependentCommand is processed by UpdateDependentInfoCommandHandler; operation(s): GetSingleRecordAsync, UpdateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberDependentApi.updateStaffMemberDependent (app/core/services/staffMember-dependent-api.ts:96).</para>
        /// <para>Angular purpose: updates staffMember dependent.</para>
        /// <para>Integrated UI page(s): /app/profile/dependent-info</para>
        /// <para>Angular UI component(s): StaffMemberDependentForm (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-form/staffMember-dependent-form.ts); StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts)</para>
        /// </remarks>
        [HttpPost("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] UpdateDependentRequestDTO dto)
        {

                _logger.LogInfo($"Updating staffMember-Dependent record. Id: {dto.Id}");

                var command = new UpdateDependentCommand(dto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember-Dependent updated successfully.");
                return Ok(result);

        }



    }
}
