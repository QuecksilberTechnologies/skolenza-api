// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Bank operations.
// ================================================================



using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers;
using skolenza.application.Features.StaffMemberCmd.Contact.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles Staff Member bank operations such as create, update, delete, and view.
    /// </summary>
    [Route("api/Staff/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public BankController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Validates IMEI number. Must be 15 digits and numeric only.
        /// </summary>


        /// <summary>
        /// Used-In-Angular: creates Staff Member bank details.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates bank info.</para>
        /// <para>Handler flow: CreateBankInfoCommand is processed by CreateBankInfoCommandHandler; operation(s): CreateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetBankResponseDTO: Id (int), StaffMemberId (string?), BankName (string?), AccountNumber (string?), AccountType (string?), IsPrimaryAccount (bool), IsInfoVerified (bool?), IFSCCode (string?), BranchName (string?), UPIId (string?), HasChequeDocUploaded (bool), FilePath (string?), FileName (string?), FileType (int?), CancelledChequeFile (IFormFile?), IsEditAllowed (bool?), IsActive (bool), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberBanksAPI.createStaffMemberBank (app/core/services/staffMember-banks-api.ts:89).</para>
        /// <para>Angular purpose: creates Staff Member bank details.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info</para>
        /// <para>Angular UI component(s): StaffMemberBankForm (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-form/staffMember-bank-form.ts); StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        public async Task<IActionResult> CreateBankInfo([FromForm] CreateBankRequestDTO Dto)
        {

                // ✅ IMEI validation
                if (Dto == null)
                {
                    _logger.LogInfo($"Invalid IMEI: {Dto}");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                }
                _logger.LogInfo("Creating new Bank process started.");

                var command = new CreateBankInfoCommand(Dto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember-Bankinfo created successfully.");
                return Ok(result);


        }




        /// <summary>
        /// Used-In-Angular: retrieves Staff Member bank details.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves bank info.</para>
        /// <para>Handler flow: GetBankInfoQuery is processed by GetBankInfoQueryHandler; operation(s): GetInfoAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetBankResponseDTO: Id (int), StaffMemberId (string?), BankName (string?), AccountNumber (string?), AccountType (string?), IsPrimaryAccount (bool), IsInfoVerified (bool?), IFSCCode (string?), BranchName (string?), UPIId (string?), HasChequeDocUploaded (bool), FilePath (string?), FileName (string?), FileType (int?), CancelledChequeFile (IFormFile?), IsEditAllowed (bool?), IsActive (bool), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberBanksAPI.getStaffMemberBanks (app/core/services/staffMember-banks-api.ts:96).</para>
        /// <para>Angular purpose: retrieves Staff Member bank details.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info</para>
        /// <para>Angular UI component(s): StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]


        public async Task<IActionResult> GetBankinfo([FromQuery] GetBankReqestDTO requestDto)
        {

                _logger.LogInfo("Fetching all bank.");

                var command = new GetBankInfoQuery(requestDto);
                var result = await _mediator.Send(command);


                return Ok(result);
       }


        /// <summary>
        /// Used-In-Angular: deletes Staff Member bank details.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes bank info.</para>
        /// <para>Handler flow: DeleteBankInfoQuery is processed by DeleteBankInfoQueryHandler; operation(s): GetSingleRecordAsync, DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBanksAPI.deleteStaffMemberBank (app/core/services/staffMember-banks-api.ts:109).</para>
        /// <para>Angular purpose: deletes Staff Member bank details.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info</para>
        /// <para>Angular UI component(s): StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
                public async Task<IActionResult> Delete([FromQuery] DeleteBankRequestDTO dto)

            {
                _logger.LogInfo($"Deleting staffMember bank info with Id: {dto.Id}");

                var command = new DeleteBankInfoQuery(dto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember bank info deleted successfully.");
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: updates Staff Member bank details.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates bank.</para>
        /// <para>Handler flow: UpdateBankCommand is processed by UpdateBankCommandHandler; operation(s): GetSingleRecordAsync, UpdateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBanksAPI.updateStaffMemberBank (app/core/services/staffMember-banks-api.ts:102).</para>
        /// <para>Angular purpose: updates Staff Member bank details.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info</para>
        /// <para>Angular UI component(s): StaffMemberBankForm (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-form/staffMember-bank-form.ts); StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts)</para>
        /// </remarks>
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromForm] UpdateBankReqestDTO dto)
        {
                _logger.LogInfo($"Updating staffMember-bank record. By UserStaffMemberId: {dto.UserStaffMemberId}");

                var command = new UpdateBankCommand(dto);
                var result = await

                    _mediator.Send(command);


                _logger.LogInfo("StaffMember updated successfully.");
                return Ok(result);
            }

        }



}
