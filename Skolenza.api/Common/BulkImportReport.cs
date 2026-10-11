using System.Text;
using System.Text.Json;
using skolenza.application.DTOS.Common;

namespace skolenza.api.Common;

/// <summary>Exports correction-friendly row results without exposing queue internals or institution identifiers.</summary>
public static class BulkImportReport
{
    public static byte[] Create(BulkImportJobResponseDTO job)
    {
        var staffMember = job.Master == skolenza.application.Common.Enums.BulkImportMaster.StaffMember;
        var csv = new StringBuilder(staffMember
            ? "RowNumber,Status,RecordId,Values,Errors,ProposedStaffMemberCode,InvitationStatus,InvitationError\r\n"
            : "RowNumber,Status,RecordId,Values,Errors\r\n");
        foreach (var row in job.Preview?.Rows ?? new())
        {
            csv.Append(row.RowNumber).Append(',').Append(row.Status).Append(',')
                .Append(row.HostRecordId ?? row.ExistingId).Append(',').Append(Cell(JsonSerializer.Serialize(row.Values)))
                .Append(',').Append(Cell(string.Join(" | ", row.Errors)));
            if (staffMember)
            {
                csv.Append(',').Append(Cell(row.ProposedStaffMemberCode ?? string.Empty));
                csv.Append(',').Append(row.InvitationStatus).Append(',').Append(Cell(row.InvitationError ?? string.Empty));
            }
            csv.Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    private static string Cell(string value)
    {
        if (value.Length > 0 && "=+-@\t\r\n".Contains(value[0]))
        {
            value = "'" + value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
