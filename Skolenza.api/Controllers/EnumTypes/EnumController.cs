// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Enum operations.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOs.Entity;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.EnumDTO;
using skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.EnumTypes
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnumController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration
        public EnumController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        /// <summary>
        /// Used-In-Angular: retrieves all currencies.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves currencies.</para>
        /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
        /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
        /// <para>Angular function(s): EnumApi.getAllCurrencies (app/core/services/enum-api.ts:30).</para>
        /// <para>Angular purpose: retrieves all currencies.</para>
        /// <para>Integrated UI page(s): /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit; /app/campuss</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
        /// </remarks>

        [HttpGet("get-all-currencies")]


        public async Task<IActionResult> GetCurrencies([FromQuery] GetCurrencyRequestDTO dto)
        {

            _logger.LogInfo("Fetching all currencies.");

            var data = CurrencyProvider.GetAll(dto.IsActive);



            return Ok(data);
        }



    }
}

