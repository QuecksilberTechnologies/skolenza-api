// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Executes stored-function queries, including authoritative Institution operational permission reads.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOs.Operation;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.DTOS.StoreProcedures.DashboardSummeries;
using skolenza.application.DTOS.Institution;
using skolenza.application.DTOS.UserLogin;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

using System.Data; 
namespace skolenza.persistance.Repositories
{
    public class StoreProcedureRepository : IStoreProcedureRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<StoreProcedureRepository> _logger;
      // 
        private readonly IMapper _mapper;
        

        public StoreProcedureRepository(SkolenzaDbContext context, ILogger<StoreProcedureRepository> logger, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
          //  
            _mapper = mapper;   
        }

 
        public async Task<GetStaffMemberCodePatternResponseDTO?> GetInstitutionStaffMemberCodePatternAsync(
      StaffMemberCodePatternRequestDTO request)
        {
            try
            {
                var institutionParam = new NpgsqlParameter("p_institutionid", request.InstitutionId);

                var result = await _context
                    .Set<GetStaffMemberCodePatternResponseDTO>()
                    .FromSqlRaw(
                        @"SELECT *
                  FROM skolenza.""GetStaffMemberCodePatternByInstitution""(@p_institutionid)",
                        institutionParam)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember code pattern for InstitutionId: {InstitutionId}",
                    request.InstitutionId);

                throw;
            }
        }
        //public async Task<List<SubscribedModuleResponseDTO>> GetSubscribedModulesByInstitutionAsync(long institutionId)
        //{
        //    try
        //    {
        //       // await using var context = await _contextFactory.CreateDbContextAsync();

        //        _logger.LogInformation("Fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);

        //        var institutionIdParam = new NpgsqlParameter("@InstitutionId", institutionId);

        //        string sqlQuery = "EXEC Skolenza.GetSubscribedModuleByInstitutionId @InstitutionId";

        //        var result = await _context.SubscribedModuleResponseDTOs
        //            .FromSqlRaw(sqlQuery, institutionIdParam)
        //            .ToListAsync();

        //        if (result == null || !result.Any())
        //        {
        //            _logger.LogWarning("No modules found for InstitutionId: {InstitutionId}", institutionId);
        //            return new List<SubscribedModuleResponseDTO>();
        //        }

        //        _logger.LogInformation("Total {Count} modules fetched for InstitutionId: {InstitutionId}", result.Count, institutionId);
        //        return result;
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL Exception occurred while fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);
        //        return new List<SubscribedModuleResponseDTO>();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);
        //        return new List<SubscribedModuleResponseDTO>();
        //    }
        //}



        //public async Task<List<SubscribedModuleResponseDTO>> GetSubscribedModulesByInstitutionAsync(long institutionId)
        //{
        //    try
        //    {
        //       // await using var context = await _contextFactory.CreateDbContextAsync();

        //        _logger.LogInformation("Fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);

        //        var institutionIdParam = new NpgsqlParameter("@InstitutionId", institutionId);

        //        string sqlQuery = "EXEC Skolenza.GetSubscribedModuleByInstitutionId @InstitutionId";

        //        var result = await _context.SubscribedModuleResponseDTOs
        //            .FromSqlRaw(sqlQuery, institutionIdParam)
        //            .ToListAsync();

        //        if (result == null || !result.Any())
        //        {
        //            _logger.LogWarning("No modules found for InstitutionId: {InstitutionId}", institutionId);
        //            return new List<SubscribedModuleResponseDTO>();
        //        }

        //        _logger.LogInformation("Total {Count} modules fetched for InstitutionId: {InstitutionId}", result.Count, institutionId);
        //        return result;
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL Exception occurred while fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);
        //        return new List<SubscribedModuleResponseDTO>();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);
        //        return new List<SubscribedModuleResponseDTO>();
        //    }
        //}

        public async Task<List<SubscribedModuleResponseDTO>> GetSubscribedModulesByInstitutionAsync(long institutionId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching subscribed modules for InstitutionId: {InstitutionId}", institutionId);

                var institutionParam = new NpgsqlParameter("p_institutionid", institutionId);

                var result = await _context
                    .Set<SubscribedModuleResponseDTO>()
                    .FromSqlRaw(
                        @"SELECT * 
                  FROM skolenza.""GetSubscribedModuleByInstitutionId""(@p_institutionid)",
                        institutionParam)
                    .AsNoTracking()
                    .ToListAsync();

                if (result.Count == 0)
                {
                    _logger.LogWarning(
                        "No modules found for InstitutionId: {InstitutionId}", institutionId);
                }

                _logger.LogInformation(
                    "Total {Count} modules fetched for InstitutionId: {InstitutionId}",
                    result.Count, institutionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while fetching subscribed modules for InstitutionId: {InstitutionId}",
                    institutionId);

                return new List<SubscribedModuleResponseDTO>();
            }
        }



        // Byte Array to Base64 Converter
        public string ConvertToBase64(byte[] iconData)
        {
            if (iconData == null || iconData.Length == 0)
                return string.Empty;

            return Convert.ToBase64String(iconData);
        }

        public async Task<bool> UpdateLoginCredential(LoginRequestDTO loginRequest)
        {
            try
            {
                _logger.LogInformation(
                    "Updating login credential for LoginId: {LoginId}",
                    loginRequest.LoginId);

                var parameters = new[]
                {
            new NpgsqlParameter("p_loginid", loginRequest.LoginId ?? (object)DBNull.Value),
            new NpgsqlParameter("p_latitude", loginRequest.Latitude.ToString()),
            new NpgsqlParameter("p_longitude", loginRequest.Longitude.ToString()),
            new NpgsqlParameter("p_logindevice", loginRequest.LoginDevice),
            new NpgsqlParameter("p_ipaddresslocal", loginRequest.IpAddressLocal ?? (object)DBNull.Value),
            new NpgsqlParameter("p_ipaddresspublic", loginRequest.IpAddressPublic ?? (object)DBNull.Value),
            new NpgsqlParameter("p_macaddress", loginRequest.MacAddress ?? (object)DBNull.Value)
        };

                var result = await _context
                    .Set<UpdateLoginCredentialResultDTO>()
                    .FromSqlRaw(
                        @"SELECT * FROM skolenza.""UpdateLoginCredential""(
                    @p_loginid,
                    @p_latitude,
                    @p_longitude,
                    @p_logindevice,
                    @p_ipaddresslocal,
                    @p_ipaddresspublic,
                    @p_macaddress)",
                        parameters)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return result?.Status == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating login credential for LoginId {LoginId}",
                    loginRequest.LoginId);

                return false;
            }
        }

        //public async Task<bool> UpdateLoginCredential(LoginRequestDTO loginRequest)
        //{
        //    try
        //    {
        //       // await using var context = await _contextFactory.CreateDbContextAsync();

        //        // var ttt = _context.Database.GetDbConnection().ToString();
        //        _logger.LogInformation("Updating login credential for LoginId: {LoginId}", loginRequest.LoginId);
        //        // _logger.LogInformation("DBContext", _context.Database.GetDbConnection().ToString());

        //        string sqlQuery = @"EXEC Skolenza.UpdateLoginCredential 
        //                    @LoginId, @Latitude, @Longitude, @LoginDevice, 
        //                    @IpAddressLocal, @IpAddressPublic, @MacAddress, 
        //                    @Status OUTPUT, @ErrorMessage OUTPUT";

        //        var statusParam = new NpgsqlParameter("@Status", NpgsqlDbType.Integer) { Direction = ParameterDirection.Output };
        //        var errorMsgParam = new NpgsqlParameter("@ErrorMessage", NpgsqlDbType.Text, 4000) { Direction = ParameterDirection.Output };

        //        await _context.Database.ExecuteSqlRawAsync(sqlQuery,
        //            new NpgsqlParameter("@LoginId", loginRequest.LoginId ?? (object)DBNull.Value),
        //            new NpgsqlParameter("@Latitude", loginRequest.Latitude),
        //            new NpgsqlParameter("@Longitude", loginRequest.Longitude),
        //            new NpgsqlParameter("@LoginDevice", loginRequest.LoginDevice),
        //            new NpgsqlParameter("@IpAddressLocal", loginRequest.IpAddressLocal ?? (object)DBNull.Value),
        //            new NpgsqlParameter("@IpAddressPublic", loginRequest.IpAddressPublic ?? (object)DBNull.Value),
        //            new NpgsqlParameter("@MacAddress", loginRequest.MacAddress ?? (object)DBNull.Value),
        //            statusParam,
        //            errorMsgParam
        //        );

        //        if (errorMsgParam.Value != DBNull.Value && !string.IsNullOrEmpty(errorMsgParam.Value.ToString()))
        //        {
        //            _logger.LogError("Update failed for LoginId: {LoginId}, Error: {ErrorMessage}", loginRequest.LoginId, errorMsgParam.Value);
        //            return false;
        //        }

        //        return true;
        //            //(statusParam.Value != DBNull.Value && (int)statusParam.Value == 1) ? "Success" : "No record updated";
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL Exception occurred while updating login credential for LoginId: {LoginId}", loginRequest.LoginId);
        //      //  return "Database error occurred while updating login credential.";
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while updating login credential for LoginId: {LoginId}", loginRequest.LoginId);
        //        return   false; // "An unexpected error occurred.";
        //    }
        //}
        public async Task<long> ValidateActiveUserLoginOnlyAsync(string loginId)
{
    try
    {
                //loginId = "admin@abc.com";
        _logger.LogInformation("Validating login for {LoginId}", loginId);

        var loginParam = new NpgsqlParameter("p_loginid", loginId);

        return await _context.Database
            .SqlQueryRaw<long>(
                "SELECT skolenza.\"ValidateActiveUserCredentialOnly\"(@p_loginid) AS \"Value\"",
                loginParam)
            .FirstOrDefaultAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating login for LoginId {LoginId}", loginId);
        return -1;
    }
}
        //public async Task<List<RoleModuleOperationResponseDTO>> GetActiveRoleModuleOperationsAsync(GetActiveRoleModuleOperationsRequestDTO request)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Fetching module operations for InstitutionId {InstitutionId} Roles {RoleIds}",
        //            request.InstitutionId, request.RoleIds);

        //        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();

        //        if (conn.State != ConnectionState.Open)
        //            await conn.OpenAsync();

        //        await using var cmd = new NpgsqlCommand(
        //            @"SELECT * 
        //      FROM skolenza.""GetActiveRoleModuleOperations""(@p_institutionid, @p_roleids);",
        //            conn);

        //        cmd.Parameters.AddWithValue("p_institutionid", request.InstitutionId);
        //        cmd.Parameters.AddWithValue("p_roleids", request.RoleIds);

        //        var result = new List<RoleModuleOperationResponseDTO>();

        //        await using var reader = await cmd.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            result.Add(new RoleModuleOperationResponseDTO
        //            {
        //                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        //                InstitutionId = reader.GetInt64(reader.GetOrdinal("InstitutionId")),
        //                ModuleId = reader.GetInt32(reader.GetOrdinal("ModuleId")),
        //                ModuleName = reader["ModuleName"]?.ToString(),
        //                URLPath = reader["URLPath"]?.ToString(),
        //                DisplayName = reader["DisplayName"]?.ToString(),
        //                ImageIconWeb = reader["ImageIconWeb"]?.ToString(),
        //                ImageIconMobile = reader["ImageIconMobile"]?.ToString(),
        //                ParentModuleId = reader.GetInt32(reader.GetOrdinal("ParentModuleId")),
        //                SubModuleName = reader["SubModuleName"]?.ToString(),
        //                MainModuleId = reader.GetInt32(reader.GetOrdinal("MainModuleId")),
        //                MainModuleName = reader["MainModuleName"]?.ToString(),
        //                OperationId = reader.GetInt32(reader.GetOrdinal("OperationId")),
        //                OperationName = reader["OperationName"]?.ToString(),
        //                DataViewStructureId = reader["DataViewStructureId"] as int?,
        //                DisplayOn = reader["DisplayOn"]?.ToString()
        //            });
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching module operations");
        //        throw;
        //    }
        //}
        /// <summary>
        /// Checks the authenticated Institution staffMember's current module-operation
        /// permission using current Primary and Secondary role assignments.
        /// </summary>
        /// <param name="institutionId">Authenticated Institution identifier.</param>
        /// <param name="staffMemberId">Authenticated StaffMember identifier.</param>
        /// <param name="tokenRoleId">Primary Role identifier carried by the JWT.</param>
        /// <param name="moduleId">Requested Module identifier.</param>
        /// <param name="operationId">Requested Operation identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the database operation.</param>
        /// <returns>The current Institution permission result.</returns>
        public async Task<InstitutionsUserPermissionCheckResponseDTO>
            CheckInstitutionStaffMemberPermissionAsync(
                long institutionId,
                long staffMemberId,
                int tokenRoleId,
                int moduleId,
                int operationId,
                CancellationToken cancellationToken = default)
        {
            // Request action identifiers are not authentication credentials. Avoid the
            // stored function's INVALID_ROLE_CONTEXT result (401) for malformed input.
            if (moduleId <= 0 || operationId <= 0)
            {
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    "ModuleId and OperationId must be positive.");
            }
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "Checking Institution permission for InstitutionId {InstitutionId}, StaffMemberId {StaffMemberId}, ModuleId {ModuleId}, OperationId {OperationId}",
                    institutionId,
                    staffMemberId,
                    moduleId,
                    operationId);

                var parameters = new[]
                {
            new NpgsqlParameter("p_institutionid", institutionId),
            new NpgsqlParameter("p_staffMemberid", staffMemberId),
            new NpgsqlParameter("p_tokenroleid", tokenRoleId),
            new NpgsqlParameter("p_moduleid", moduleId),
            new NpgsqlParameter("p_operationid", operationId)
        };

                var result = await _context
                    .Set<InstitutionsUserPermissionCheckResponseDTO>()
                    .FromSqlRaw(
                        """
                SELECT *
                FROM skolenza."CheckInstitutionStaffMemberPermission"(
                    @p_institutionid,
                    @p_staffMemberid,
                    @p_tokenroleid,
                    @p_moduleid,
                    @p_operationid
                )
                """,
                        parameters)
                    .AsNoTracking()
                    .SingleAsync(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking Institution permission for InstitutionId {InstitutionId}, StaffMemberId {StaffMemberId}, ModuleId {ModuleId}, OperationId {OperationId}",
                    institutionId,
                    staffMemberId,
                    moduleId,
                    operationId);

                throw;
            }
        }

        /// <summary>
        /// Checks the authenticated Host user's current module-operation permission using the authoritative Host role model.
        /// </summary>
        /// <param name="hostUserId">The authenticated Host user identifier.</param>
        /// <param name="tokenHostRoleId">The Host role identifier carried by the access token.</param>
        /// <param name="moduleId">The requested Host module identifier.</param>
        /// <param name="operationId">The requested Host operation identifier.</param>
        /// <param name="cancellationToken">The token used to cancel the database operation.</param>
        /// <returns>The current Host permission result.</returns>
        public async Task<HostUserPermissionCheckResponseDTO> CheckHostUserPermissionAsync(
            long hostUserId,
            long tokenHostRoleId,
            int moduleId,
            int operationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new[]
                {
                    new NpgsqlParameter("p_hostuserid", hostUserId),
                    new NpgsqlParameter("p_tokenhostroleid", tokenHostRoleId),
                    new NpgsqlParameter("p_moduleid", moduleId),
                    new NpgsqlParameter("p_operationid", operationId)
                };

                return await _context
                    .Set<HostUserPermissionCheckResponseDTO>()
                    .FromSqlRaw(
                        """
                        SELECT *
                        FROM skolenza."CheckHostUserPermission"(
                            @p_hostuserid,
                            @p_tokenhostroleid,
                            @p_moduleid,
                            @p_operationid
                        )
                        """,
                        parameters)
                    .AsNoTracking()
                    .SingleAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking Host permission for ModuleId {ModuleId}, OperationId {OperationId}",
                    moduleId,
                    operationId);
                throw;
            }
        }

        #region Institution Operational Access Queries

        /// <summary>
        /// Retrieves the legacy Institution operational permission rows for the supplied internal role-ID set.
        /// </summary>
        /// <param name="request">The trusted Institution identifier and internal comma-separated role identifiers.</param>
        /// <returns>The legacy operational permission rows.</returns>
        public async Task<List<RoleModuleOperationResponseDTO>> GetActiveRoleModuleOperationsAsync(
            GetActiveRoleModuleOperationsRequestDTO request)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching module operations for InstitutionId {InstitutionId} Roles {RoleIds}",
                    request.InstitutionId,
                    request.RoleIds);

                var parameters = new[]
                {
            new NpgsqlParameter("p_institutionid", request.InstitutionId),
            new NpgsqlParameter("p_roleids", request.RoleIds)
        };

                var result = await _context
                    .Set<RoleModuleOperationResponseDTO>()
                    .FromSqlRaw(
                        @"SELECT * 
                  FROM skolenza.""GetActiveRoleModuleOperations""(@p_institutionid, @p_roleids)",
                        parameters)
                    .AsNoTracking()
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching module operations");
                throw;
            }
        }
          /// <summary>
        /// Retrieves the authenticated institution staffMember's current effective module-operation access
        /// using the authoritative institution permission source.
        /// </summary>
        /// <param name="institutionId">The validated Institution identifier.</param>
        /// <param name="roleIds">The current effective role identifiers resolved by the application.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The current permitted operational rows used to construct navigation.</returns>
        public async Task<List<RoleModuleOperationResponseDTO>> GetCurrentInstitutionOperationalAccessAsync(
            long institutionId,
            IReadOnlyCollection<int> roleIds,
            CancellationToken cancellationToken = default)
        {
            if (institutionId <= 0 || roleIds.Count == 0)
            {
                return new List<RoleModuleOperationResponseDTO>();
            }

            var normalizedRoleIds = roleIds
                .Where(roleId => roleId > 0)
                .Distinct()
                .OrderBy(roleId => roleId)
                .ToArray();
            if (normalizedRoleIds.Length == 0)
            {
                return new List<RoleModuleOperationResponseDTO>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Query the authoritative Institution permission source using the current effective role set.
            // Do not apply a second module-enable model: InstitutionEnabledModule stores enabled hierarchy roots,
            // while this function resolves the granted leaf module-operation permission rows.
            return await GetActiveRoleModuleOperationsAsync(
                new GetActiveRoleModuleOperationsRequestDTO
                {
                    InstitutionId = institutionId,
                    RoleIds = string.Join(',', normalizedRoleIds)
                });
        }

        #endregion

        public async Task<StaffMemberCountResponseStatsSp?> GetStaffMemberCountsAsync(long institutionId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching staffMember count statistics. InstitutionId: {InstitutionId}",
                    institutionId);

                var institutionParam = new NpgsqlParameter("p_institutionid", institutionId);

                var result = await _context.Set<StaffMemberCountResponseStatsSp>()
                    .FromSqlRaw(
                        @"SELECT * 
                  FROM skolenza.""AllStaffMemberCountData""(@p_institutionid)",
                        institutionParam)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember count statistics. InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }

  
        public async Task<List<GetStaffMemberIdentitySp>> GetIdentityRecordAsync(
    long staffMemberId,
    int countryId,
    bool isActive)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching StaffMember Identity records. StaffMemberId: {StaffMemberId}, CountryId: {CountryId}",
                    staffMemberId, countryId);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var result = await (
                    from rule in _context.CountryIdentityRules
                    join identityRecord in _context.StaffMemberIdentities
                            .Where(item =>
                                item.StaffMemberId == staffMemberId &&
                                item.IsActive == isActive &&
                                !item.IsSoftDeleted &&
                                (!item.EffectiveTo.HasValue || item.EffectiveTo >= today))
                        on rule.IdentityCategoryDocumentId equals identityRecord.IdentityCategoryDocumentId
                        into identityRecords
                    from identityRecord in identityRecords.DefaultIfEmpty()
                    where rule.CountryId == countryId &&
                          rule.IsActive == true &&
                          rule.Country.IsActive == true &&
                          rule.IdentityCategoryDocument.IsActive &&
                          rule.IdentityCategoryDocument.IdentityCategory.IsActive
                    orderby rule.IdentityCategoryDocument.IdentityCategory.Name,
                        rule.IdentityCategoryDocument.DocumentName
                    select new GetStaffMemberIdentitySp
                    {
                        Id = identityRecord == null ? null : identityRecord.Id,
                        StaffMemberId = identityRecord == null ? null : identityRecord.StaffMemberId,
                        CountryCode = rule.Country.CountryCode,
                        CountryName = rule.Country.CountryName,
                        IdentityCategoryName = rule.IdentityCategoryDocument.IdentityCategory.Name,
                        IdentityCategoryDocumentId = rule.IdentityCategoryDocumentId,
                        DocumentCode = rule.IdentityCategoryDocument.Code,
                        DocumentName = rule.IdentityCategoryDocument.DocumentName,
                        Description = rule.IdentityCategoryDocument.Description,
                        IsMandatory = rule.IsMandatory,
                        StaffMemberIdentityId = identityRecord == null ? null : identityRecord.Id,
                        IdentityValue = identityRecord == null ? null : identityRecord.IdentityValue,
                        IsVerified = identityRecord == null ? null : identityRecord.IsInfoVerified,
                        IsEditAllowed = identityRecord == null ? null : identityRecord.IsEditAllowed,
                        HasIdentityUploaded = identityRecord == null ? null : identityRecord.HasIdentityUploaded,
                        EffectiveFrom = identityRecord == null ? null : identityRecord.EffectiveFrom,
                        EffectiveTo = identityRecord == null ? null : identityRecord.EffectiveTo,
                        IsActive = identityRecord == null ? null : identityRecord.IsActive
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while fetching identity records. StaffMemberId: {StaffMemberId}",
                    staffMemberId);
                throw;
            }
        }
         

        public async Task<long> ValidateActiveUserCredentialOnlyAsync(string loginId)
        {
            try
            {
                _logger.LogInformation(
                    "Validating user login for LoginId: {LoginId}", loginId);

                var loginParam = new NpgsqlParameter("p_loginid", loginId ?? (object)DBNull.Value);

                var result = await _context.Database
                    .SqlQueryRaw<long>(
                        @"SELECT skolenza.""ValidateActiveUserCredentialOnly""(@p_loginid) AS ""Value""",
                        loginParam)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error validating user login for LoginId: {LoginId}",
                    loginId);

                return -1;
            }
        }

        //public async Task<bool> GetHasAccessOperation(GetCheckOperationPermissionRequestDTO checkOperationPermissionRequest)
        //{
        //    try
        //    {
        //     //   await using var context = await _contextFactory.CreateDbContextAsync();

        //        // SQL query calling the stored procedure and returning the result
        //        string sqlQuery = @"EXEC Skolenza.CheckPermission 
        //                    @RoleId, @ProjectChildModuleDetailId, @OperationId, @HasAccess, @IsActive";

        //        // Parameters definitione
        //        var parameters = new[]
        //        {
        //           new NpgsqlParameter("@RoleId", checkOperationPermissionRequest.RoleIds),           
        //           new NpgsqlParameter("@OperationId", checkOperationPermissionRequest.OperationId),
        //           new NpgsqlParameter("@HasAccess", checkOperationPermissionRequest.HasAccess),
        //           new NpgsqlParameter("@IsActive", checkOperationPermissionRequest.IsActive)
        //};

        //        // Executing the stored procedure and getting the result (BIT value)
        //        var result = await _context.Database.SqlQueryRaw<bool>(sqlQuery, parameters).ToListAsync();

        //        // If result is 1 (HasAccess), return true, otherwise false
        //        if (result != null && result.Count > 0)
        //        {
        //            return result[0];
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception or handle accordingly
        //        _logger.LogError($"Error in GetHasAccessOperation: {ex.Message}");
        //        return false;
        //    }
        //}
        public async Task<bool> GetHasAccessOperation(GetCheckOperationPermissionRequestDTO request)
        {
            try
            {
                var parameters = new[]
                {
            new NpgsqlParameter("p_roleid", request.RoleIds),
            new NpgsqlParameter("p_projectchildmoduledetailid", request.OperationId),
            new NpgsqlParameter("p_operationid", request.OperationId),
            new NpgsqlParameter("p_hasaccess", request.HasAccess),
            new NpgsqlParameter("p_isactive", request.IsActive)
        };

                var result = await _context.Database
                    .SqlQueryRaw<bool>(
                        @"SELECT skolenza.""CheckPermission""(
                    @p_roleid,
                    @p_projectchildmoduledetailid,
                    @p_operationid,
                    @p_hasaccess,
                    @p_isactive
                ) AS ""Value""",
                        parameters)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHasAccessOperation");
                return false;
            }
        }
        public Task<bool> HasPermissionAsync(long userId, string permissionCode)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsInstitutionValidAsync(long userId, long? institutionId)
        {
            throw new NotImplementedException();
        }

        public Task<int> ValidateUserPasswordAsync(string loginId)
        {
            throw new NotImplementedException();
        }
        //public async Task<UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO> UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO(UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO request)
        //{
        //    try
        //    {
        //      //  await using var context = await _contextFactory.CreateDbContextAsync();

        //        _logger.LogInformation("Get new Module Operations from ModuleOperation: {InstitutionId}", request.InstitutionId);

        //        var institutionIdParam = new NpgsqlParameter("@InstitutionId", request.InstitutionId);

        //        // SQL query (no need for output param, as your procedure returns scalar)
        //        string sqlQuery = "DECLARE @Result INT; EXEC @Result = Skolenza.UpdateInstitutionEnabledOperationFromModuleOperation @InstitutionId; SELECT @Result";

        //        var result = await _context.Database.ExecuteSqlRawAsync(sqlQuery, institutionIdParam);

        //        return new UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO
        //        {
        //            Result = result
        //        };
        //    }
        //    catch (NpgsqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL Exception occurred while updating institution operation for InstitutionId: {InstitutionId}", request.InstitutionId);

        //        return new UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO
        //        {
        //            Result = -1
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while updating institution operation for InstitutionId: {InstitutionId}", request.InstitutionId);

        //        return new UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO
        //        {
        //            Result = -1
        //        };
        //    }
        //}

        public async Task<UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO>
   UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO(
       UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO request)
        {
            try
            {
                _logger.LogInformation(
                    "Updating institution enabled operations from module operations. InstitutionId: {InstitutionId}",
                    request.InstitutionId);

                var institutionParam = new NpgsqlParameter("p_institutionid", request.InstitutionId);

                var result = await _context.Database
                    .SqlQueryRaw<int>(
                        @"SELECT skolenza.""UpdateInstitutionEnabledOperationFromModuleOperation""(@p_institutionid) AS ""Value""",
                        institutionParam)
                    .SingleAsync();

                return new UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO
                {
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while updating institution operations for InstitutionId: {InstitutionId}",
                    request.InstitutionId);

                return new UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO
                {
                    Result = -1
                };
            }
        }


      

        //public async Task<List<GetModuleOperationRolePermissionsResponseDTO>> GetInstitutionModulesConfigurationResponses(GetInstitutionModuleOperationRolePermissionsRequestDTO request)
        //{
        //    try
        //    {
        //     //   await using var context = await _contextFactory.CreateDbContextAsync();

        //        var institutionIdParam = new NpgsqlParameter("@InstitutionId", request.InstitutionId);

        //        var flatList = await _context.InstitutionModulesConfigurations
        //            .FromSqlRaw("EXEC Skolenza.GetInstitutionModuleOperationalConfigData @InstitutionId", institutionIdParam)
        //            .ToListAsync();

        //        var moduleDict = flatList
        //            .GroupBy(f => f.ModuleId)
        //            .ToDictionary(
        //                g => g.Key,
        //                g =>
        //                {
        //                    var first = g.First();
        //                    return new GetModuleOperationRolePermissionsResponseDTO
        //                    {
        //                        ModuleId = first.ModuleId,
        //                        ModuleName = first.ModuleName,
        //                        IsLeafNode = first.IsLeafNode,
        //                        IsEnabled = first.IsEnabled,
        //                        Level = first.Level,
        //                        BreadCrum = first.BreadCrum,
        //                        Operations = g
        //                            .Where(x => x.OperationId.HasValue)
        //                            .Select(x => new OperationDTO_Config                                    {
        //                                OperationId = x.OperationId.Value,
        //                                OperationName = x.OperationName,
        //                                IsOperationUsed = x.IsOperationUsed ?? false
        //                            }).ToList(),
        //                        Children = new List<GetModuleOperationRolePermissionsResponseDTO>()
        //                    };
        //                });

        //        List<GetModuleOperationRolePermissionsResponseDTO> roots = new();
        //        foreach (var flat in flatList)
        //        {
        //            if (flat.ParentModuleId.HasValue && moduleDict.ContainsKey(flat.ParentModuleId.Value))
        //            {
        //                var parent = moduleDict[flat.ParentModuleId.Value];
        //                if (!parent.Children.Any(c => c.ModuleId == flat.ModuleId))
        //                {
        //                    parent.Children.Add(moduleDict[flat.ModuleId]);
        //                }
        //            }
        //            else if (!flat.ParentModuleId.HasValue)
        //            {
        //                if (!roots.Any(r => r.ModuleId == flat.ModuleId))
        //                {
        //                    roots.Add(moduleDict[flat.ModuleId]);
        //                }
        //            }
        //        }

        //        return roots;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while getting institution module configuration for InstitutionId: {InstitutionId}", request.InstitutionId);
        //        return new List<GetModuleOperationRolePermissionsResponseDTO>();
        //    }
        //}

        public async Task<List<GetModuleOperationRolePermissionsResponseDTO>>
GetInstitutionModulesConfigurationResponses(GetInstitutionModuleOperationRolePermissionsRequestDTO request)
        {
            try
            {
                var institutionParam = new NpgsqlParameter("p_institutionid", request.InstitutionId);

                var flatList = await _context.InstitutionModulesConfigurations
                    .FromSqlRaw(
                        @"SELECT * 
                  FROM skolenza.""GetInstitutionModuleOperationalConfigData""(@p_institutionid)",
                        institutionParam)
                    .AsNoTracking()
                    .ToListAsync();

                var moduleDict = flatList
                    .GroupBy(f => f.ModuleId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var first = g.First();

                            return new GetModuleOperationRolePermissionsResponseDTO
                            {
                                ModuleId = first.ModuleId,
                                ModuleName = first.ModuleName,
                                IsLeafNode = first.IsLeafNode,
                                IsEnabled = first.IsEnabled,
                                Level = first.Level,
                                BreadCrum = first.BreadCrum,

                                Operations = g
                                    .Where(x => x.OperationId.HasValue)
                                    .Select(x => new OperationDTO_Config
                                    {
                                        OperationId = x.OperationId.Value,
                                        OperationName = x.OperationName,
                                        IsOperationUsed = x.IsOperationUsed ?? false
                                    })
                                    .ToList(),

                                Children = new List<GetModuleOperationRolePermissionsResponseDTO>()
                            };
                        });

                var roots = new List<GetModuleOperationRolePermissionsResponseDTO>();

                foreach (var flat in flatList)
                {
                    if (flat.ParentModuleId.HasValue &&
                        moduleDict.TryGetValue(flat.ParentModuleId.Value, out var parent))
                    {
                        parent.Children.Add(moduleDict[flat.ModuleId]);
                    }
                    else if (!flat.ParentModuleId.HasValue)
                    {
                        roots.Add(moduleDict[flat.ModuleId]);
                    }
                }

                return roots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while getting institution module configuration for InstitutionId: {InstitutionId}",
                    request.InstitutionId);

                return new List<GetModuleOperationRolePermissionsResponseDTO>();
            }
        }



        //public async Task<int> ValidateUserLoginAsync(string loginId)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Validating user login for LoginId: {LoginId}", loginId);
        //        var param = new[] { new NpgsqlParameter("@LoginId", loginId ?? (object)DBNull.Value),
        //              new NpgsqlParameter("@result",SqlDbType.Int) { Direction=ParameterDirection.Output} };


        //        string sqlQuery  = @"DECLARE @Result INT;
        //                    EXEC Skolenza.ValidateUserLogin 
        //                        @LoginId = @loginId, 
        //                        @StaffMemberId = @StaffMemberId OUTPUT;
        //                    SELECT @StaffMemberId;";

        //        var result = await _context.Database.ExecuteSqlRawAsync(sqlQuery, param);

        //        return result;

        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError(ex, "SQL Exception occurred while validating user login for LoginId: {LoginId}", loginId);
        //        throw new Exception("Database error occurred while validating user login.");
        //    }

        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while validating user login for LoginId: {LoginId}");
        //        throw;
        //    }
        //}


    }
}
