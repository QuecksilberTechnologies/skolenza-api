using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberImport")]
public sealed class StaffMemberImportTests
{
    [TestCase(false, "43905")]
    [TestCase(true, "42443")]
    public async Task Xlsx_reader_retains_the_workbook_date_system(bool date1904, string serial)
    {
        using var stream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            void Entry(string path, string content)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                writer.Write(content);
            }
            Entry("xl/workbook.xml", $"<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><workbookPr date1904=\"{(date1904 ? 1 : 0)}\"/><sheets><sheet name=\"StaffMembers\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<Relationships><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Entry("xl/worksheets/sheet1.xml", $"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>DateOfOnBoarding</t></is></c></row><row r=\"2\"><c r=\"A2\"><v>{serial}</v></c></row></sheetData></worksheet>");
        }
        stream.Position = 0;
        var table = await BulkImportTableReader.ReadAsync(new BulkImportPreviewRequestDTO
        {
            File = new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "File", "staffMember.xlsx")
        }, CancellationToken.None);
        Assert.That(table.IsExcel, Is.True);
        Assert.That(table.Uses1904DateSystem, Is.EqualTo(date1904));
        Assert.That(StaffMemberImportRowMapper.ParseDate(table.Rows[0].Values[0], table.IsExcel, table.Uses1904DateSystem)
            ?.ToString("yyyy-MM-dd"), Is.EqualTo("2020-03-15"));
    }

    [TestCase("PreviewBulkImport", "bulk/preview")]
    [TestCase("ConfirmBulkImport", "bulk/confirm")]
    [TestCase("GetBulkImport", "bulk/jobs/{jobId:guid}")]
    [TestCase("ListBulkImports", "bulk/jobs")]
    [TestCase("RetryBulkImport", "bulk/retry")]
    [TestCase("CancelBulkImport", "bulk/cancel")]
    [TestCase("DownloadBulkTemplate", "bulk/template")]
    [TestCase("DownloadBulkReport", "bulk/jobs/{jobId:guid}/report")]
    [TestCase("SendBulkInvitations", "bulk/send-invitations")]
    public void StaffMember_routes_require_authentication(string method, string route)
    {
        var info = typeof(skolenza.api.Controllers.Staff.StaffController).GetMethod(method)!;
        Assert.That(info.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true), Is.Not.Empty);
        var routes = info.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute), true)
            .Cast<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>();
        Assert.That(routes.Single().Template, Is.EqualTo(route));
    }

    [TestCase("2020-03-15", false, false, "2020-03-15")]
    [TestCase("43905", true, false, "2020-03-15")]
    [TestCase("42443", true, true, "2020-03-15")]
    [TestCase("15/03/2020", false, false, null)]
    [TestCase("43905", false, false, null)]
    [TestCase("60", true, false, null)]
    [TestCase("43905.5", true, false, null)]
    [TestCase("not a date", false, false, null)]
    public void Joining_dates_are_unambiguous_and_respect_excel_date_system(string value, bool excel, bool date1904, string? expected)
    {
        Assert.That(StaffMemberImportRowMapper.ParseDate(value, excel, date1904)?.ToString("yyyy-MM-dd"), Is.EqualTo(expected));
    }

    [Test]
    public void Aliases_require_explicit_mapping_and_source_cannot_be_reused()
    {
        var table = new BulkImportTableDTO { Columns = ["First Name", "Family", "Email"] };
        var mapping = BulkImportPreviewService.ResolveColumnMapping(table,
            "{\"LastName\":\"Family\",\"OfficialEmail\":\"Email\"}", BulkImportConstants.StaffMemberColumns);
        Assert.That(mapping["FirstName"], Is.EqualTo("First Name"));
        Assert.That(mapping["LastName"], Is.EqualTo("Family"));
        Assert.That(BulkImportPreviewService.ResolveColumnMapping(table, null, BulkImportConstants.StaffMemberColumns)
            .ContainsKey("OfficialEmail"), Is.False);
        Assert.Throws<ValidationErrorException>(() => BulkImportPreviewService.ResolveColumnMapping(table,
            "{\"FirstName\":\"Family\",\"LastName\":\"Family\"}", BulkImportConstants.StaffMemberColumns));
    }

    [Test]
    public void Address_requires_contact_number_and_reuses_personal_primary_contact_contract()
    {
        var row = ValidRow();
        row.Values["Address"] = "Flat 12, Example Street";
        var mapped = StaffMemberImportRowMapper.Map(row);
        Assert.That(row.Errors, Has.Some.Contains("ContactNumber"));
        Assert.That(mapped.Contact!.IsPrimary, Is.True);
        Assert.That(mapped.Contact.ContactType, Is.EqualTo(ConstantValues.ContactTypeEnum.Personal));
        row.Errors.Clear();
        row.Values["ContactNumber"] = "+910000000000";
        Assert.That(StaffMemberImportRowMapper.Map(row).Contact!.ContactNumber, Is.EqualTo("+910000000000"));
        Assert.That(row.Errors, Is.Empty);
    }

    [TestCase("OfficialEmail", "invalid email")]
    [TestCase("DepartmentId", "-1")]
    [TestCase("InstitutionStaffCategoryMasterId", "0")]
    [TestCase("StaffPositionMasterId", "")]
    [TestCase("GenderId", "Female")]
    [TestCase("IsActive", "maybe")]
    [TestCase("FirstName", "")]
    public void Invalid_required_fields_are_reported(string field, string value)
    {
        var row = ValidRow();
        row.Values[field] = value;
        StaffMemberImportRowMapper.Map(row);
        Assert.That(row.Errors, Is.Not.Empty);
    }

    internal static BulkImportPreviewRowDTO ValidRow()
    {
        return new BulkImportPreviewRowDTO
        {
            Values = new()
            {
                ["FirstName"] = "Import", ["LastName"] = "Example", ["OfficialEmail"] = "example@example.invalid",
                ["DateOfBirth"] = "1990-01-01", ["DateOfOnBoarding"] = "2020-03-15",
                ["GenderId"] = "1", ["CountryId"] = "1", ["DepartmentId"] = "1",
                ["InstitutionStaffCategoryMasterId"] = "1", ["StaffPositionMasterId"] = "1",
                ["StaffEmploymentTypeId"] = "1", ["HasPermanent"] = "true", ["IsActive"] = "true"
            }
        };
    }
}

