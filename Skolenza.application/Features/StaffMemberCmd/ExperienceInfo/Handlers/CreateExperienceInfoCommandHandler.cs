// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Experience Info Command Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateExperienceInfoCommand application component.
    /// </summary>
    public class CreateExperienceInfoCommand : IRequest<ApiResponse<GetStaffMemberExperienceResponseDTO>>
    {
        public CreateExperienceRequestDTO DTO { get; set; }

        public CreateExperienceInfoCommand(CreateExperienceRequestDTO dTO)
        {
            DTO = dTO;
        }
    }

    /// <summary>
    /// Handles CreateExperienceInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateExperienceInfoCommandHandler
     : IRequestHandler<CreateExperienceInfoCommand, ApiResponse<GetStaffMemberExperienceResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateExperienceInfoCommand> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IIdEncoderService _idEncoderService;
        public CreateExperienceInfoCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateExperienceInfoCommand> logger,
            ICommonRequestService commonRequestService,
            IFileStorageService fileStorageService, IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
            _idEncoderService =idEncoderService;
        }

        public async Task<ApiResponse<GetStaffMemberExperienceResponseDTO>> Handle(
            CreateExperienceInfoCommand request,
            CancellationToken cancellationToken)
        {
            List<string> uploadedFiles = new();

            try
            {
                _logger.LogInformation("🚀 CreateExperience started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request");

                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");

                if (validation.InstitutionId <= 0)
                    throw new ValidationErrorException("Invalid InstitutionId.");

                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                    request.DTO.CompanyName,
                    "Company name");
                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                    request.DTO.PreviousPositionTitle,
                    "PreviousPositionTitle");
                if (!request.DTO.StartDate.HasValue)
                    throw new ValidationErrorException("Start date is required.");
                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.ValidateDateRange(
                    request.DTO.StartDate,
                    request.DTO.EndDate);

                if (request.DTO.IsAnyGap)
                {
                    skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                        request.DTO.ReasonOfGap,
                        "Gap reason");
                    skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.ValidateDateRange(
                        request.DTO.GapYearFrom,
                        request.DTO.GapYearTo,
                        "Gap start date",
                        "Gap end date");
                }
                // ===============================
                // 2️⃣ TRANSACTION START
                // ===============================
                await _unitOfWork.BeginTransactionAsync();
                // ===============================
                // 5️⃣ FILE UPLOAD
                // ===============================


                // ===============================
                // 3️⃣ CREATE EXPERIENCE
                // ===============================
                var exp = new StaffMemberExperience
                {
                    StaffMemberId = staffMemberId,
                    CompanyName = request.DTO.CompanyName,
                    PreviousPositionTitle = request.DTO.PreviousPositionTitle,
                    StaffMemberIdOfCompany = request.DTO.StaffMemberIdOfCompany,
                    Ctc = request.DTO.Ctc,
                    StartDate = request.DTO.StartDate,
                    EndDate = request.DTO.EndDate,
                    Experience = request.DTO.Experience,
                    IsWfh = request.DTO.IsWFH,
                    WorkingCountryId = request.DTO.WorkingCountryId,
                    WorkingStateId = request.DTO.WorkingStateId,
                    WorkingDistrictId = request.DTO.WorkingDistrictId,
                    IsForeignExperience = request.DTO.IsForeignExperience,
                    ReasonForLeaving = request.DTO.ReasonForLeaving,
                    Remark = request.DTO.Remark,
                    ColleagueName = request.DTO.ColleagueName,
                    ColleaguePositionTitle = request.DTO.ColleaguePositionTitle,
                    ColleagueContactNumber = request.DTO.ColleagueContactNumber,

                    ReportingManagerName = request.DTO.ReportingManagerName,
                    ReportingManagerNumber = request.DTO.ReportingManagerNumber,

                    VerificationEmail = request.DTO.VerificationEmail,

                    IsAnyGap = request.DTO.IsAnyGap,
                    ReasonOfGap = request.DTO.ReasonOfGap,
                    GapYearFrom = request.DTO.GapYearFrom,
                    GapYearTo = request.DTO.GapYearTo,

                    IsExperienceVerified = false,
                    IsExperienceVerifiedByMail = false,
                    IsExperienceVerifiedByCall = false,

                    InfoVerifiedById = null,
                    InfoVerifiedDateTime = null,
                    IsInfoVerified = false,
                    IsEditAllowed = true,
                    AddedById = validation.UserStaffMemberId,
                    AddedDateTime = DateTime.UtcNow,
                    IsActive = true,
                    IsSoftDeleted = false
                };




                // ===============================
                // 4️⃣ DOCUMENTS (UPLOAD LIKE EDUCATION )
                // ===============================
                if (request.DTO.Documents != null)
                {
                    //  Filter only valid files (IMPORTANT)
                    var validDocs = request.DTO.Documents
                        .Where(d => d.File != null && d.File.Length > 0)
                        .ToList();

                    if (validDocs.Any())
                    {
                        exp.StaffMemberExperienceDocument = new List<StaffMemberExperienceDocument>();

                        foreach (var docDto in validDocs)
                        {
                            try
                            {
                                //  STEP 1: Copy file to memory (fix stream issue)
                                using var memoryStream = new MemoryStream();
                                await docDto.File!.CopyToAsync(memoryStream);
                                memoryStream.Position = 0;

                                //  STEP 2: Generate unique filename
                                var fileName = $"experience-{staffMemberId}-{Guid.NewGuid()}";

                                //  STEP 3: Folder path
                                string folderPath =
                                    $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                                    $"{ConstantValues.StaffMemberFolder}/{staffMemberId}/" +
                                    $"{ConstantValues.ExperienceFolder}";

                                //  STEP 4: File extension
                                var extension = Path.GetExtension(docDto.File.FileName);

                                //  STEP 5: Upload
                                var docPath = await _fileStorageService.UploadMultiFileAsync(
                                    memoryStream,
                                    folderPath,
                                    fileName,
                                    docDto.File.ContentType,
                                    extension);

                                //  STEP 6: Validate upload
                                if (string.IsNullOrWhiteSpace(docPath))
                                    throw new ApiException("File upload failed.", 500);

                                uploadedFiles.Add(docPath);

                                //  STEP 7: Save entity
                                exp.StaffMemberExperienceDocument.Add(new StaffMemberExperienceDocument
                                {
                                    DocumentType = docDto.DocumentType,
                                    FileName = fileName,
                                    FilePath = docPath,
                                    HasExperienceDocUploaded = true,
                                    AddedById = validation.UserStaffMemberId,
                                    AddedDateTime = DateTime.UtcNow,
                                    IsActive = true,
                                    IsSoftDeleted = false
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Experience document upload failed");
                                throw new ApiException("File upload failed.", 500);
                            }
                        }
                    }
                }
                // ===============================
                // 4️⃣ ADD
                // ===============================

                await _unitOfWork.StaffMemberExperienceRepository.AddAsync(exp);

                // ===============================
                // SAVE (ONLY HERE )
                // ===============================
                var rows = await   _unitOfWork.SaveChangesAsync(cancellationToken);

                if (rows <= 0 || exp.Id <= 0)
                {
                    _logger.LogError("❌ CreateExperience failed | SaveChanges failed");
                    throw new ApiException("Failed to save experience.", 500);
                }

                // ===============================
                // COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // MAP (NOW SAFE)
                // ===============================
                var response = new GetStaffMemberExperienceResponseDTO
                {
                    Id = exp.Id,
                    StaffMemberId = exp.StaffMemberId.ToString(),
                    CompanyName = exp.CompanyName,
                    PreviousPositionTitle = exp.PreviousPositionTitle,
                    StaffMemberIdOfCompany = exp.StaffMemberIdOfCompany,
                    Ctc = exp.Ctc,
                    StartDate = exp.StartDate,
                    EndDate = exp.EndDate,
                    Experience = exp.Experience,
                    IsWFH = exp.IsWfh,
                    WorkingCountryId = exp.WorkingCountryId,
                    WorkingStateId = exp.WorkingStateId,
                    WorkingDistrictId = exp.WorkingDistrictId,
                    IsForeignExperience = exp.IsForeignExperience,
                    ReasonForLeaving = exp.ReasonForLeaving,
                    Remark = exp.Remark,
                    ColleagueName = exp.ColleagueName,
                    ColleaguePositionTitle = exp.ColleaguePositionTitle,
                    ColleagueContactNumber = exp.ColleagueContactNumber,
                    ReportingManagerName = exp.ReportingManagerName,
                    ReportingManagerNumber = exp.ReportingManagerNumber,
                    VerificationEmail = exp.VerificationEmail,
                    IsAnyGap =  exp.IsAnyGap ,
                    ReasonOfGap = exp.ReasonOfGap,
                    GapYearFrom = exp.GapYearFrom,
                    GapYearTo = exp.GapYearTo,
                    IsEditAllowed = exp.IsEditAllowed,
                    IsInfoVerified = exp.IsInfoVerified,

                    //   DOCUMENT LIST

                    Documents = exp.StaffMemberExperienceDocument != null
                        ? exp.StaffMemberExperienceDocument.Select(d => new GetStaffMemberExperienceDocumentDTO
                        {
                            Id = d.Id,
                            DocumentType = d.DocumentType,
                            FileName = d.FileName,
                            FilePath = d.FilePath,

                            Url = !string.IsNullOrWhiteSpace(d.FilePath)
                                ? _fileStorageService.GetFileUrl(d.FilePath)
                                : null,

                            HasExperienceDocUploaded = d.HasExperienceDocUploaded ,
                        }).ToList()
                        : new List<GetStaffMemberExperienceDocumentDTO>()
                };

                // Completion
                response.CompletionPercentage =
                    CompletionCalculatorHelper.ExperiencePropCalculate(response);

                _logger.LogInformation("✅ Experience created successfully | Id: {Id}", exp.Id);

                return ApiResponse<GetStaffMemberExperienceResponseDTO>
                    .Success(response, "Experience saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CreateExperience failed");

                await _unitOfWork.RollbackTransactionAsync();

                //  FILE ROLLBACK

                foreach (var file in uploadedFiles)
                {
                    try { await _fileStorageService.DeleteFileAsync(file); }
                    catch { }
                }

                throw;
            }
        }

    }


    #endregion
}

