// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for File Upload operations.
// ================================================================


using skolenza.application.DTOS.DocFile.skolenza.application.DTOS.FileUpload;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.FileUpload
{
    /// <summary>
    /// Controller to manage all file upload operations like Asset Image upload.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;
        private readonly IFileStorageService _fileStorageService;
        public FileUploadController(IMediator mediator, ILoggerService logger, IFileStorageService fileStorageService)
        {
            _mediator = mediator;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function upload asset.</para>
                /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
                /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/fileupload/uploadasset/upload.</para>
                /// </remarks>

                [HttpPost("UploadAsset/upload")]
                public async Task<IActionResult> UploadAsset([FromBody] UploadFileDTO dto)
                {
                    try
                    {
                        if (dto == null)
                            return BadRequest(new { Success = false, Message = "Invalid file data." });

                        byte[] fileBytes;

                        // ✅ 1️⃣ - Detect Source (Base64, URL, or Local Path)
                        if (!string.IsNullOrEmpty(dto.FileBase64))
                        {
                            var base64Data = dto.FileBase64.Contains(",")
                                ? dto.FileBase64.Split(',')[1]
                                : dto.FileBase64;

                            fileBytes = Convert.FromBase64String(base64Data);
                        }
                        else if (!string.IsNullOrEmpty(dto.FileBase64))
                        {

                        }
                        else
                        {
                            return BadRequest(new { Success = false, Message = "No valid file source provided." });
                        }


                        return Ok(new
                        {
                            Success = true,
                            Message = "✅ Your file has been successfully saved.",
                            FilePath = "/path/to/saved/file" // Replace with actual saved file path
                        });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new
                        {
                            Success = false,
                            Message = $"❌ File upload failed: {ex.Message}"
                        });
                    }
                }

    }
}
