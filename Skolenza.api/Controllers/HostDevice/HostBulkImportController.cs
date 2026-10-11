using System.Text;
using skolenza.api.Common;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Host-only device catalogue imports; does not assign devices to institutions.</summary>
[Authorize]
[ApiController]
[Route("api/DeviceMaster/import")]
public sealed class DeviceMasterBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.DeviceMaster);

/// <summary>Host-only card inventory imports for exactly one selected institution.</summary>
[Authorize]
[ApiController]
[Route("api/InstitutionCardMaster/import")]
public sealed class InstitutionCardBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.InstitutionCard);

/// <summary>Host-only parent Module catalogue imports.</summary>
[Authorize, ApiController, Route("api/Module/import")]
public sealed class HostModuleBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.HostModule);

/// <summary>Host-only direct child Module catalogue imports.</summary>
[Authorize, ApiController, Route("api/ChildModule/import"), Route("api/SubModule/import")]
public sealed class HostSubModuleBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.HostSubModule);

/// <summary>Host-only Operation catalogue imports.</summary>
[Authorize, ApiController, Route("api/Operation/import")]
public sealed class HostOperationBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.HostOperation);

/// <summary>Host-only Module-Operation mapping imports.</summary>
[Authorize, ApiController, Route("api/ModuleOperation/import")]
public sealed class HostModuleOperationBulkImportController(IMediator mediator)
    : HostBulkImportController(mediator, BulkImportMaster.HostModuleOperation);

/// <summary>Shared durable import protocol. Permission IDs come from the scope-2 bulk menu.</summary>
public abstract class HostBulkImportController(IMediator mediator, BulkImportMaster master) : ControllerBase
{
    #region Preview and confirmation

    /// <summary>Not-Used-In-Angular. Upload Excel/CSV or PastedText with Import permission; saves a draft only.</summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview([FromForm] HostBulkImportPreviewRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new PreviewHostBulkImportQuery(master, request), ct));
    }

    /// <summary>Not-Used-In-Angular. Queues the saved valid draft; accepts no replacement rows.</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Confirm, request), ct));
    }

    #endregion

    #region Job lifecycle and downloads

    /// <summary>Not-Used-In-Angular. Reads owned job status, row results and completion counts.</summary>
    [HttpGet("job")]
    public async Task<IActionResult> Job([FromQuery] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Get, request), ct));
    }

    /// <summary>Not-Used-In-Angular. Lists only the authenticated Host user's jobs for this target and institution.</summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs([FromQuery] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.List, request), ct));
    }

    /// <summary>Not-Used-In-Angular. Retries failed rows; committed rows are never inserted again.</summary>
    [HttpPost("retry")]
    public async Task<IActionResult> Retry([FromBody] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Retry, request), ct));
    }

    /// <summary>Not-Used-In-Angular. Cancels Draft/Queued/Running jobs at batch boundaries; committed rows remain.</summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Cancel, request), ct));
    }

    /// <summary>Not-Used-In-Angular. Downloads supported starter headers using View or Import permission.</summary>
    [HttpGet("template")]
    public async Task<IActionResult> Template([FromQuery] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        var response = await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Template, request), ct);
        return File(Encoding.UTF8.GetBytes((string)response.Data!), "text/csv", $"{master}-template.csv");
    }

    /// <summary>Not-Used-In-Angular. Downloads owned job results; card numbers are masked.</summary>
    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        var response = await mediator.Send(new ManageHostBulkImportCommand(master, BulkImportAction.Get, request), ct);
        return File(BulkImportReport.Create((BulkImportJobResponseDTO)response.Data!), "text/csv", $"{master}-{request.JobId}.csv");
    }

    #endregion
}
