using System.Text.Json;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;

namespace skolenza.application.Common.Helpers;

/// <summary>Produces institution-scoped master previews using existing repositories. Never persists data.</summary>
public sealed class BulkImportPreviewService(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
{
    #region Trusted reads

    public async Task<BulkImportPreviewResponseDTO> PreviewAsync(
        BulkImportMaster master,
        BulkImportPreviewRequestDTO request,
        CancellationToken cancellationToken)
    {
        // The module-specific MediatR behavior runs before this service.
        var context = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!context.Success || context.InstitutionId <= 0 ||
            context.LoggedInStaffMemberId <= 0 || context.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var table = await BulkImportTableReader.ReadAsync(request, cancellationToken);
        var departments = new List<GetDepartmentResponseDTO>();
        var roles = new List<GetRoleResponseDTO>();
        var staffEmploymentTypes = new List<global::skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO>();

        // Existing list repositories already enforce institution ownership and exclude soft-deleted rows.
        if (master == BulkImportMaster.Department)
        {
            departments = (await unitOfWork.DepartmentRepository.GetAsync(
                new GetDepartmentRequestDTO { PageNumber = 1, PageSize = int.MaxValue },
                context.InstitutionId,
                cancellationToken)).Data;
        }



        if (master == BulkImportMaster.Role)
        {
            roles = (await unitOfWork.RoleRepository.GetAsync(
                context.InstitutionId,
                new GetRoleRequestDTO
                {
                    PageNumber = 1,
                    PageSize = int.MaxValue,
                    IsActive = false // Existing repository convention: include both states.
                })).Data;
        }

        if (master == BulkImportMaster.StaffEmploymentType)
        {
            staffEmploymentTypes = await unitOfWork.StaffEmploymentTypeRepository.GetAllAsync(context.InstitutionId, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Build(master, request.ColumnMappingJson, table, departments, roles, staffEmploymentTypes);
    }

    #endregion

    #region Mapping and row validation

    public static BulkImportPreviewResponseDTO Build(
        BulkImportMaster master,
        string? mappingJson,
        BulkImportTableDTO table,
        IReadOnlyList<GetDepartmentResponseDTO> departments,
        IReadOnlyList<GetRoleResponseDTO> roles,
        IReadOnlyList<global::skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO>? staffEmploymentTypes = null)
    {
        var nameField = master switch
        {
            BulkImportMaster.Department => BulkImportConstants.DepartmentName,
            BulkImportMaster.Role => BulkImportConstants.RoleName,
            BulkImportMaster.StaffEmploymentType => BulkImportConstants.TypeName,
            _ => throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest)
        };
        var allowed = new List<string> { nameField, BulkImportConstants.IsActive };
        allowed.Add(master == BulkImportMaster.Role ? BulkImportConstants.Remark : BulkImportConstants.Description);
        if (master is BulkImportMaster.Department or BulkImportMaster.StaffEmploymentType)
        {
            allowed.Add(BulkImportConstants.Remark);
        }

        if (master == BulkImportMaster.Role)
        {
            allowed.Add(BulkImportConstants.RoleType);
        }

        var result = new BulkImportPreviewResponseDTO
        {
            Master = master,
            SourceColumns = table.Columns.ToList()
        };
        var mapping = ResolveColumnMapping(table, mappingJson, allowed);
        result.ColumnMapping = mapping;
        var required = new List<string> { nameField };

        if (master == BulkImportMaster.Role)
        {
            required.Add(BulkImportConstants.RoleType);
        }
        foreach (var field in required.Where(field => !mapping.ContainsKey(field)))
        {
            result.Errors.Add($"Map the required column {field}.");
        }

        foreach (var source in table.Rows)
        {
            var row = new BulkImportPreviewRowDTO
            {
                RowNumber = source.RowNumber,
                Values = mapping.ToDictionary(pair => pair.Key,
                    pair => source.Values[table.Columns.IndexOf(pair.Value)].Trim()),
                Status = BulkImportRowStatus.Ready
            };
            var name = row.Values.GetValueOrDefault(nameField, string.Empty);
            foreach (var field in required)
            {
                if (string.IsNullOrWhiteSpace(row.Values.GetValueOrDefault(field)))
                {
                    row.Errors.Add($"{field} is required.");
                }
            }
            if (name.Length > (master == BulkImportMaster.Role ? 100 : 255))
            {
                row.Errors.Add($"{nameField} exceeds the database field length.");
            }
            if (row.Values.GetValueOrDefault(BulkImportConstants.Description, string.Empty).Length > (master == BulkImportMaster.StaffEmploymentType ? 255 : 500) ||
                row.Values.GetValueOrDefault(BulkImportConstants.Remark, string.Empty).Length > (master == BulkImportMaster.StaffEmploymentType ? 255 : 200))
            {
                row.Errors.Add("Description or Remark exceeds the database field length.");
            }
            if (row.Values.TryGetValue(BulkImportConstants.IsActive, out var active) &&
                !string.IsNullOrWhiteSpace(active) && !bool.TryParse(active, out _))
            {
                row.Errors.Add("IsActive must be true or false.");
            }

            if (master == BulkImportMaster.Department)
            {
                var matches = departments.Where(item => Same(item.DepartmentName, name)).ToList();
                Match(row, matches.Select(item => (item.Id, item.IsActive)).ToList());
            }

            else if (master == BulkImportMaster.StaffEmploymentType)
            {
                var matches = (staffEmploymentTypes ?? Array.Empty<global::skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO>())
                    .Where(item => Same(item.TypeName, name)).ToList();
                Match(row, matches.Select(item => (item.Id, item.IsActive == true)).ToList());
            }
            else
            {
                var rawType = row.Values.GetValueOrDefault(BulkImportConstants.RoleType);
                if (!int.TryParse(rawType, out var roleType) ||
                    !ConstantValues.IsSupportedInstitutionRoleType(roleType))
                {
                    row.Errors.Add("RoleType must match an ID from the Role Type catalogue.");
                }
                var matches = roles.Where(item => Same(item.RoleName, name)).ToList();
                Match(row, matches.Select(item => (item.Id, item.IsActive)).ToList());
                if (matches.Count == 1 && matches[0].RoleType != roleType)
                {
                    row.Errors.Add("Existing role has a different RoleType; import must not overwrite it.");
                }
            }

            result.Rows.Add(row);
        }

        var duplicates = result.Rows.GroupBy(row => (
                Name: row.Values.GetValueOrDefault(nameField, string.Empty).ToUpperInvariant(),
                Department: string.Empty))
            .Where(group => group.Count() > 1);
        foreach (var group in duplicates)
        {
            foreach (var row in group)
            {
                row.Errors.Add("Duplicate source rows: " + string.Join(", ", group.Select(item => item.RowNumber)));
            }
        }
        foreach (var row in result.Rows.Where(row => row.Errors.Count > 0))
        {
            row.Status = BulkImportRowStatus.Invalid;
        }

        return result;
    }

    private static void Match(BulkImportPreviewRowDTO row, List<(int Id, bool Active)> matches)
    {
        if (matches.Count > 1)
        {
            row.Errors.Add("Multiple existing matches require manual review.");
        }
        else if (matches.Count == 1)
        {
            row.ExistingId = matches[0].Id;
            row.Status = BulkImportRowStatus.Existing;
            if (!matches[0].Active && IsRequestedActive(row))
            {
                row.WillReactivate = true;
            }
        }
    }

    private static bool IsRequestedActive(BulkImportPreviewRowDTO row)
    {
        return !row.Values.TryGetValue(BulkImportConstants.IsActive, out var raw) ||
            string.IsNullOrWhiteSpace(raw) || bool.TryParse(raw, out var active) && active;
    }

    /// <summary>Resolves explicit or deterministic canonical headers for every import module.</summary>
    public static Dictionary<string, string> ResolveColumnMapping(
        BulkImportTableDTO table, string? mappingJson, IReadOnlyCollection<string> allowed)
    {
        Dictionary<string, string> mapping;
        try
        {
            mapping = string.IsNullOrWhiteSpace(mappingJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson) ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new ValidationErrorException("ColumnMappingJson must be an object of target field to source header.");
        }
        foreach (var pair in mapping)
        {
            if (!allowed.Contains(pair.Key) || !table.Columns.Contains(pair.Value))
            {
                throw new ValidationErrorException("Column mapping contains an unknown target field or source header.");
            }
        }
        foreach (var field in allowed.Where(field => !mapping.ContainsKey(field)))
        {
            var matches = table.Columns.Where(column => HeaderKey(column) == HeaderKey(field)).ToList();
            if (matches.Count == 1)
            {
                mapping[field] = matches[0];
            }
        }
        if (mapping.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mapping.Count)
        {
            throw new ValidationErrorException("One source column cannot map to multiple target fields.");
        }
        return mapping;
    }

    private static string HeaderKey(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    }

    private static bool Same(string? first, string? second)
    {
        return string.Equals(first?.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
