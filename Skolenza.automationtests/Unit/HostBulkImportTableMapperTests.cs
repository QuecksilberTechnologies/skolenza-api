using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using skolenza.api.Controllers.HostDevice;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("HostBulkImport")]
public sealed class HostBulkImportTableMapperTests
{
    [TestCase("InstitutionId")]
    [TestCase("IsOccupied")]
    [TestCase("CampusId")]
    public void Assignment_columns_are_rejected_even_when_not_explicitly_mapped(string field)
    {
        Assert.Throws<ValidationErrorException>(() => HostBulkImportTableMapper.ResolveColumns(
            new[] { "DeviceCode", field }, HostBulkImportTableMapper.DeviceColumns, null));
    }

    [TestCase("InstitutionId")]
    [TestCase("ModuleId")]
    [TestCase("OperationId")]
    [TestCase("AddedById")]
    public void Card_spreadsheet_cannot_override_trusted_context(string field)
    {
        Assert.Throws<ValidationErrorException>(() => HostBulkImportTableMapper.ResolveColumns(
            new[] { "Input" }, HostBulkImportTableMapper.CardColumns,
            System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { [field] = "Input" })));
    }

    [Test]
    public void Card_numbers_keep_leading_zeros_and_invoice_values_use_explicit_formats()
    {
        var table = new BulkImportTableDTO
        {
            Columns = ["CardNumber", "PurchaseInvoiceDate", "UnitPurchasePriceExcludingTax"],
            Rows = [new() { RowNumber = 2, Values = ["0000123456789012345", "2026-09-12", "12.50"] }]
        };
        var mapping = HostBulkImportTableMapper.ResolveColumns(table.Columns, HostBulkImportTableMapper.CardColumns, null);
        var dto = HostBulkImportTableMapper.ReadRow<CreateInstitutionCardMasterRequestDTO>(table, table.Rows[0], mapping);
        Assert.Multiple(() =>
        {
            Assert.That(dto.CardNumber, Is.EqualTo("0000123456789012345"));
            Assert.That(dto.PurchaseInvoiceDate, Is.EqualTo(new DateOnly(2026, 9, 12)));
            Assert.That(dto.UnitPurchasePriceExcludingTax, Is.EqualTo(12.50m));
            Assert.That(dto.PurchaseCurrencyCode, Is.EqualTo("INR"));
            Assert.That(dto.InstitutionId, Is.Null);
        });
    }

    [TestCase("PurchaseInvoiceDate", "12/09/2026")]
    [TestCase("UnitPurchasePriceExcludingTax", "1,200")]
    [TestCase("IsActive", "maybe")]
    [TestCase("TaxTreatment", "99999")]
    public void Invalid_formats_are_rejected_without_echoing_cell_data(string field, string value)
    {
        var table = new BulkImportTableDTO { Columns = [field] };
        var mapping = HostBulkImportTableMapper.ResolveColumns(table.Columns, HostBulkImportTableMapper.CardColumns, null);
        var error = Assert.Throws<ValidationErrorException>(() => HostBulkImportTableMapper.ReadRow<CreateInstitutionCardMasterRequestDTO>(
            table, new BulkImportSourceRowDTO { RowNumber = 2, Values = [value] }, mapping));
        Assert.That(error!.Message, Does.Not.Contain(value));
    }

    [Test]
    public void Nonstandard_headers_require_explicit_mapping()
    {
        Assert.That(HostBulkImportTableMapper.ResolveColumns(new[] { "Serial" }, HostBulkImportTableMapper.DeviceColumns, null), Is.Empty);
        var mapping = HostBulkImportTableMapper.ResolveColumns(new[] { "Serial" }, HostBulkImportTableMapper.DeviceColumns, "{\"SNo\":\"Serial\"}");
        var dto = HostBulkImportTableMapper.ReadRow<CreateDeviceMasterRequestDTO>(
            new BulkImportTableDTO { Columns = ["Serial"] }, new BulkImportSourceRowDTO { RowNumber = 2, Values = ["000123"] }, mapping);
        Assert.That(dto.SNo, Is.EqualTo("000123"));
    }

    [Test]
    public void Duplicate_headers_are_rejected()
    {
        Assert.Throws<ValidationErrorException>(() => HostBulkImportTableMapper.ResolveColumns(
            new[] { "CardNumber", "cardnumber" }, HostBulkImportTableMapper.CardColumns, null));
    }

    [Test]
    public void Catalogue_contracts_use_stable_codes_and_exclude_database_identity_fields()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HostBulkImportTableMapper.ModuleColumns, Does.Contain("ModuleCode").And.Contain("PageName"));
            Assert.That(HostBulkImportTableMapper.SubModuleColumns, Does.Contain("ParentModuleCode"));
            Assert.That(HostBulkImportTableMapper.OperationColumns, Does.Contain("OperationType").And.Contain("OperationName"));
            Assert.That(HostBulkImportTableMapper.ModuleOperationColumns,
                Does.Contain("ModuleCode").And.Contain("OperationType"));
            Assert.That(HostBulkImportTableMapper.ModuleOperationColumns, Does.Not.Contain("ModuleId").And.Not.Contain("OperationId"));
        });
    }

    [Test]
    public void Catalogue_rows_parse_typed_values_without_numeric_database_references()
    {
        var table = new BulkImportTableDTO
        {
            Columns = ["ParentModuleCode", "ModuleCode", "ModuleName", "PageName", "ModuleScope", "IsActive"],
            Rows = [new() { RowNumber = 2, Values = ["HOST_MODULES", "QA_CHILD", "QA Child", "qa-child", "2", "true"] }]
        };
        var mapping = HostBulkImportTableMapper.ResolveColumns(table.Columns,
            HostBulkImportTableMapper.SubModuleColumns, null);
        var dto = HostBulkImportTableMapper.ReadRow<HostSubModuleImportRowDTO>(table, table.Rows[0], mapping);
        Assert.Multiple(() =>
        {
            Assert.That(dto.ParentModuleCode, Is.EqualTo("HOST_MODULES"));
            Assert.That(dto.ModuleScope, Is.EqualTo(2));
            Assert.That(dto.IsActive, Is.True);
        });
    }

    [TestCase("ModuleId")]
    [TestCase("OperationId")]
    [TestCase("ParentModuleId")]
    public void Catalogue_spreadsheets_cannot_supply_database_ids(string field)
    {
        Assert.Throws<ValidationErrorException>(() => HostBulkImportTableMapper.ResolveColumns(
            new[] { field }, HostBulkImportTableMapper.ModuleOperationColumns, null));
    }

    [TestCase(typeof(HostModuleBulkImportController), "api/Module/import")]
    [TestCase(typeof(HostSubModuleBulkImportController), "api/SubModule/import")]
    [TestCase(typeof(HostOperationBulkImportController), "api/Operation/import")]
    [TestCase(typeof(HostModuleOperationBulkImportController), "api/ModuleOperation/import")]
    public void Catalogue_controllers_publish_the_shared_eight_endpoint_contract(Type controller, string route)
    {
        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>().Select(attribute => attribute.Template), Does.Contain(route));
            Assert.That(typeof(HostBulkImportController).GetMethods()
                .Count(method => method.GetCustomAttributes(true).Any(attribute =>
                    attribute is HttpMethodAttribute)), Is.EqualTo(8));
        });
    }
}
