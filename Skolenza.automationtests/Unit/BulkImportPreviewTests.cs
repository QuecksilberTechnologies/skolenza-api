using System.IO.Compression;
using System.Text;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Exercises real parsing and preview outcomes, without writes or live AI requests.</summary>
[TestFixture]
[Category("BulkImport")]
public sealed class BulkImportPreviewTests
{
    #region Input cases

    [Test]
    public async Task Csv_preserves_quoted_commas_and_source_row_numbers()
    {
        var table = await Read("DepartmentName,Description\nIT,\"Software, support\"\nHR,People");
        Assert.Multiple(() =>
        {
            Assert.That(table.Rows, Has.Count.EqualTo(2));
            Assert.That(table.Rows[0].RowNumber, Is.EqualTo(2));
            Assert.That(table.Rows[0].Values[1], Is.EqualTo("Software, support"));
        });
    }

    [Test]
    public async Task Pasted_excel_tabs_are_supported()
    {
        var table = await Read("Department Name\tDescription\nIT\tSupport");
        Assert.That(table.Columns, Has.Count.EqualTo(2));
    }

    [TestCase("")]
    [TestCase("Name\n")]
    [TestCase("Name,Name\nIT,HR")]
    [TestCase("Name,Description\nIT")]
    [TestCase("Name\n\"unclosed")]
    public void Invalid_table_is_rejected(string input)
    {
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Read(input));
    }

    [Test]
    public void Rows_over_the_limit_are_rejected()
    {
        var input = "DepartmentName\n" + string.Join("\n", Enumerable.Repeat("IT", BulkImportConstants.MaxRows + 1));
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Read(input));
    }

    [Test]
    public void Canonical_template_columns_use_current_institution_and_staff_contracts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BulkImportConstants.DepartmentColumns,
                Is.EqualTo(new[] { "DepartmentName", "Description", "Remark", "IsActive" }));
            Assert.That(BulkImportConstants.RoleColumns,
                Is.EqualTo(new[] { "RoleName", "RoleType", "Remark", "IsActive" }));
            Assert.That(BulkImportConstants.StaffEmploymentTypeColumns,
                Is.EqualTo(new[] { "TypeName", "Description", "Remark", "IsActive" }));
            Assert.That(BulkImportConstants.StaffMemberColumns,
                Does.Contain("StaffMemberCode"));
            Assert.That(BulkImportConstants.StaffMemberColumns,
                Does.Contain("StaffEmploymentTypeId"));
            Assert.That(BulkImportConstants.StaffMemberColumns,
                Does.Not.Contain("EmployeeCode"));
            Assert.That(BulkImportConstants.StaffMemberColumns,
                Does.Not.Contain("EmployeeTypeId"));
        });
    }

    [Test]
    public void File_and_paste_together_are_rejected()
    {
        var request = new BulkImportPreviewRequestDTO
        {
            PastedText = "Name\nIT",
            File = File("Name\nIT", "departments.csv")
        };
        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await BulkImportTableReader.ReadAsync(request, CancellationToken.None));
    }

    [TestCase("departments.xls")]
    [TestCase("departments.xlsm")]
    [TestCase("departments.exe")]
    public void Unsupported_file_types_are_rejected(string name)
    {
        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await BulkImportTableReader.ReadAsync(new BulkImportPreviewRequestDTO
            {
                File = File("DepartmentName\nIT", name)
            }, CancellationToken.None));
    }

    [Test]
    public async Task Xlsx_inline_and_shared_strings_are_read()
    {
        var table = await BulkImportTableReader.ReadAsync(new BulkImportPreviewRequestDTO
        {
            File = Workbook(false)
        }, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns, Is.EqualTo(new[] { "DepartmentName" }));
            Assert.That(table.Rows[0].Values[0], Is.EqualTo("IT"));
            Assert.That(table.Rows[0].RowNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void Xlsx_formulas_are_not_evaluated_or_imported()
    {
        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await BulkImportTableReader.ReadAsync(new BulkImportPreviewRequestDTO
            {
                File = Workbook(true)
            }, CancellationToken.None));
    }

    #endregion

    #region Master matching







    [Test]
    public async Task Custom_column_mapping_is_explicit()
    {
        var table = await Read("Dept\nFinance");
        var preview = BulkImportPreviewService.Build(BulkImportMaster.Department,
            "{\"DepartmentName\":\"Dept\"}", table, Departments(),
            Array.Empty<GetRoleResponseDTO>());
        Assert.That(preview.ReadyCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Unmapped_required_column_is_not_guessed()
    {
        var preview = await Preview(BulkImportMaster.Department, "Unclear\nFinance");
        Assert.That(preview.IsValid, Is.False);
        Assert.That(preview.Errors, Is.Not.Empty);
    }

    [Test]
    public async Task InstitutionId_cannot_be_mapped_from_uploaded_data()
    {
        var table = await Read("Dept,Institution\nFinance,999");
        Assert.Throws<ValidationErrorException>(() => BulkImportPreviewService.Build(
            BulkImportMaster.Department, "{\"DepartmentName\":\"Dept\",\"InstitutionId\":\"Institution\"}",
            table, Departments(), Array.Empty<GetRoleResponseDTO>()));
    }

    [Test]
    public async Task Role_type_uses_existing_constants_and_preserves_existing_role()
    {
        var table = await Read($"RoleName,RoleType\nStaffMember,{ConstantValues.RoleTypeStaffMember}");
        var preview = BulkImportPreviewService.Build(BulkImportMaster.Role, null, table,
            Departments(), new[]
            {
                new GetRoleResponseDTO { Id = 4, RoleName = "StaffMember", RoleType = ConstantValues.RoleTypeStaffMember, IsActive = true }
            });
        Assert.That(preview.ExistingCount, Is.EqualTo(1));
    }

    [TestCase(BulkImportMaster.Department)]
    [TestCase(BulkImportMaster.Role)]
    [TestCase(BulkImportMaster.StaffEmploymentType)]
    public async Task Active_upload_marks_matching_inactive_master_for_reactivation(BulkImportMaster master)
    {
        var table = master switch
        {
            BulkImportMaster.Department => await Read("DepartmentName,IsActive\nInactive,true"),
            BulkImportMaster.Role => await Read($"RoleName,RoleType,IsActive\nInactive,{ConstantValues.RoleTypeStaffMember},true"),
            _ => await Read("TypeName,IsActive\nInactive,true")
        };
        var preview = BulkImportPreviewService.Build(master, null, table,
            Departments(),
            new[] { new GetRoleResponseDTO { Id = 22, RoleName = "Inactive", RoleType = ConstantValues.RoleTypeStaffMember, IsActive = false } },
            new[] { new global::skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO { Id = 23, TypeName = "Inactive", IsActive = false } });

        if (master == BulkImportMaster.Department)
        {
            preview = BulkImportPreviewService.Build(master, null, table,
                new[] { new GetDepartmentResponseDTO { Id = 20, DepartmentName = "Inactive", IsActive = false } },
                Array.Empty<GetRoleResponseDTO>());
        }

        Assert.Multiple(() =>
        {
            Assert.That(preview.IsValid, Is.True);
            Assert.That(preview.Rows.Single().Status, Is.EqualTo(BulkImportRowStatus.Existing));
            Assert.That(preview.Rows.Single().WillReactivate, Is.True);
            Assert.That(preview.Rows.Single().Errors, Is.Empty);
        });
    }

    [Test]
    public async Task Inactive_upload_keeps_matching_inactive_role_without_reactivation()
    {
        var table = await Read($"RoleName,RoleType,IsActive\nInactive,{ConstantValues.RoleTypeStaffMember},false");
        var preview = BulkImportPreviewService.Build(BulkImportMaster.Role, null, table,
            Departments(), new[]
            {
                new GetRoleResponseDTO { Id = 22, RoleName = "Inactive", RoleType = ConstantValues.RoleTypeStaffMember, IsActive = false }
            });

        Assert.Multiple(() =>
        {
            Assert.That(preview.IsValid, Is.True);
            Assert.That(preview.Rows.Single().WillReactivate, Is.False);
        });
    }


    private static Task<BulkImportTableDTO> Read(string input)
    {
        return BulkImportTableReader.ReadAsync(new BulkImportPreviewRequestDTO { PastedText = input }, CancellationToken.None);
    }

    private static async Task<BulkImportPreviewResponseDTO> Preview(BulkImportMaster master, string input)
    {
        return BulkImportPreviewService.Build(master, null, await Read(input), Departments(),
            Array.Empty<GetRoleResponseDTO>());
    }

    private static GetDepartmentResponseDTO[] Departments()
    {
        return new[]
        {
            new GetDepartmentResponseDTO { Id = 10, DepartmentName = "IT", IsActive = true },
            new GetDepartmentResponseDTO { Id = 20, DepartmentName = "HR", IsActive = true },
            new GetDepartmentResponseDTO { Id = 30, DepartmentName = "Inactive", IsActive = false }
        };
    }

    private static IFormFile File(string content, string name)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "File", name);
    }

    private static IFormFile Workbook(bool formula)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "xl/workbook.xml", """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Departments" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Write(archive, "xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            Write(archive, "xl/sharedStrings.xml", """
                <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><si><t>IT</t></si></sst>
                """);
            Write(archive, "xl/worksheets/sheet1.xml", """
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>DepartmentName</t></is></c></row><row r="2"><c r="A2" t="s">
                """ + (formula ? "<f>1+1</f>" : string.Empty) + "<v>0</v></c></row></sheetData></worksheet>");
        }

        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "File", "departments.xlsx");
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }

    #endregion
}
