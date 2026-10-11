using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberBankDetail
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? Ifsccode { get; set; }

    public string? BranchName { get; set; }

    public string? AccountType { get; set; }

    public string? Upiid { get; set; }

    public bool IsPrimaryAccount { get; set; } = false;

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; } = false;

    public long? InfoVerifiedById { get; set; }

    public bool IsInfoVerified { get; set; } = false;

    public DateTime? InfoVerifiedDateTime { get; set; }

    public bool IsEditAllowed { get; set; } = false;

    public bool IsActive { get; set; } = false;

    public bool HasChequeDocUploaded { get; set; } = false;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public int? FileType { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;
}
