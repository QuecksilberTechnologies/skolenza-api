// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Education operations.
// ================================================================

using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.Features.StaffMemberCmd.EducationInfo.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all StaffMember Education & Related operations.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class EducationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;
        public EducationController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: creates staffMember education.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates education info.</para>
        /// <para>Handler flow: CreateEducationInfoCommand is processed by CreateEducationInfoCommandHandler; operation(s): CreateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetEducationResponseDTO: Id (int), StaffMemberId (string), Degree (string?), InstituteName (string?), Remark (string?), StartDate (DateOnly?), EndDate (DateOnly?), ScoreValue (string?), GradeDivision (string?), ScoreType (string?), FilePath (string?), FileType (int), FileName (string?), EducationGap (bool?), IsEditAllowed (bool?), IsInfoVerified (bool?), GapYears (double), ReasonOfEducationGap (string?), InfoVerifiedById (string?), HasEducationDocUploded (bool), IsActive (bool?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberEducationAPI.createStaffMemberEducation (app/core/services/staffMember-education-api.ts:81).</para>
        /// <para>Angular purpose: creates staffMember education.</para>
        /// <para>Integrated UI page(s): /app/profile/education-info</para>
        /// <para>Angular UI component(s): StaffMemberEducationForm (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-form/staffMember-education-form.ts); StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        public async Task<IActionResult> CreateStaffMember([FromForm] CreateEducationRequestDTO dto)

            {
                var command = new CreateEducationInfoCommand(dto);
                _logger.LogInfo("📩 Creating new staffMember education info...");

                var result = await _mediator.Send(command);
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: retrieves staffMember educations.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves education info.</para>
        /// <para>Handler flow: GetEducationInfoQuery is processed by GetEducationInfoQueryHandler; operation(s): GetInfo.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetEducationResponseDTO: Id (int), StaffMemberId (string), Degree (string?), InstituteName (string?), Remark (string?), StartDate (DateOnly?), EndDate (DateOnly?), ScoreValue (string?), GradeDivision (string?), ScoreType (string?), FilePath (string?), FileType (int), FileName (string?), EducationGap (bool?), IsEditAllowed (bool?), IsInfoVerified (bool?), GapYears (double), ReasonOfEducationGap (string?), InfoVerifiedById (string?), HasEducationDocUploded (bool), IsActive (bool?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberEducationAPI.getStaffMemberEducations (app/core/services/staffMember-education-api.ts:88).</para>
        /// <para>Angular purpose: retrieves staffMember educations.</para>
        /// <para>Integrated UI page(s): /app/profile/education-info</para>
        /// <para>Angular UI component(s): StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetAllStaffMemberInfo([FromQuery] GetEducationRequestDTO commandDto)
        {

                var command = new GetEducationInfoQuery(commandDto);
                var result = await _mediator.Send(command);
                    return Ok(result);


        }


       /// <summary>
       /// Used-In-Angular: deletes staffMember education.
       /// </summary>
       /// <remarks>
       /// <para>Angular usage status: Used-In-Angular.</para>
       /// <para>API endpoint purpose: deletes education info.</para>
       /// <para>Handler flow: DeleteEducationInfoQuery is processed by DeleteEducationInfoQueryHandler; operation(s): GetSingleRecordAsync, DeleteAsync.</para>
       /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
       /// <para>Angular function(s): StaffMemberEducationAPI.deleteStaffMemberEducation (app/core/services/staffMember-education-api.ts:101).</para>
       /// <para>Angular purpose: deletes staffMember education.</para>
       /// <para>Integrated UI page(s): /app/profile/education-info</para>
       /// <para>Angular UI component(s): StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts)</para>
       /// </remarks>
       [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] DeleteEducationRequestDTO dto)
        {
                _logger.LogInfo($"Deleting staffMember with Id: {dto.Id}");

                var command = new DeleteEducationInfoQuery(dto);
                var result = await _mediator.Send(command);


                _logger.LogInfo("Education deleted successfully.");
                return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: updates staffMember education.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates education info.</para>
        /// <para>Handler flow: UpdateEducationInfoCommand is processed by UpdateEducationInfoCommandHandler; operation(s): GetSingleRecordAsync, UpdateStaffMemberFieldAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberEducationAPI.updateStaffMemberEducation (app/core/services/staffMember-education-api.ts:94).</para>
        /// <para>Angular purpose: updates staffMember education.</para>
        /// <para>Integrated UI page(s): /app/profile/education-info</para>
        /// <para>Angular UI component(s): StaffMemberEducationForm (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-form/staffMember-education-form.ts); StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts)</para>
        /// </remarks>
        [HttpPost("update-education")]
        public async Task<IActionResult> UpdateEducation(UpdateEducationRequestDTO dto)

        {

                _logger.LogInfo($"Updating staffMember-education record. StaffMemberId: {dto.Id}");

                var command = new UpdateEducationInfoCommand(dto);
                var result = await _mediator.Send(command);



                _logger.LogInfo("StaffMember-education updated successfully.");
                return Ok(result);
            }

        }


}
