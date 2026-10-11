using System.Text.Json;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace skolenza.persistance.Repositories;

public sealed partial class BulkImportRepository
{
    #region Host preview

    public async Task<BulkImportPreviewResponseDTO> PreviewHostAsync(
        BulkImportMaster master, BulkImportTableDTO table, string? mappingJson,
        long institutionId, HostUserRequestContext host, CancellationToken ct)
    {
        if (master is not (BulkImportMaster.DeviceMaster or BulkImportMaster.InstitutionCard or
            BulkImportMaster.HostModule or BulkImportMaster.HostSubModule or
            BulkImportMaster.HostOperation or BulkImportMaster.HostModuleOperation))
        {
            throw new ValidationErrorException("Unsupported Host import target.");
        }
        if (master == BulkImportMaster.InstitutionCard)
        {
            await EnsureCardInstitutionAsync(institutionId, ct);
        }
        var columns = master switch
        {
            BulkImportMaster.DeviceMaster => HostBulkImportTableMapper.DeviceColumns,
            BulkImportMaster.InstitutionCard => HostBulkImportTableMapper.CardColumns,
            BulkImportMaster.HostModule => HostBulkImportTableMapper.ModuleColumns,
            BulkImportMaster.HostSubModule => HostBulkImportTableMapper.SubModuleColumns,
            BulkImportMaster.HostOperation => HostBulkImportTableMapper.OperationColumns,
            _ => HostBulkImportTableMapper.ModuleOperationColumns
        };
        var mapping = HostBulkImportTableMapper.ResolveColumns(table.Columns, columns, mappingJson);
        var result = new BulkImportPreviewResponseDTO
        {
            Master = master, SourceColumns = table.Columns, ColumnMapping = mapping
        };
        var required = master switch
        {
            BulkImportMaster.DeviceMaster => new[] { "SNo", "DeviceCode", "DeviceName", "CompanyName", "ModelNo", "DeviceType" },
            BulkImportMaster.InstitutionCard => new[] { "CardNumber" },
            BulkImportMaster.HostModule => new[] { "ModuleCode", "ModuleName", "PageName", "ModuleScope" },
            BulkImportMaster.HostSubModule => new[] { "ParentModuleCode", "ModuleCode", "ModuleName", "PageName", "ModuleScope" },
            BulkImportMaster.HostOperation => new[] { "OperationName", "OperationType" },
            _ => new[] { "ModuleCode", "OperationType" }
        };
        foreach (var name in required.Where(name => !mapping.ContainsKey(name)))
        {
            result.Errors.Add($"Map the required {name} column.");
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in table.Rows)
        {
            var row = new BulkImportPreviewRowDTO { RowNumber = source.RowNumber, Status = BulkImportRowStatus.Ready };
            result.Rows.Add(row);
            try
            {
                foreach (var pair in mapping)
                {
                    var index = table.Columns.FindIndex(column => string.Equals(column, pair.Value, StringComparison.OrdinalIgnoreCase));
                    row.Values[pair.Key] = index < source.Values.Count ? source.Values[index].Trim() : string.Empty;
                }
                if (master == BulkImportMaster.DeviceMaster)
                {
                    var dto = HostBulkImportTableMapper.ReadRow<CreateDeviceMasterRequestDTO>(table, source, mapping);
                    CreateDeviceMasterCommandHandler.Validate(dto);
                    ValidateLengths<DeviceMaster>(dto);
                    var code = "code:" + dto.DeviceCode;
                    var model = "model:" + JsonSerializer.Serialize(new[] { dto.CompanyName.ToUpperInvariant(), dto.ModelNo.ToUpperInvariant() });
                    if (!seen.Add(code) || !seen.Add(model))
                    {
                        throw new ValidationErrorException("Duplicate device code or company/model in this upload.");
                    }
                    row.HostRecordId = await ExistingDeviceAsync(dto, ct);
                }
                else if (master == BulkImportMaster.InstitutionCard)
                {
                    var dto = HostBulkImportTableMapper.ReadRow<CreateInstitutionCardMasterRequestDTO>(table, source, mapping);
                    InstitutionCardMasterHandlerBase.ValidateImport(dto, true);
                    ValidateLengths<InstitutionCardMaster>(dto);
                    var number = dto.CardNumber;
                    if (!seen.Add(number))
                    {
                        throw new ValidationErrorException("Duplicate card in this upload.");
                    }
                    row.CardLookupHash = InstitutionCardMasterHandlerBase.ImportHash(number, host.InstitutionEncryptionKey);
                    row.CardCiphertext = (encryption ?? throw new InvalidOperationException("Card encryption service is unavailable."))
                        .Encrypt(number, host.InstitutionEncryptionKey);
                    row.HostRecordId = await ExistingCardAsync(institutionId, row.CardLookupHash, ct);
                }
                else
                {
                    row.HostRecordId = await PreviewHostCatalogueRowAsync(master, table, source, mapping, seen, ct);
                }
                if (row.HostRecordId.HasValue)
                {
                    row.Status = BulkImportRowStatus.Existing;
                }
            }
            catch (ValidationErrorException error)
            {
                row.Status = BulkImportRowStatus.Invalid;
                row.Errors.Add(error.Message);
            }
            finally
            {
                // Even invalid card rows must never place raw card numbers in durable JSON or responses.
                if (master == BulkImportMaster.InstitutionCard && row.Values.TryGetValue("CardNumber", out var raw))
                {
                    row.Values["CardNumber"] = raw.Length <= 4 ? "****" : "****" + raw[^4..];
                }
            }
        }
        return result;
    }

    private async Task<long?> PreviewHostCatalogueRowAsync(BulkImportMaster master, BulkImportTableDTO table,
        BulkImportSourceRowDTO source, IReadOnlyDictionary<string, string> mapping, HashSet<string> seen, CancellationToken ct)
    {
        if (master is BulkImportMaster.HostModule or BulkImportMaster.HostSubModule)
        {
            var dto = master == BulkImportMaster.HostModule
                ? HostBulkImportTableMapper.ReadRow<HostModuleImportRowDTO>(table, source, mapping)
                : HostBulkImportTableMapper.ReadRow<HostSubModuleImportRowDTO>(table, source, mapping);
            if (string.IsNullOrWhiteSpace(dto.ModuleCode) || string.IsNullOrWhiteSpace(dto.ModuleName) ||
                string.IsNullOrWhiteSpace(dto.PageName) || dto.ModuleScope is not (1 or 2))
            {
                throw new ValidationErrorException("ModuleCode, ModuleName, PageName and a supported ModuleScope are required.");
            }
            if (!seen.Add("module:" + dto.ModuleCode.Trim()))
            {
                throw new ValidationErrorException("Duplicate ModuleCode in this upload.");
            }
            if (master == BulkImportMaster.HostSubModule)
            {
                var child = (HostSubModuleImportRowDTO)dto;
                if (string.IsNullOrWhiteSpace(child.ParentModuleCode) || !await context.Modules.AsNoTracking()
                    .AnyAsync(x => x.ModuleCode == child.ParentModuleCode.Trim() && x.IsActive &&
                        x.ModuleScope == child.ModuleScope, ct))
                {
                    throw new ValidationErrorException("Active parent ModuleCode in the same scope was not found.");
                }
            }
            return await context.Modules.AsNoTracking().Where(x => x.ModuleCode == dto.ModuleCode.Trim() &&
                x.ModuleScope == dto.ModuleScope).Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
        }
        if (master == BulkImportMaster.HostOperation)
        {
            var dto = HostBulkImportTableMapper.ReadRow<HostOperationImportRowDTO>(table, source, mapping);
            if (string.IsNullOrWhiteSpace(dto.OperationName) || dto.OperationType <= 0)
            {
                throw new ValidationErrorException("OperationName and a positive OperationType are required.");
            }
            if (!seen.Add("operation:" + dto.OperationType))
            {
                throw new ValidationErrorException("Duplicate OperationType in this upload.");
            }
            var existing = await context.Operations.AsNoTracking().FirstOrDefaultAsync(x => x.OperationType == dto.OperationType, ct);
            if (existing != null && !string.Equals(existing.OperationName, dto.OperationName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationErrorException("OperationType already exists with a different immutable OperationName.");
            }
            return existing?.Id;
        }
        var map = HostBulkImportTableMapper.ReadRow<HostModuleOperationImportRowDTO>(table, source, mapping);
        if (string.IsNullOrWhiteSpace(map.ModuleCode) || map.OperationType <= 0 ||
            !seen.Add($"mapping:{map.ModuleCode.Trim()}:{map.OperationType}"))
        {
            throw new ValidationErrorException("Unique ModuleCode and positive OperationType are required.");
        }
        var moduleId = await context.Modules.AsNoTracking().Where(x => x.ModuleCode == map.ModuleCode.Trim() && x.IsActive)
            .Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        var operationId = await context.Operations.AsNoTracking().Where(x => x.OperationType == map.OperationType && x.IsActive)
            .OrderBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        if (!moduleId.HasValue || !operationId.HasValue)
        {
            throw new ValidationErrorException("Active ModuleCode or OperationType dependency was not found.");
        }
        return await context.ModuleOperationMappings.AsNoTracking().Where(x => x.ModuleId == moduleId && x.OperationId == operationId)
            .Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
    }

    private void ValidateLengths<TEntity>(object dto)
    {
        var entity = context.Model.FindEntityType(typeof(TEntity))!;
        foreach (var property in dto.GetType().GetProperties())
        {
            var maximum = entity.FindProperty(property.Name)?.GetMaxLength();
            if (maximum.HasValue && property.GetValue(dto) is string text && text.Length > maximum.Value)
            {
                throw new ValidationErrorException($"{property.Name} exceeds {maximum.Value} characters.");
            }
        }
    }

    private Task<long?> ExistingDeviceAsync(CreateDeviceMasterRequestDTO dto, CancellationToken ct)
    {
        return context.DeviceMasters.AsNoTracking().Where(item => !item.IsSoftDeleted &&
            (item.DeviceCode.ToLower() == dto.DeviceCode.ToLower() ||
             (item.CompanyName.ToLower() == dto.CompanyName.ToLower() && item.ModelNo.ToLower() == dto.ModelNo.ToLower())))
            .OrderBy(item => item.Id).Select(item => (long?)item.Id).FirstOrDefaultAsync(ct);
    }

    private Task<long?> ExistingCardAsync(long institutionId, string hash, CancellationToken ct)
    {
        return context.InstitutionCardMasters.AsNoTracking().Where(item => item.InstitutionId == institutionId &&
            item.CardNumberLookupHash == hash && !item.IsSoftDeleted)
            .Select(item => (long?)item.Id).FirstOrDefaultAsync(ct);
    }

    private async Task EnsureCardInstitutionAsync(long institutionId, CancellationToken ct)
    {
        if (!await context.Set<Institution>().AsNoTracking().AnyAsync(item => item.Id == institutionId &&
            item.IsActive && item.IsSoftDeleted != true, ct))
        {
            throw new ValidationErrorException("Selected institution is inactive or unavailable.");
        }
    }

    #endregion

    #region Host durable worker

    private async Task ProcessHostBatchAsync(BulkImportJob job, BulkImportPreviewResponseDTO preview, CancellationToken ct)
    {
        var transaction = context.Database.CurrentTransaction!;
        var end = Math.Min(preview.Rows.Count, job.NextRow + Math.Clamp(options.Value.BatchSize, 1, 200));
        for (var index = job.NextRow; index < end; index++)
        {
            var row = preview.Rows[index];
            if (row.Status == BulkImportRowStatus.Created)
            {
                job.NextRow = index + 1;
                continue;
            }
            await transaction.CreateSavepointAsync("host_bulk_row", ct);
            try
            {
                var table = new BulkImportTableDTO { Columns = row.Values.Keys.ToList() };
                var source = new BulkImportSourceRowDTO { RowNumber = row.RowNumber, Values = row.Values.Values.ToList() };
                var mapping = table.Columns.Where(name => job.Master != (int)BulkImportMaster.InstitutionCard || name != "CardNumber")
                    .ToDictionary(name => name, name => name);
                if (job.Master == (int)BulkImportMaster.DeviceMaster)
                {
                    var dto = HostBulkImportTableMapper.ReadRow<CreateDeviceMasterRequestDTO>(table, source, mapping);
                    CreateDeviceMasterCommandHandler.Validate(dto);
                    ValidateLengths<DeviceMaster>(dto);
                    row.HostRecordId = await ExistingDeviceAsync(dto, ct);
                    if (!row.HostRecordId.HasValue)
                    {
                        var entity = mapper.Map<DeviceMaster>(dto);
                        entity.IsOccupied = false;
                        entity.IsSoftDeleted = false;
                        entity.AddedById = job.ActorId;
                        entity.AddedDateTime = DateTime.UtcNow;
                        context.DeviceMasters.Add(entity);
                        await context.SaveChangesAsync(ct);
                        row.HostRecordId = entity.Id;
                        row.Status = BulkImportRowStatus.Created;
                    }
                    else
                    {
                        row.Status = BulkImportRowStatus.Existing;
                    }
                }
                else if (job.Master == (int)BulkImportMaster.InstitutionCard)
                {
                    await EnsureCardInstitutionAsync(job.InstitutionId, ct);
                    var dto = HostBulkImportTableMapper.ReadRow<CreateInstitutionCardMasterRequestDTO>(table, source, mapping);
                    InstitutionCardMasterHandlerBase.ValidateImport(dto, false);
                    ValidateLengths<InstitutionCardMaster>(dto);
                    if (string.IsNullOrWhiteSpace(row.CardCiphertext) || string.IsNullOrWhiteSpace(row.CardLookupHash))
                    {
                        throw new ValidationErrorException("Protected card snapshot is missing. Create a new preview.");
                    }
                    row.HostRecordId = await ExistingCardAsync(job.InstitutionId, row.CardLookupHash, ct);
                    if (!row.HostRecordId.HasValue)
                    {
                        var entity = new InstitutionCardMaster
                        {
                            InstitutionId = job.InstitutionId, CardNumberEncrypted = row.CardCiphertext,
                            CardNumberLookupHash = row.CardLookupHash, CardStatus = (short)InstitutionCardStatus.Available,
                            AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow
                        };
                        InstitutionCardMasterHandlerBase.ApplyImport(entity, dto);
                        context.InstitutionCardMasters.Add(entity);
                        await context.SaveChangesAsync(ct);
                        row.HostRecordId = entity.Id;
                        row.Status = BulkImportRowStatus.Created;
                    }
                    else
                    {
                        row.Status = BulkImportRowStatus.Existing;
                    }
                }
                else
                {
                    await ProcessHostCatalogueRowAsync(job, table, source, mapping, row, ct);
                }
                row.Errors.Clear();
                await transaction.ReleaseSavepointAsync("host_bulk_row", ct);
            }
            catch (Exception error) when (error is ValidationErrorException ||
                error is DbUpdateException { InnerException: PostgresException })
            {
                await transaction.RollbackToSavepointAsync("host_bulk_row", ct);
                context.ChangeTracker.Clear();
                row.Status = BulkImportRowStatus.Failed;
                row.Errors = new List<string> { error is ValidationErrorException
                    ? error.Message : "Data changed or a database constraint rejected this row. Review and retry." };
                await transaction.ReleaseSavepointAsync("host_bulk_row", ct);
            }
            row.Processed = true;
            job.NextRow = index + 1;
        }
        job.Status = job.NextRow < preview.Rows.Count ? (int)BulkImportJobStatus.Running
            : (int)(preview.Rows.Any(row => row.Status == BulkImportRowStatus.Failed)
                ? BulkImportJobStatus.CompletedWithErrors : BulkImportJobStatus.Completed);
        await SaveProgress(job, preview, ct);
    }

    private async Task ProcessHostCatalogueRowAsync(BulkImportJob job, BulkImportTableDTO table,
        BulkImportSourceRowDTO source, IReadOnlyDictionary<string, string> mapping,
        BulkImportPreviewRowDTO row, CancellationToken ct)
    {
        var master = (BulkImportMaster)job.Master;
        row.HostRecordId = await PreviewHostCatalogueRowAsync(master, table, source, mapping,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase), ct);
        if (row.HostRecordId.HasValue)
        {
            row.Status = BulkImportRowStatus.Existing;
            return;
        }
        if (master is BulkImportMaster.HostModule or BulkImportMaster.HostSubModule)
        {
            var dto = master == BulkImportMaster.HostModule
                ? HostBulkImportTableMapper.ReadRow<HostModuleImportRowDTO>(table, source, mapping)
                : HostBulkImportTableMapper.ReadRow<HostSubModuleImportRowDTO>(table, source, mapping);
            var entity = new Module
            {
                ModuleCode = dto.ModuleCode.Trim(), ModuleName = dto.ModuleName.Trim(), PageName = dto.PageName.Trim(),
                DisplayName = dto.DisplayName, Urlpath = dto.URLPath, IsLeafNode = master == BulkImportMaster.HostSubModule,
                IsModuleDisplayInUI = dto.IsModuleDisplayInUI, IsCommonMenu = dto.IsCommonMenu,
                ModuleScope = dto.ModuleScope, IsActive = dto.IsActive, ImageIconWeb = dto.ImageIconWeb,
                ImageIconMobile = dto.ImageIconMobile, ItemPriority = dto.ItemPriority, Remark = dto.Remark,
                AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow
            };
            if (master == BulkImportMaster.HostSubModule)
            {
                var child = (HostSubModuleImportRowDTO)dto;
                entity.ParentModuleId = await context.Modules.Where(x => x.ModuleCode == child.ParentModuleCode.Trim() &&
                    x.ModuleScope == child.ModuleScope && x.IsActive).Select(x => x.Id).SingleAsync(ct);
            }
            context.Modules.Add(entity);
            await context.SaveChangesAsync(ct);
            row.HostRecordId = entity.Id;
        }
        else if (master == BulkImportMaster.HostOperation)
        {
            var dto = HostBulkImportTableMapper.ReadRow<HostOperationImportRowDTO>(table, source, mapping);
            var entity = new Operation
            {
                OperationName = dto.OperationName.Trim(), OperationType = dto.OperationType, Remark = dto.Remark,
                IsActive = dto.IsActive, IconImage = dto.IconImage, AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow
            };
            context.Operations.Add(entity);
            await context.SaveChangesAsync(ct);
            row.HostRecordId = entity.Id;
        }
        else
        {
            var dto = HostBulkImportTableMapper.ReadRow<HostModuleOperationImportRowDTO>(table, source, mapping);
            var entity = new ModuleOperationMapping
            {
                ModuleId = await context.Modules.Where(x => x.ModuleCode == dto.ModuleCode.Trim() && x.IsActive)
                    .Select(x => x.Id).SingleAsync(ct),
                OperationId = await context.Operations.Where(x => x.OperationType == dto.OperationType && x.IsActive)
                    .OrderBy(x => x.Id).Select(x => x.Id).FirstAsync(ct),
                DataViewStructureId = dto.DataViewStructureId, PageTypeId = dto.PageTypeId, PageUrl = dto.PageURL,
                IconUrl = dto.IconURL, IsCommonItem = dto.IsCommonItem, IsOperational = dto.IsOperational,
                Priority = dto.Priority, Remark = dto.Remark, IsActive = dto.IsActive,
                AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow
            };
            context.ModuleOperationMappings.Add(entity);
            await context.SaveChangesAsync(ct);
            row.HostRecordId = entity.Id;
        }
        row.Status = BulkImportRowStatus.Created;
    }

    #endregion
}
