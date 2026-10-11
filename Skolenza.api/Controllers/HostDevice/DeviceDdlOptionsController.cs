// ================================================================
// Purpose : Exposes the firmware-confirmed dropdown values used by the Institution
//           device-configuration UI. Angular must not hard-code device codes.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Provides a separate option endpoint for every Device Configuration UI section.</summary>
[Authorize]
[ApiController]
[Route("api/device-ddl-options")]
public sealed class DeviceDdlOptionsController : ControllerBase
{
    /// <summary>Gets time, date, NTP, and time-zone options so Angular renders firmware-valid values for the Time settings section.</summary>
    [HttpGet("time")]
    public IActionResult GetTime() => Ok(Section(DeviceDdl.Time));

    /// <summary>Gets bell output and ring-pattern options so Angular does not hard-code vendor values for the Bell section.</summary>
    [HttpGet("bell")]
    public IActionResult GetBell() => Ok(Section(DeviceDdl.Bell));

    /// <summary>Gets device display, language, and recognition options for the Device setup form.</summary>
    [HttpGet("device-setup")]
    public IActionResult GetDeviceSetup() => Ok(Section(DeviceDdl.DeviceSetup));

    /// <summary>Gets verification, QR, and fill-light options for the Advanced settings form.</summary>
    [HttpGet("advanced")]
    public IActionResult GetAdvanced() => Ok(Section(DeviceDdl.Advanced));

    /// <summary>Gets door, Wiegand, and card-display options for the Lock settings form.</summary>
    [HttpGet("lock")]
    public IActionResult GetLock() => Ok(Section(DeviceDdl.Lock));

    /// <summary>Gets serial baud-rate and function options for the Serial settings form.</summary>
    [HttpGet("serial")]
    public IActionResult GetSerial() => Ok(Section(DeviceDdl.Serial));

    /// <summary>Gets Ethernet options for the Ethernet network form.</summary>
    [HttpGet("ethernet")]
    public IActionResult GetEthernet() => Ok(Section(DeviceDdl.Ethernet));

    /// <summary>Gets Wi-Fi options for the Wi-Fi network form.</summary>
    [HttpGet("wifi")]
    public IActionResult GetWifi() => Ok(Section(DeviceDdl.Wifi));

    /// <summary>Gets app-notification options for the optional App notification form.</summary>
    [HttpGet("app-notification")]
    public IActionResult GetAppNotification() => Ok(Section(DeviceDdl.AppNotification));

    /// <summary>Gets staff member credential choices only for the Remove credential action, keeping credential codes out of Angular constants.</summary>
    [HttpGet("staff-member-device-credentials")]
    public IActionResult GetStaffMemberDeviceCredentials() => Ok(Section(DeviceDdl.StaffMemberDeviceCredentials));

    /// <summary>Gets Monday-first working-access-day options for the staff member device access-window editor.</summary>
    [HttpGet("staff-member-device-access-windows")]
    public IActionResult GetStaffMemberDeviceAccessWindows() => Ok(Section(DeviceDdl.StaffMemberDeviceAccessWindows));

    /// <summary>Gets Host card-procurement tax treatment and inventory-status filter options for Institution card inventory screens.</summary>
    [HttpGet("institution-card-inventory")]
    public IActionResult GetInstitutionCardInventory() => Ok(Section(DeviceDdl.InstitutionCardInventory));

    private static ApiResponse<IReadOnlyList<DeviceDdlField>> Section(string section) =>
        ApiResponse<IReadOnlyList<DeviceDdlField>>.Success(DeviceDdl.GetSection(section));
}
