// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Experience operations.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;

using skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all StaffMember Experience & Related operations.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class ExperienceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public ExperienceController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // <summary>
        // Create new staffMember experience record.
        // </summary>
        /// <summary>
        /// Used-In-Angular: creates staffMember experience.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates experience info.</para>
        /// <para>Handler flow: CreateExperienceInfoCommand is processed by CreateExperienceInfoCommandHandler; operation(s): GetExtension, Add, AddAsync, SaveChangesAsync, GetFileUrl.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStaffMemberExperienceResponseDTO: Id (long), StaffMemberId (string?), Ctc (decimal?), CompanyName (string?), Designation (string?), StaffMemberIdOfCompany (string?), StartDate (DateTime?), EndDate (DateTime?), Experience (int?), IsWFH (bool), WorkingCountryId (int?), WorkingStateId (int?), WorkingDistrictId (int?), IsForeignExperience (bool), ReasonForLeaving (string?), Remark (string?), ColleagueName (string?), ColleagueDesignation (string?), ColleagueContactNumber (string?), ReportingManagerName (string?), ReportingManagerNumber (string?), VerificationEmail (string?), CompletionPercentage (double), IsAnyGap (bool), ReasonOfGap (string?), GapYearFrom (DateTime?), GapYearTo (DateTime?), IsExperienceVerified (bool?), IsExperienceVerifiedByMail (bool?), IsExperienceVerifiedByCall (bool?), IsInfoVerified (bool?), IsEditAllowed (bool?), Documents (List&lt;GetStaffMemberExperienceDocumentDTO&gt;?)</para>
        /// <para>Angular function(s): StaffMemberExperienceAPI.createStaffMemberExperience (app/core/services/staffMember-experience-api.ts:104).</para>
        /// <para>Angular purpose: creates staffMember experience.</para>
        /// <para>Integrated UI page(s): /app/profile/experience-info</para>
        /// <para>Angular UI component(s): StaffMemberExperienceForm (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-form/staffMember-experience-form.ts); StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateExperience([FromForm] CreateExperienceRequestDTO dto)
        {
            _logger.LogInfo("Received Experience Create Request for StaffMember: " + dto.StaffMemberId);
            var command = new CreateExperienceInfoCommand(dto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves staffMember experience.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves experience info.</para>
        /// <para>Handler flow: GetExperienceInfoQuery is processed by GetExperienceInfoQueryHandler; operation(s): GetByStaffMemberIdWithDocumentsAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStaffMemberExperienceResponseDTO: Id (long), StaffMemberId (string?), Ctc (decimal?), CompanyName (string?), Designation (string?), StaffMemberIdOfCompany (string?), StartDate (DateTime?), EndDate (DateTime?), Experience (int?), IsWFH (bool), WorkingCountryId (int?), WorkingStateId (int?), WorkingDistrictId (int?), IsForeignExperience (bool), ReasonForLeaving (string?), Remark (string?), ColleagueName (string?), ColleagueDesignation (string?), ColleagueContactNumber (string?), ReportingManagerName (string?), ReportingManagerNumber (string?), VerificationEmail (string?), CompletionPercentage (double), IsAnyGap (bool), ReasonOfGap (string?), GapYearFrom (DateTime?), GapYearTo (DateTime?), IsExperienceVerified (bool?), IsExperienceVerifiedByMail (bool?), IsExperienceVerifiedByCall (bool?), IsInfoVerified (bool?), IsEditAllowed (bool?), Documents (List&lt;GetStaffMemberExperienceDocumentDTO&gt;?)</para>
        /// <para>Angular function(s): StaffMemberExperienceAPI.getStaffMemberExperience (app/core/services/staffMember-experience-api.ts:98).</para>
        /// <para>Angular purpose: retrieves staffMember experience.</para>
        /// <para>Integrated UI page(s): /app/profile/experience-info</para>
        /// <para>Angular UI component(s): StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        //
        //
        public async Task<IActionResult> GetAllexperinceInfo([FromQuery] GetExperienceRequestDTO commandDto)
        {

                var command = new GetExperienceInfoQuery(commandDto);
                var result = await _mediator.Send(command);

                    return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: updates staffMember experience.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates experience info.</para>
        /// <para>Handler flow: UpdateExperienceInfoCommand is processed by UpdateExperienceInfoCommandHandler; operation(s): GetByIdAsync, UpdateAsync, SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberExperienceAPI.updateStaffMemberExperience (app/core/services/staffMember-experience-api.ts:110).</para>
        /// <para>Angular purpose: updates staffMember experience.</para>
        /// <para>Integrated UI page(s): /app/profile/experience-info</para>
        /// <para>Angular UI component(s): StaffMemberExperienceForm (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-form/staffMember-experience-form.ts); StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts)</para>
        /// </remarks>
        [HttpPost("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] UpdateExperienceRequestDTO dto)
        {
              _logger.LogInfo($"Updating staffMember-experience record. StaffMemberId: {dto.StaffMemberId}");
                var command = new UpdateExperienceInfoCommand(dto);
                var result = await _mediator.Send(command);
                _logger.LogInfo("StaffMember-experience updated successfully.");
                return Ok(result);

        }
        /// <summary>
        /// Used-In-Angular: deletes staffMember experience.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes experience.</para>
        /// <para>Handler flow: DeleteExperienceCommand is processed by DeleteExperienceCommandHandler; operation(s): GetByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberExperienceAPI.deleteStaffMemberExperience (app/core/services/staffMember-experience-api.ts:117).</para>
        /// <para>Angular purpose: deletes staffMember experience.</para>
        /// <para>Integrated UI page(s): /app/profile/experience-info</para>
        /// <para>Angular UI component(s): StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] DeleteRequestDTO dto)
        {
            _logger.LogInfo($"Deleting staffMember with Id: {dto.Id}");

            var command = new DeleteExperienceCommand(dto);
            var result = await _mediator.Send(command);


            _logger.LogInfo("Education deleted successfully.");
            return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: deletes staffMember experience doc.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes experience doc.</para>
        /// <para>Handler flow: DeleteExperienceDocCommand is processed by DeleteExperienceDocCommandHandler; operation(s): GetSingleByDetailIdAsync, SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberExperienceAPI.deleteStaffMemberExperienceDoc (app/core/services/staffMember-experience-api.ts:123).</para>
        /// <para>Angular purpose: deletes staffMember experience doc.</para>
        /// <para>Integrated UI page(s): /app/profile/experience-info</para>
        /// <para>Angular UI component(s): StaffMemberExperienceForm (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-form/staffMember-experience-form.ts); StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts)</para>
        /// </remarks>
        [HttpDelete("delete-doc")]
        public async Task<IActionResult> DeleteDoc([FromQuery] DeleteRequestDTO dto)
        {
            _logger.LogInfo($"Deleting staffMember with Id: {dto.Id}");

            var command = new DeleteExperienceDocCommand(dto);
            var result = await _mediator.Send(command);


            _logger.LogInfo("Education deleted successfully.");
            return Ok(result);


        }
    }
}
