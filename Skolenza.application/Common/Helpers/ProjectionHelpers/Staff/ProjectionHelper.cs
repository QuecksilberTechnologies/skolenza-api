using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using Microsoft.Extensions.Configuration;
namespace skolenza.application.Common.Helpers.ProjectionHelpers.Staff
{
    public static class ProjectionHelper
    {
        // Manual projection for bank entity -> DTO with encrypted Id

        private static string SafeEncrypt(string? value, IEncryptionService encryptionService, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return encryptionService.Encrypt(value, institutionKey);
        }
        private static string EnsureEncrypted(string? value, IEncryptionService encryptionService, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // If already encrypted (heuristic check)
            if (value.Length > 10 && (value.Contains("=") || value.Contains("+") || value.Contains("/")))
                return value; // Return as-is

            return encryptionService.Encrypt(value, institutionKey);
        }







        /// <summary>
        /// Prevents double encryption by detecting pre-encrypted strings.
        /// </summary>

        //public static List<GetExperienceResponseDTO> ToGetExperienceResponseDTOs(List<GetExperienceResponseDTO> entities, IEncryptionService encryptionService, string institutionKey, string encryptedStaffMemberId)
        //{
        //    if (entities == null || entities.Count == 0)
        //        return new List<GetExperienceResponseDTO>();

        //    return entities.Select(e => new GetExperienceResponseDTO
        //    {
        //        Id = SafeEncrypt(e.UserStaffMemberId, encryptionService, institutionKey),
        //        StaffMemberId = encryptedStaffMemberId,
        //        CompanyName = e.CompanyName ?? string.Empty,
        //        JobTitle = e.JobTitle ?? string.Empty,
        //        StartDate = e.StartDate,
        //        EndDate = e.EndDate,
        //        ReasonForLeaving = e.ReasonForLeaving ?? string.Empty,
        //        Remark = e.Remark ?? string.Empty,
        //        ExperienceTypeId = e.ExperienceTypeId,
        //        Location = e.Location ?? string.Empty,
        //        CTC = e.CTC,
        //        ReportingManagerName = e.ReportingManagerName ?? string.Empty,
        //        ReportingManagerNumber = e.ReportingManagerNumber ?? string.Empty,
        //        ReportingManagerEmail = e.ReportingManagerEmail ?? string.Empty,
        //        WorkedWithName = e.WorkedWithName ?? string.Empty,
        //        WorkedWithContactNumber = e.WorkedWithContactNumber ?? string.Empty,
        //        WorkedWithDesignation = e.WorkedWithDesignation ?? string.Empty,
        //        ExperienceLetterPath = e.ExperienceLetterPath ?? string.Empty,
        //        Comment = e.Comment ?? string.Empty,
        //        AddedById = SafeEncrypt(e.AddedById, encryptionService, institutionKey),

        //    }).ToList();
        //}


        public static List<GetDependentResponseDTO> ToGetDependentResponseDTOs(
    List<GetDependentResponseDTO> entities,
    IIdEncoderService encoderService,
    string institutionKey,
    IConfiguration configuration ,IFileStorageService _fileStorageService)
        {
            if (entities == null || !entities.Any())
                return new List<GetDependentResponseDTO>();

            string baseUrl = configuration["FileSettings:BaseUrl"] ?? string.Empty;
            foreach (var item in entities)
            {
                if (item == null)
                    continue;

                // 🔐 Encode StaffMemberId
                if (long.TryParse(item.StaffMemberId, out long empRawId) && empRawId > 0)
                    item.StaffMemberId = encoderService.EncodeId_long(empRawId, institutionKey);

                // 📁 File path
               

                if (!string.IsNullOrEmpty(item.FilePath))
                    item.FilePath = _fileStorageService.GetFileUrl(item.FilePath);

                // 📊 Completion %
                item.CompletionPercentage =
                    CompletionCalculatorHelper.DependentPropCalculate(item);

                // ✅ ENUM → STRING (CORRECT PLACE)
                if (item.Relation.HasValue &&
                    Enum.IsDefined(typeof(RelationDependant), item.Relation.Value))
                {
                    item.RelationType =
                        Enum.GetName(typeof(RelationDependant), item.Relation.Value);
                }
                else
                {
                    item.RelationType = RelationDependant.Other.ToString();
                }

                // 🧹 Null safety
             

                item.StaffMemberId ??= string.Empty;
                item.DependentName ??= string.Empty;
                item.Remark ??= string.Empty;
                item.Description ??= string.Empty;
            }

            return entities;
        }


        public static List<GetStaffMemberIdentityResponseDTO> ToGetIdentityResponseDTO(
        IEnumerable<GetStaffMemberIdentitySp> entities,
        IIdEncoderService encoder,
        string institutionKey)
        {
            if (entities == null)
                return new List<GetStaffMemberIdentityResponseDTO>();

            return entities.Select(e => new GetStaffMemberIdentityResponseDTO
            {
                // 🔐 ONLY StaffMemberId is encoded (ASXER rule)
                StaffMemberId = e.StaffMemberId.HasValue
                    ? encoder.EncodeId_long(e.StaffMemberId.Value, institutionKey)
                    : null,
                
                // 🌍 Country
                Id = e.Id,
                CountryCode = e.CountryCode,
                CountryName = e.CountryName,

                // 🗂 Identity Category
                IdentityCategoryName = e.IdentityCategoryName,

                // 📄 Document (RAW master IDs)
                IdentityCategoryDocumentId = e.IdentityCategoryDocumentId,
                DocumentCode = e.DocumentCode,
                DocumentName = e.DocumentName,
                Description = e.Description,
                IsMandatory = e.IsMandatory,
                

                // 👤 StaffMemberIdentity
                StaffMemberIdentityId = e.StaffMemberIdentityId,
                IdentityValue = e.IdentityValue,

                // ✅ Flags
                IsVerified = e.IsVerified,
                IsEditAllowed = e.IsEditAllowed,
                HasIdentityUploaded = e.HasIdentityUploaded,

                // 📆 Validity
                EffectiveFrom = e.EffectiveFrom.HasValue
                   ? e.EffectiveFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                   : null,
                  EffectiveTo = e.EffectiveTo.HasValue
                   ? e.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                   : null,

                IsActive = e.IsActive,
                CompletionPercentage = StaffMemberProfileCompletionCalculator.CalculateIdentityRow(
                    e.IdentityValue,
                    e.HasIdentityUploaded == true,
                    e.IsMandatory,
                    e.StaffMemberIdentityId.HasValue)
            }).ToList();
        }

        public static StaffMemberProfileSummaryInfo? ToGetProfileSummaryResponseDTO(
       StaffMemberProfileSummaryInfo? entity,
       IIdEncoderService encoderService,
       string institutionKey)
        {
            if (entity == null)
                return null;

            // 🔐 Encode StaffMemberId
            if (!string.IsNullOrWhiteSpace(entity.StaffMemberId) &&
                long.TryParse(entity.StaffMemberId, out long rawId) &&
                rawId > 0)
            {
                entity.StaffMemberId = encoderService.EncodeId_long(rawId, institutionKey);
            }

            // 🧹 Null safety (only fields present in DTO)
            entity.FullName ??= string.Empty;
            entity.DesignationType ??= string.Empty;
            entity.DepartmentType ??= string.Empty;
            entity.StaffMemberCode ??= string.Empty;
          
            entity.MobileNumber ??= string.Empty;
            entity.OffilcialEmail ??= string.Empty;

            return entity;
        }

        public static List<GetAllStaffMemberInfoResponseDTO> ToGetAllStaffMemberInfoResponseDTOs(
     PagedResponseDTO<GetAllStaffMemberInfoResponseDTO> entities,
      IIdEncoderService encoderService,
      string institutionKey,
      IFileStorageService fileStorageService,
      long requestingStaffMemberId,
      int requestingRoleTypeId)
        {
           


            
            if (entities == null || !entities.Data.Any())
                return new List<GetAllStaffMemberInfoResponseDTO>();

            foreach (var item in entities.Data)
            {
                var hasStaffMemberId = long.TryParse(item.StaffMemberId, out long rawId) && rawId > 0;
                if (hasStaffMemberId &&
                    !CanViewStaffMemberPersonalData(
                        requestingStaffMemberId,
                        rawId,
                        requestingRoleTypeId))
                {
                    RedactStaffMemberListPersonalData(item);
                }

                if (hasStaffMemberId)
                {
                    item.StaffMemberId = encoderService.EncodeId_long(rawId, institutionKey);
                }
                item.StaffMemberImagePath ??= null;

                // 📁 Final Image URL build
                if (!string.IsNullOrEmpty(item.StaffMemberImagePath))
                    item.StaffMemberImagePath = fileStorageService.GetFileUrl(item.StaffMemberImagePath);

                item.EmployementCode ??= string.Empty;
                item.FirstName ??= string.Empty;
                item.LastName ??= string.Empty;
                item.MiddleName ??= string.Empty;
                item.OfficialEmail ??= string.Empty;

            }

            return entities.Data;
        }

        /// <summary>
        /// Determines whether the current Institution user may receive another StaffMember's personal details.
        /// StaffMember-role users receive full details only for their own record. An Admin may
        /// receive full details, while a Manager's direct-report rows stay directory-only.
        /// Unknown role types fail closed.
        /// </summary>
        public static bool CanViewStaffMemberPersonalData(
            long requestingStaffMemberId,
            long subjectStaffMemberId,
            int requestingRoleTypeId)
        {
            if (requestingStaffMemberId <= 0 || subjectStaffMemberId <= 0)
            {
                return false;
            }

            if (requestingStaffMemberId == subjectStaffMemberId)
            {
                return true;
            }

            return requestingRoleTypeId == ConstantValues.RoleTypeAdmin;
        }

        /// <summary>
        /// Removes personal and administrative fields from a directory row while retaining
        /// the staffMember's basic organizational identity for StaffMember-role viewers.
        /// </summary>
        private static void RedactStaffMemberListPersonalData(GetAllStaffMemberInfoResponseDTO staffMember)
        {
            staffMember.EmployementCode = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.DateOfBirth = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.DateOfOnBoarding = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.Nationality = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.CountryCode = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.EmergencyContactPerson = AppConstants.ResponseValues.WithheldStaffMemberField;
            staffMember.CompletionPercentage = null;
            staffMember.HasImagePicUploaded = null;
        }

        /// <summary>
        /// Limits another StaffMember's compact profile summary to directory-level information.
        /// Official contact details remain available for workplace communication, while the
        /// internal staffMember code is visible only to the subject StaffMember, Admin, or Manager.
        /// </summary>
        public static StaffMemberProfileSummaryInfo? ApplyStaffMemberProfileSummaryVisibility(
            StaffMemberProfileSummaryInfo? staffMember,
            long requestingStaffMemberId,
            long subjectStaffMemberId,
            int requestingRoleTypeId)
        {
            if (staffMember is null ||
                CanViewStaffMemberPersonalData(
                    requestingStaffMemberId,
                    subjectStaffMemberId,
                    requestingRoleTypeId))
            {
                return staffMember;
            }

            staffMember.StaffMemberCode = AppConstants.ResponseValues.WithheldStaffMemberField;
            return staffMember;
        }

 
     
        public static List<GetAssetResponseDTO> ToGetAssetResponseDTOs( List<GetAssetResponseDTO> source, IIdEncoderService encoderService,
        string institutionKey, IConfiguration configuration, IFileStorageService fileStorageService)
        {
            if (source == null || source.Count == 0)
                return source ?? new List<GetAssetResponseDTO>();
 
            foreach (var item in source)
            {
                if (item == null)
                    continue;
 

                // 🔹 Build full image URL

                if (!string.IsNullOrEmpty(item.AssetImagePath))
                    item.AssetImagePath = fileStorageService.GetFileUrl(item.AssetImagePath);
            }

            return source;
        }
        public static SummaryStaffMemberInfo? ToGetSummaryResponseDTO(
           SummaryStaffMemberInfo? entity,
           IIdEncoderService encoderService, string institutionKey, IConfiguration configuration, IFileStorageService _fileStorageService
          )
        {
            if (entity == null)
                return null;

            // =====================================================
            // 🔐 ENCODE STAFF_MEMBER ID
            // =====================================================
            if (!string.IsNullOrWhiteSpace(entity.StaffMemberId)
                && long.TryParse(entity.StaffMemberId, out long rawId)
                && rawId > 0)
            {
                entity.StaffMemberId = encoderService.EncodeId_long(rawId, institutionKey);
            }
            
           

            if (!string.IsNullOrEmpty(entity.ProfileImage))
                entity.ProfileImage  = _fileStorageService.GetFileUrl(entity.ProfileImage);


            // =====================================================
            // 🔁 RELATION ENUM → STRING
            // =====================================================
            if (entity.Relation.HasValue &&
                Enum.IsDefined(typeof(EmergencyContactRelation), entity.Relation.Value))
            {
                entity.RelationName =
                    Enum.GetName(
                        typeof(EmergencyContactRelation),
                        entity.Relation.Value
                    );
            }
            else
            {
                entity.RelationName = "Other";
            }

            // =====================================================
            // 🧹 NULL SAFETY (ONLY VALID FIELDS)
            // =====================================================
            entity.EmergencyContactPerson ??= string.Empty;
            entity.EmergencyContactNumber ??= string.Empty;
            entity.StaffMemberCode ??= string.Empty;
            entity.BloodGroup ??= string.Empty;
            entity.MobileNumber ??= string.Empty;
            entity.PersonalEmail ??= string.Empty;
            entity.CountryCode ??= string.Empty;
            entity.Designation ??= string.Empty;
            entity.Department ??= string.Empty;
            entity.StaffEmploymentTypeName ??= string.Empty;
            entity.ProfileImage ??= string.Empty;
            entity.City ??= string.Empty;
            entity.Address ??= string.Empty;
            entity.CurrentSalaryStatusRemark ??= string.Empty;
            entity.RoleType ??= null;

            // =====================================================
            // 🌍 UTC DATE NORMALIZATION
            // =====================================================
            entity.LastLoginDateTime = entity.LastLoginDateTime.HasValue
                ? DateTime.SpecifyKind(entity.LastLoginDateTime.Value, DateTimeKind.Utc)
                : null;

            entity.DateOfJoining = entity.DateOfJoining.HasValue
                ? DateTime.SpecifyKind(entity.DateOfJoining.Value, DateTimeKind.Utc)
                : null;

            return entity;
        }

        public static GetBaseStaffMemberResponseDTO? ToGetBaseInfoResponseDTO(
            GetBaseStaffMemberResponseDTO? entity,
            IIdEncoderService encoderService,
            string institutionKey)
        {
            if (entity == null)
                return null;

            // 🔐 Encode StaffMember Id
            if (!string.IsNullOrWhiteSpace(entity.Id)
                && long.TryParse(entity.Id, out long rawId)
                && rawId > 0)
            {
                entity.Id = encoderService.EncodeId_long(rawId, institutionKey);
            }

            // ✅ RELATION ENUM → STRING
            if (entity.Relation.HasValue &&
                Enum.IsDefined(typeof(EmergencyContactRelation), entity.Relation.Value))
            {
                entity.RelationType =
                    Enum.GetName(typeof(EmergencyContactRelation), entity.Relation.Value);
            }
            else
            {
                entity.RelationType = "Other";
            }

            // 🧹 Null safety
            entity.FirstName ??= string.Empty;
            entity.LastName ??= string.Empty;
            entity.MiddleName ??= string.Empty;
            entity.OfficialEmail ??= string.Empty;
            entity.EmployementCode ??= string.Empty;

            // 🌍 UTC Date Handling
            entity.DateOfBirth = entity.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(entity.DateOfBirth.Value, DateTimeKind.Utc)
                : null;

            entity.DateOfOnBoarding = entity.DateOfOnBoarding.HasValue
                ? DateTime.SpecifyKind(entity.DateOfOnBoarding.Value, DateTimeKind.Utc)
                : null;

            entity.DateOfExit = entity.DateOfExit.HasValue
                ? DateTime.SpecifyKind(entity.DateOfExit.Value, DateTimeKind.Utc)
                : null;

            return entity;
        }

        public static List<GetBaseStaffMemberResponseDTO> ToGetBaseInfoListResponseDTOs(List<GetBaseStaffMemberResponseDTO> entities,
        IIdEncoderService encoderService,
      string institutionKey)
        {
            if (entities == null || !entities.Any())
                return new List<GetBaseStaffMemberResponseDTO>();

            foreach (var item in entities)
            {
                // ✅ Sirf Id encode karo (agar valid hai)
                if (long.TryParse(item.Id, out long rawId) && rawId > 0)
                {
                    item.Id = encoderService.EncodeId_long(rawId, institutionKey);
                }

                // ✅ RELATION ENUM → STRING
                if (item.Relation.HasValue &&
                    Enum.IsDefined(typeof(EmergencyContactRelation), item.Relation.Value))
                {
                    item.RelationType =
                        Enum.GetName(typeof(EmergencyContactRelation), item.Relation.Value);
                }
                else
                {
                    item.RelationType = "Other";
                }

                // ✅ Optional null-safe cleanup (lightweight safety)
                item.EmployementCode ??= string.Empty;
                item.FirstName ??= string.Empty;
                item.LastName ??= string.Empty;
                item.MiddleName ??= string.Empty;
                item.OfficialEmail ??= string.Empty;
                item.DateOfBirth = item.DateOfBirth.HasValue
                   ? DateTime.SpecifyKind(item.DateOfBirth.Value, DateTimeKind.Utc)
                   : null;
            }

            return entities;
        }

        public static List<GetContactResponseDTO> ToGetContactResponseDTOs(
                PagedResponseDTO<GetContactResponseDTO> pagedResult,
                  IIdEncoderService encoderService,
                   string institutionKey)
        {
            if (pagedResult?.Data == null || !pagedResult.Data.Any())
                return new List<GetContactResponseDTO>();

            foreach (var item in pagedResult.Data)
            {
                if (item == null)
                    continue;
                               

                // ✅ Encode StaffMemberId
                if (long.TryParse(item.StaffMemberId, out long empRawId) && empRawId > 0)
                    item.StaffMemberId = encoderService.EncodeId_long(empRawId, institutionKey);

                // ✅ Lightweight null safety
                item.Id = item.Id;
                item.StaffMemberId ??= string.Empty;
                item.ContactNumber ??= string.Empty;
                item.Email ??= string.Empty;
                item.ContactName ??= string.Empty;
                item.Relation ??= 0;
                item.ContactType ??= 0;
                item.CountryName ??= string.Empty;
                item.StateName ??= string.Empty;
                item.DistrictName ??= string.Empty;
                item.HouseNo ??= string.Empty;
                item.LandMark ??= string.Empty;
                item.Street ??= string.Empty;
                item.Address ??= string.Empty;
                item.Remark ??= string.Empty;
                item.Description ??= string.Empty;
                item.IsPrimary = item.IsPrimary ;
                item.IsActive = item.IsActive ?? false;
                item.IsEditAllowed = item.IsEditAllowed ?? false;
                item.IsInfoVerified = item.IsInfoVerified ?? false;
                item.CountryId = item.CountryId ?? 0;
                item.StateId = item.StateId ?? 0;
                item.DistrictId = item.DistrictId ?? 0;
                item.InfoVerifiedDateTime = item.InfoVerifiedDateTime.HasValue
                   ? DateTime.SpecifyKind(item.InfoVerifiedDateTime.Value, DateTimeKind.Utc)
                   : null;
                

            }

            return pagedResult.Data;
        }

        public static List<GetBankResponseDTO> ToGetBankResponseDTOs(
     PagedResponseDTO<GetBankResponseDTO> entities,
     IIdEncoderService encoderService,
     IEncryptionService encryptionService,
     string institutionKey,
     IConfiguration configuration, IFileStorageService _fileStorageService)
        {
            if (entities?.Data == null || !entities.Data.Any())
                return new List<GetBankResponseDTO>();

           

            foreach (var item in entities.Data)
            {
                if (item == null)
                    continue;

                // 🔐 Encode StaffMemberId
                if (!string.IsNullOrWhiteSpace(item.StaffMemberId)
                    && long.TryParse(item.StaffMemberId, out long rawEmpId)
                    && rawEmpId > 0)
                {
                    item.StaffMemberId = encoderService.EncodeId_long(rawEmpId, institutionKey);
                }

                item.AccountNumber = DecryptBankValue(item.AccountNumber, encryptionService, institutionKey);
                item.IFSCCode = DecryptBankValue(item.IFSCCode, encryptionService, institutionKey);
                item.UPIId = DecryptBankValue(item.UPIId, encryptionService, institutionKey);

               if (!string.IsNullOrEmpty(item.FilePath))
                    item.FilePath = _fileStorageService.GetFileUrl(item.FilePath);

                else item.FilePath = null;


                // 🧹 Null safety
                item.FileName ??= string.Empty;
                item.FileType ??= 0;
                item.StaffMemberId ??= string.Empty;
            }

            return entities.Data;
        }

        private static string? DecryptBankValue(
            string? encryptedValue,
            IEncryptionService encryptionService,
            string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptedValue))
                return null;

            return encryptionService.Decrypt(encryptedValue, institutionKey);
        }

        private static string EncodeId(
    string? rawId,
    IIdEncoderService encoder,
    string institutionKey)
        {
            if (long.TryParse(rawId, out long id) && id > 0)
                return encoder.EncodeId_long(id, institutionKey);

            return string.Empty;
        }

       
         

        public static List<GetEducationResponseDTO> ToGetEducationResponseDTOs(
        PagedResponseDTO<GetEducationResponseDTO> source,
        IIdEncoderService encoderService,
        string institutionKey, IConfiguration configuration, IFileStorageService _fileStorageService)
        {
            
            if (source?.Data == null || source.Data.Count == 0)
                return new();

            return source.Data
                .Where(x => x != null)
                .Select(item => new GetEducationResponseDTO
                {
                    Id = item.Id,
                    StaffMemberId = EncodeId(item.StaffMemberId, encoderService, institutionKey),

                    Degree = item.Degree,
                    InstituteName = item.InstituteName,
                    ScoreType = item.ScoreType,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                  
                    FilePath = !string.IsNullOrEmpty(item.FilePath) ? _fileStorageService.GetFileUrl(item.FilePath) : null,

                    FileName = item.FileName,
                    IsActive = item.IsActive,
                    IsEditAllowed = item.IsEditAllowed,
                    IsInfoVerified = item.IsInfoVerified,
                    EducationGap = item.EducationGap.HasValue ? item.EducationGap.Value: false,
                    HasEducationDocUploded = item.HasEducationDocUploded,
                    CompletionPercentage = item.CompletionPercentage,
                    GradeDivision = item.GradeDivision,
                    ScoreValue = item.ScoreValue,
                    Remark = item.Remark,
                    


                })
                .ToList();
        }


        //public static List<GetDepartmentResponseDTO> ToGetDepartmentResponseDTOs(List<GetDepartmentResponseDTO> entities, IIdEncoderService idEncoderService, string institutionKey)
        //{
        //    if (entities == null || entities.Count == 0)
        //        return new List<GetDepartmentResponseDTO>();

        //    return entities.Select(e => new GetDepartmentResponseDTO
        //    {

        //        Id =  e.,
        //        //AddedById = idEncoderService.EncodeId(SafeParser.TryParseLong(e.AddedById), institutionKey),
        //        //UpdatedById = idEncoderService.EncodeId(SafeParser.TryParseLong(e.UpdatedById), institutionKey),
        //        DepartmentName = e.DepartmentName ?? string.Empty,
        //        Description = e.Description ?? string.Empty,
        //        IsActive = e.IsActive
        //    }).ToList();
        //}

    




        //public static List<GetRoleResponseDTO> ToGetRoleResponseDTOs(List<GetRoleResponseDTO> entities, IEncryptionService encryptionService, string institutionKey)
        //{
        //    if (entities == null || entities.Count == 0)
        //        return new List<GetRoleResponseDTO>();

        //    return entities.Select(e => new GetRoleResponseDTO
        //    {
        //        AddedById = SafeEncrypt(e.AddedById?.ToString(), encryptionService, institutionKey),
        //        UpdatedById = SafeEncrypt(e.UpdatedById?.ToString(), encryptionService, institutionKey),
        //        RoleName = e.RoleName ?? string.Empty,
        //        Remark = e.Remark ?? string.Empty,
        //        IsActive = e.IsActive
        //    }).ToList();
        //}

        //public static List<GetDesignationResponseDTO> ToGetDesignationResponseDTOs(List<GetDesignationResponseDTO> entities, IEncryptionService encryptionService, string institutionKey)
        //{
        //    if (entities == null || entities.Count == 0)
        //        return new List<GetDesignationResponseDTO>();

        //    return entities.Select(e => new GetDesignationResponseDTO
        //    {
        //        Id = SafeEncrypt(e.Id?.ToString(), encryptionService, institutionKey),
        //        AddedById = SafeEncrypt(e.AddedById?.ToString(), encryptionService, institutionKey),
        //        UpdatedById = SafeEncrypt(e.UpdatedById?.ToString(), encryptionService, institutionKey),
        //        DepartmentName = e.DepartmentName ?? string.Empty,
        //        Description = e.Description ?? string.Empty,
        //        IsActive = e.IsActive
        //    }).ToList();
        //}

      //  public static List<GetStaffMemberImageReponseDTO> ToGetStaffMemberImageResponseDTOs(
      //PagedResponseDTO<GetStaffMemberImageReponseDTO> entities,
      //IIdEncoderService encoderService,
      //string institutionKey,
      //IConfiguration configuration)
      //  {
      //      if (entities == null || entities.Items == null || !entities.Items.Any())
      //          return new List<GetStaffMemberImageReponseDTO>();

      //      // 🔥 Base URL & Default Image — Correct Way
      //      string baseUrl = configuration["FileSettings:BaseUrl"] ?? string.Empty;
      //      string defaultImg = configuration["FileSettings:DefaultImage"] ?? string.Empty;

      //      var result = new List<GetStaffMemberImageReponseDTO>();

      //      foreach (var item in entities.Items)
      //      {
      //          if (item == null) continue;

      //          // 🔐 Encode Image Id

      //          item.Id = item.Id.ToString();

      //          // 🔐 Encode StaffMember Id
      //          if (long.TryParse(item.StaffMemberId, out long empRawId) && empRawId > 0)
      //              item.StaffMemberId = encoderService.EncodeId_long(empRawId, institutionKey);

      //          // 🧹 Null sanitization
      //          item.Id ??= string.Empty;
      //          item.StaffMemberId ??= string.Empty;
      //          item.FilePath ??= string.Empty;

      //          // 📁 Final Image URL build
      //          if (!string.IsNullOrEmpty(item.FilePath))
      //              item.FilePath = $"{baseUrl}{item.FilePath}";


      //          // These are already same — keeping for clarity
      //          item.IsPrimary = item.IsPrimary;
      //          item.IsActive = item.IsActive;
      //          item.CompletionPercentage = item.CompletionPercentage;

      //          result.Add(item);
      //      }

      //      return result;
      //  }
                    




    }

}

