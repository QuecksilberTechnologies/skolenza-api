
// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates attendance requests and temporary TIMMY biometric-device diagnostics.
// ================================================================

using skolenza.application.DTOs.Attendance;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Features.AttendanceCmd;

using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;

using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;


namespace skolenza.api.Controllers.Attendance
{
    // UserLoginController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public AttendanceController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function attendance.</para>
                /// <para>Handler flow: AttendanceCommand is dispatched from the controller; no matching handler class was statically resolved.</para>
                /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/attendance/mark-attendance.</para>
                /// </remarks>


                [Authorize]
                [HttpPost("mark-attendance")]
                public async Task<IActionResult> MarkAttendance([FromBody] AttendanceRequestDTO? attendanceRequestDTO)
                {
                    if (attendanceRequestDTO is null) return BadRequest();
                    return Ok(await _mediator.Send(new MarkAttendanceCommand(attendanceRequestDTO)));
                }
                /// <summary>Gets today's attendance timeline for the authenticated Staff Member.</summary>
                /// <remarks>
                /// Uses the validated Institution token identity. Staff Member, Module and Operation identifiers are not accepted.
                /// </remarks>
                [Authorize]
                [HttpGet("today")]
                public async Task<IActionResult> GetTodayAttendance()
                {
                    return Ok(await _mediator.Send(new GetTodayAttendanceQuery()));
                }
                /// <summary>Gets active attendance device/source types for attendance UI controls.</summary>
                /// <remarks>Authenticated lookup endpoint. UI must use returned IDs and must not hard-code them.</remarks>
                [Authorize]
                [HttpGet("device-types")]
                public async Task<IActionResult> GetAttendanceDeviceTypes()
                {
                    return Ok(await _mediator.Send(new GetAttendanceDeviceTypesQuery()));
                }
        #region TIMMY HTTPS Test
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function timmy test.</para>
                /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
                /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/attendance/timmy-test.</para>
                /// </remarks>

                [AllowAnonymous]
                [HttpPost("timmy-test")]
                public async Task<IActionResult> TimmyTest()
                {
                    try
                    {
                        #region Request Diagnostics

                        Console.WriteLine("========================================");
                        Console.WriteLine("TIMMY TEST ENDPOINT HIT");
                        Console.WriteLine($"Time           : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Method         : {Request.Method}");
                        Console.WriteLine($"Path           : {Request.Path}");
                        Console.WriteLine($"Content-Type   : {Request.ContentType}");
                        Console.WriteLine($"Content-Length : {Request.ContentLength}");
                        Console.WriteLine($"QueryString    : {Request.QueryString}");
                        Console.WriteLine("========================================");

                        _logger.LogInfo(
                            $"TIMMY TEST ENDPOINT HIT | Method: {Request.Method} | " +
                            $"Path: {Request.Path} | Content-Type: {Request.ContentType} | " +
                            $"Content-Length: {Request.ContentLength}");

                        #endregion


                        #region Read Raw Request

                        using var reader = new StreamReader(Request.Body);
                        var rawJson = await reader.ReadToEndAsync();

                        Console.WriteLine("========================================");
                        Console.WriteLine("TIMMY RAW REQUEST RECEIVED");
                        Console.WriteLine(rawJson);
                        Console.WriteLine("========================================");

                        _logger.LogInfo(
                            "TIMMY RAW REQUEST RECEIVED : " + rawJson);

                        #endregion


                        var cloudTime = DateTime.UtcNow
                            .ToString("yyyy-MM-dd HH:mm:ss");


                        #region Temporary Initial Registration ACK

                        /*
                         * TEMPORARY TEST BEHAVIOUR:
                         *
                         * During initial testing the TIMMY device may send an empty
                         * request or "{}" before registration is fully established.
                         *
                         * Return a temporary registration acknowledgement so that
                         * HTTPS connectivity and the device handshake can be verified.
                         *
                         * REMOVE this fallback before production implementation.
                         */

                        if (string.IsNullOrWhiteSpace(rawJson) ||
                            rawJson.Trim() == "{}")
                        {
                            Console.WriteLine("========================================");
                            Console.WriteLine("TIMMY EMPTY INITIAL REQUEST RECEIVED");
                            Console.WriteLine("RETURNING TEMPORARY REG ACK");
                            Console.WriteLine("========================================");

                            _logger.LogInfo(
                                "TIMMY initial empty request received. " +
                                "Returning temporary REG acknowledgement.");

                            return Ok(new
                            {
                                ret = "reg",
                                result = true,
                                cloudtime = cloudTime,
                                nosenduser = true
                            });
                        }

                        #endregion


                        #region Parse JSON

                        using var jsonDocument = JsonDocument.Parse(rawJson);
                        var root = jsonDocument.RootElement;

                        if (!root.TryGetProperty("cmd", out var cmdProperty))
                        {
                            Console.WriteLine("========================================");
                            Console.WriteLine("TIMMY CMD MISSING");
                            Console.WriteLine($"RAW JSON : {rawJson}");
                            Console.WriteLine("RETURNING TEMPORARY REG ACK");
                            Console.WriteLine("========================================");

                            _logger.LogInfo(
                                "TIMMY cmd missing. Returning temporary REG acknowledgement. " +
                                "Raw JSON: " + rawJson);

                            return Ok(new
                            {
                                ret = "reg",
                                result = true,
                                cloudtime = cloudTime,
                                nosenduser = true
                            });
                        }

                        var command = cmdProperty
                            .GetString()?
                            .Trim()
                            .ToLowerInvariant();

                        #endregion


                        #region Register

                        if (command == "reg")
                        {
                            var sn = root.TryGetProperty("sn", out var snProperty)
                                ? snProperty.GetString()
                                : null;

                            Console.WriteLine("========================================");
                            Console.WriteLine("TIMMY REG RECEIVED");
                            Console.WriteLine($"SN : {sn}");
                            Console.WriteLine("========================================");

                            _logger.LogInfo(
                                "TIMMY REG RECEIVED. SN : " + sn);

                            return Ok(new
                            {
                                ret = "reg",
                                result = true,
                                cloudtime = cloudTime,
                                nosenduser = true
                            });
                        }

                        #endregion


                        #region Attendance

                        if (command == "sendlog")
                        {
                            var sn = root.TryGetProperty("sn", out var snProperty)
                                ? snProperty.GetString()
                                : null;

                            var count =
                                root.TryGetProperty("count", out var countProperty)
                                && countProperty.TryGetInt32(out var parsedCount)
                                    ? parsedCount
                                    : 0;

                            var logIndex =
                                root.TryGetProperty("logindex", out var logIndexProperty)
                                && logIndexProperty.TryGetInt32(out var parsedLogIndex)
                                    ? parsedLogIndex
                                    : 0;

                            Console.WriteLine("========================================");
                            Console.WriteLine("TIMMY ATTENDANCE RECEIVED");
                            Console.WriteLine($"Device SN : {sn}");
                            Console.WriteLine($"Count     : {count}");
                            Console.WriteLine($"LogIndex  : {logIndex}");
                            Console.WriteLine("========================================");

                            _logger.LogInfo(
                                $"TIMMY ATTENDANCE RECEIVED | " +
                                $"SN: {sn} | Count: {count} | LogIndex: {logIndex}");

                            #region Attendance Records

                            if (root.TryGetProperty("record", out var recordProperty) &&
                                recordProperty.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var attendanceRecord in recordProperty.EnumerateArray())
                                {
                                    var enrollId =
                                        attendanceRecord.TryGetProperty(
                                            "enrollid",
                                            out var enrollIdProperty)
                                        && enrollIdProperty.TryGetInt32(out var parsedEnrollId)
                                            ? parsedEnrollId
                                            : 0;

                                    var attendanceTime =
                                        attendanceRecord.TryGetProperty(
                                            "time",
                                            out var timeProperty)
                                            ? timeProperty.GetString()
                                            : null;

                                    var mode =
                                        attendanceRecord.TryGetProperty(
                                            "mode",
                                            out var modeProperty)
                                        && modeProperty.TryGetInt32(out var parsedMode)
                                            ? parsedMode
                                            : 0;

                                    var inOut =
                                        attendanceRecord.TryGetProperty(
                                            "inout",
                                            out var inOutProperty)
                                        && inOutProperty.TryGetInt32(out var parsedInOut)
                                            ? parsedInOut
                                            : 0;

                                    var eventCode =
                                        attendanceRecord.TryGetProperty(
                                            "event",
                                            out var eventProperty)
                                        && eventProperty.TryGetInt32(out var parsedEvent)
                                            ? parsedEvent
                                            : 0;

                                    var temperature =
                                        attendanceRecord.TryGetProperty(
                                            "temp",
                                            out var tempProperty)
                                        && tempProperty.TryGetDouble(out var parsedTemperature)
                                            ? parsedTemperature
                                            : (double?)null;

                                    var hasImage =
                                        attendanceRecord.TryGetProperty(
                                            "image",
                                            out var imageProperty)
                                        && !string.IsNullOrWhiteSpace(
                                            imageProperty.GetString());

                                    var verificationType = mode switch
                                    {
                                        1 => "Fingerprint",
                                        2 => "Password",
                                        3 => "RFID Card",
                                        8 => "Face Recognition",
                                        _ => $"Unknown ({mode})"
                                    };

                                    var inOutText = inOut switch
                                    {
                                        0 => "IN",
                                        1 => "OUT",
                                        _ => $"Unknown ({inOut})"
                                    };

                                    Console.WriteLine("========================================");
                                    Console.WriteLine("TIMMY ATTENDANCE RECORD");
                                    Console.WriteLine($"Device SN          : {sn}");
                                    Console.WriteLine($"Enroll ID          : {enrollId}");
                                    Console.WriteLine($"Attendance Time    : {attendanceTime}");
                                    Console.WriteLine($"Mode               : {mode}");
                                    Console.WriteLine($"Verification Type  : {verificationType}");
                                    Console.WriteLine($"In/Out             : {inOutText}");
                                    Console.WriteLine($"Event              : {eventCode}");
                                    Console.WriteLine(
                                        $"Temperature        : {(temperature.HasValue ? temperature.Value.ToString() : "N/A")}");
                                    Console.WriteLine(
                                        $"Punch Image        : {(hasImage ? "YES" : "NO")}");

                                    switch (mode)
                                    {
                                        case 1:
                                            Console.WriteLine(
                                                "✅ FINGERPRINT ATTENDANCE DETECTED");
                                            break;

                                        case 2:
                                            Console.WriteLine(
                                                "✅ PASSWORD ATTENDANCE DETECTED");
                                            break;

                                        case 3:
                                            Console.WriteLine(
                                                "✅ RFID CARD ATTENDANCE DETECTED");
                                            break;

                                        case 8:
                                            Console.WriteLine(
                                                "✅ FACE ATTENDANCE DETECTED");
                                            break;

                                        default:
                                            Console.WriteLine(
                                                $"⚠ UNKNOWN VERIFICATION MODE : {mode}");
                                            break;
                                    }

                                    Console.WriteLine("========================================");

                                    _logger.LogInfo(
                                        $"TIMMY ATTENDANCE RECORD | " +
                                        $"SN: {sn} | " +
                                        $"EnrollId: {enrollId} | " +
                                        $"Time: {attendanceTime} | " +
                                        $"Mode: {mode} | " +
                                        $"VerificationType: {verificationType} | " +
                                        $"InOut: {inOutText} | " +
                                        $"Event: {eventCode} | " +
                                        $"Temperature: {temperature} | " +
                                        $"HasImage: {hasImage}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("========================================");
                                Console.WriteLine("TIMMY SENDLOG RECEIVED WITHOUT RECORD ARRAY");
                                Console.WriteLine($"RAW JSON : {rawJson}");
                                Console.WriteLine("========================================");

                                _logger.LogInfo(
                                    "TIMMY sendlog received without valid record array. " +
                                    "Raw JSON: " + rawJson);
                            }

                            #endregion

                            return Ok(new
                            {
                                ret = "sendlog",
                                result = true,
                                count,
                                logindex = logIndex,
                                cloudtime = cloudTime,
                                access = 1,
                                message = "OK"
                            });
                        }

                        #endregion


                        #region Heartbeat

                        if (command == "checklive")
                        {
                            var sn = root.TryGetProperty("sn", out var snProperty)
                                ? snProperty.GetString()
                                : null;

                            Console.WriteLine("========================================");
                            Console.WriteLine("TIMMY CHECKLIVE RECEIVED");
                            Console.WriteLine($"SN : {sn}");
                            Console.WriteLine("========================================");

                            _logger.LogInfo(
                                "TIMMY CHECKLIVE RECEIVED. SN : " + sn);

                            return Ok(new
                            {
                                ret = "checklive",
                                result = true,
                                cloudtime = cloudTime
                            });
                        }

                        #endregion


                        #region Unsupported Command

                        Console.WriteLine("========================================");
                        Console.WriteLine("TIMMY UNSUPPORTED COMMAND");
                        Console.WriteLine($"Command  : {command}");
                        Console.WriteLine($"Raw JSON : {rawJson}");
                        Console.WriteLine("========================================");

                        _logger.LogInfo(
                            $"TIMMY unsupported command received. " +
                            $"Command: {command}. Raw JSON: {rawJson}");

                        return Ok(new
                        {
                            ret = command,
                            result = false,
                            reason = "Unsupported command"
                        });

                        #endregion
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("========================================");
                        Console.WriteLine("TIMMY INVALID JSON");
                        Console.WriteLine(ex);
                        Console.WriteLine("========================================");

                        _logger.LogError(

                            "Invalid JSON received from TIMMY biometric device.");

                        return Ok(new
                        {
                            result = false,
                            reason = "Invalid JSON"
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("========================================");
                        Console.WriteLine("TIMMY TEST ERROR");
                        Console.WriteLine(ex);
                        Console.WriteLine("========================================");

                        _logger.LogError(

                            "Error while processing TIMMY test request.");

                        return Ok(new
                        {
                            result = false,
                            reason = "Server error"
                        });
                    }
                }

        #endregion


    }
}
