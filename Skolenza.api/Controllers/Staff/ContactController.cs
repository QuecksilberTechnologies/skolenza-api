// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Contact operations.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.Contact;

using skolenza.application.Features.StaffMemberCmd.Contact.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all StaffMember-Contact related operations like create, update, delete, and view.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public ContactController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: retrieves the centralized StaffMember contact relation catalogue.
        /// </summary>
        /// <remarks>
        /// <para>Authentication: bearer token is required.</para>
        /// <para>Permission behavior: this constants lookup accepts no ModuleId or OperationId.</para>
        /// <para>Handler flow: GetContactRelationOptionsQuery is processed by GetContactRelationOptionsQueryHandler without a database read.</para>
        /// <para>Angular function(s): StaffMemberContactsAPI.getRelationOptions.</para>
        /// <para>Angular UI component(s): StaffMemberContactForm; StaffMemberManageDialog; StaffMemberContactInfo; StaffMemberBasicInfo; AvatarPopup.</para>
        /// </remarks>
        [Authorize]
        [HttpGet("relation-options")]
        public async Task<IActionResult> GetRelationOptions(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetContactRelationOptionsQuery(),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates staffMember contact.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates contact info.</para>
        /// <para>Handler flow: CreateContactInfoCommand is processed by CreateContactInfoCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetContactResponseDTO: Id (long), StaffMemberId (string?), ContactName (string?), Relation (int?), ContactType (int?), ContactNumber (string?), AlternateNumber (string?), Email (string?), IsPrimary (bool), CountryName (string?), StateName (string?), DistrictName (string?), CountryId (int?), StateId (int?), DistrictId (int?), HouseNo (string?), LandMark (string?), Street (string?), Address (string?), Remark (string?), IsActive (bool?), IsEditAllowed (bool?), IsInfoVerified (bool?), InfoVerifiedById (string?), InfoVerifiedDateTime (DateTime?), Description (string?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberContactsAPI.createStaffMemberContact (app/core/services/staffMember-contacts-api.ts:79).</para>
        /// <para>Angular purpose: creates staffMember contact.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): StaffMemberContactForm (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-form/staffMember-contact-form.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        public async Task<IActionResult> CreateContactInfo([FromBody] CreateContactRequestDTO Dto)
        {
                // ✅ IMEI validation
                if (Dto == null)
                {
                    _logger.LogInfo($"Invalid IMEI: {Dto}");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                }

                _logger.LogInfo("Creating new empolyee contact process started.");

                var command = new CreateContactInfoCommand(Dto);
                var result = await _mediator.Send(command);


                _logger.LogInfo("StaffMember-contact created successfully.");
                return Ok(result);
            }




        /// <summary>
        /// Used-In-Angular: retrieves staffMember contacts.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves contact info.</para>
        /// <para>Handler flow: GetContactInfoQuery is processed by GetContactInfoQueryHandler; operation(s): GetInfo.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetContactResponseDTO: Id (long), StaffMemberId (string?), ContactName (string?), Relation (int?), ContactType (int?), ContactNumber (string?), AlternateNumber (string?), Email (string?), IsPrimary (bool), CountryName (string?), StateName (string?), DistrictName (string?), CountryId (int?), StateId (int?), DistrictId (int?), HouseNo (string?), LandMark (string?), Street (string?), Address (string?), Remark (string?), IsActive (bool?), IsEditAllowed (bool?), IsInfoVerified (bool?), InfoVerifiedById (string?), InfoVerifiedDateTime (DateTime?), Description (string?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberContactsAPI.getStaffMemberContacts (app/core/services/staffMember-contacts-api.ts:86).</para>
        /// <para>Angular purpose: retrieves staffMember contacts.</para>
        /// <para>Integrated UI page(s): /app/profile/contact-info</para>
        /// <para>Angular UI component(s): StaffMemberContactInfo (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
                public async Task<IActionResult> GetBankinfo([FromQuery] GetContactRequestDTO requestDto)

            {
                _logger.LogInfo("Fetching all bank.");

                var command = new GetContactInfoQuery(requestDto);
                var result = await _mediator.Send(command);
                return Ok(result);

        }



        /// <summary>
        /// Used-In-Angular: updates staffMember contact.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates staffMember contact.</para>
        /// <para>Handler flow: UpdateStaffMemberContactCommand is processed by UpdateContactInfoCommandHandler; operation(s): GetSingleRecordAsync, UpdateContactAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberContactsAPI.updateStaffMemberContact (app/core/services/staffMember-contacts-api.ts:92).</para>
        /// <para>Angular purpose: updates staffMember contact.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): StaffMemberContactForm (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-form/staffMember-contact-form.ts)</para>
        /// </remarks>
        [HttpPost("update")]
        public async Task<IActionResult> UpdateContact([FromBody] UpdateContactRequestDTO dto)
        {
                _logger.LogInfo($"Updating staffMember-contact record. StaffMemberId: {dto.Id}");

                var command = new UpdateStaffMemberContactCommand(dto);
                var result = await _mediator.Send(command);



                _logger.LogInfo("StaffMember-contact updated successfully.");
                return Ok(result);


        }

        /// <summary>
        /// Used-In-Angular: deletes staffMember contact.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes contact.</para>
        /// <para>Handler flow: DeleteContactQuery is processed by DeleteContactInfoQueryHandler; operation(s): GetSingleRecordAsync, DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberContactsAPI.deleteStaffMemberContact (app/core/services/staffMember-contacts-api.ts:99).</para>
        /// <para>Angular purpose: deletes staffMember contact.</para>
        /// <para>Integrated UI page(s): /app/profile/contact-info</para>
        /// <para>Angular UI component(s): StaffMemberContactInfo (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-info.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] DeleteRequestDTO dto)
        {

                _logger.LogInfo($"Deleting staffMember with Id: {dto.Id}");

                var command = new DeleteContactQuery(dto);
                var result = await _mediator.Send(command);


                _logger.LogInfo("StaffMember deleted successfully.");
                return Ok(result);

        }
    }
}
