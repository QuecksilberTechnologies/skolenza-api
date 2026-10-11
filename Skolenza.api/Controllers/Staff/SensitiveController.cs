// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Sensitive operations.
// ================================================================


using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.Features.StaffMemberCmd.IdentitiesInfo.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all StaffMember Personal & Related operations.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class SensitiveController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public SensitiveController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

         /// <summary>
         /// Used-In-Angular: creates staffMember sensitive.
         /// </summary>
         /// <remarks>
         /// <para>Angular usage status: Used-In-Angular.</para>
         /// <para>API endpoint purpose: creates identity info.</para>
         /// <para>Handler flow: CreateIdentityInfoCommand is processed by CreateStaffMemberIdentityCommandHandler; operation(s): Add, CreateAsync, DeleteFileAsync.</para>
         /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
         /// <para>Angular function(s): StaffMemberIdentityApi.createStaffMemberSensitive (app/core/services/staffMember-identity-api.ts:75).</para>
         /// <para>Angular purpose: creates staffMember sensitive.</para>
         /// <para>Integrated UI page(s): /app/profile/identity-info</para>
         /// <para>Angular UI component(s): StaffMemberIdentityForm (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-form/staffMember-identity-form.ts); StaffMemberIdentityInfo (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-info.ts)</para>
         /// </remarks>

         [HttpPost("Create")]
        public async Task<IActionResult> Createpersonalinfo([FromForm] CreateStaffMemberIdentityRequestDTO dto)
        {

                var command = new CreateIdentityInfoCommand(dto);
                _logger.LogInfo("Creating new staffMember personal info info...");

                var result = await _mediator.Send(command);
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: retrieves staffMember identities.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves identity info.</para>
        /// <para>Handler flow: GetIdentityInfoQuery is processed by GetIdentityInfoQueryHandler; operation(s): GetIdentityRecordAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStaffMemberIdentityResponseDTO: Id (long?), StaffMemberId (string?), CountryCode (string), CountryName (string), IdentityCategoryName (string), IdentityCategoryDocumentId (int), DocumentCode (string), DocumentName (string), Description (string?), IsMandatory (bool), StaffMemberIdentityId (long?), IdentityValue (string?), IsVerified (bool?), IsEditAllowed (bool?), HasIdentityUploaded (bool?), EffectiveFrom (DateTime?), EffectiveTo (DateTime?), IsActive (bool?)</para>
        /// <para>Angular function(s): StaffMemberIdentityApi.getStaffMemberIdentities (app/core/services/staffMember-identity-api.ts:69).</para>
        /// <para>Angular purpose: retrieves staffMember identities.</para>
        /// <para>Integrated UI page(s): /app/profile/identity-info</para>
        /// <para>Angular UI component(s): StaffMemberIdentityInfo (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetSensitiveData([FromQuery] GetIdentityRequestDTO commandDto)
        {
                var command = new GetIdentityInfoQuery(commandDto);
                var result = await _mediator.Send(command);

                    return Ok(result);


        }

        /// <summary>
        /// Used-In-Angular: soft deletes one persisted staffMember identity.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes an editable identity record and its uploaded file.</para>
        /// <para>Handler flow: DeleteIdentityInfoCommand is processed by DeleteIdentityInfoCommandHandler.</para>
        /// <para>Integrated UI page(s): /app/profile/identity-info</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteIdentityRequestDTO dto)
        {
            var result = await _mediator.Send(new DeleteIdentityInfoCommand(dto));
            return Ok(result);
        }
        /// <summary>
        /// Updates staffMember details.
        /// </summary>
        //[HttpPost("update")]
        //
        //
        //
        //public async Task<IActionResult> Update([FromBody] GenricUpdateRequestDTO dto)
        //{
        //    try
        //    {
        //        _logger.LogInfo($"Updating staffMember-personal info record. StaffMemberId: {dto._StaffMemberId}");

        //        var command = new UpdateIdentityInfoCommand(dto);
        //        var result = await _mediator.Send(command);

        //        if (!result.IsSucceeded)
        //        {
        //            _logger.LogInfo($"Failed to update staffMember-personal info with Id: {dto._StaffMemberId}");
        //            return BadRequest(result);
        //        }

        //        _logger.LogInfo("StaffMember-personal info updated successfully.");
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error updating staffMember-personal info: {ex.Message}");
        //        var errorResponse = ApiResponse<bool>.Fail("An unexpected error occurred while updating staffMember-personal info info.",
        //            new List<string> { ex.Message });
        //        return StatusCode(500, errorResponse);
        //    }
        //}


    }
}
