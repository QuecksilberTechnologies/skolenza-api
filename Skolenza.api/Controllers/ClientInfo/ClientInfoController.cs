// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Client Info operations.
// ================================================================

using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;

[Route("api/[controller]")]
[ApiController]
public class ClientInfoController : ControllerBase
{
    private string GetPublicIpFromRequest()
    {
        // Agar client proxy ke through connected hai toh "X-Forwarded-For" header check karein
        string ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrEmpty(ip))
        {
            ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        return ip;
    }
    /// <summary>
    /// Used-In-Angular: retrieves device info.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves device info.</para>
    /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
    /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
    /// <para>Angular function(s): AuthApi.getDeviceInfo (app/core/services/auth-api.ts:170).</para>
    /// <para>Angular purpose: retrieves device info.</para>
    /// <para>Integrated UI page(s): /auth/login</para>
    /// <para>Angular UI component(s): Login (app/features/authentication/login/login.ts)</para>
    /// </remarks>
    [HttpGet("detect-device")]
    public IActionResult GetDeviceInfo()
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var deviceType = GetDeviceType(userAgent);
        var localIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        var publicIp = GetPublicIpFromRequest();
        //var useragent = request.headers["user-agent"].tostring();
        //var devicetype = getdevicetype(useragent);
        //var localip ="192.168.0.08";
        //var publicip = "33.454.32.344";
        var deviceInfo = new
        {
            localIp ,
            publicIp ,
            deviceType,
            userAgent
        };

        return Ok(deviceInfo);
    }

    private int GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return -1;

        userAgent = userAgent.ToLower();

        if (userAgent.Contains("mobile")) return 2;
        if (userAgent.Contains("tablet")) return 3;
        if (userAgent.Contains("ipad")) return 3;
        if (userAgent.Contains("android") && !userAgent.Contains("mobile")) return 3;

        return 1;
    }
}
