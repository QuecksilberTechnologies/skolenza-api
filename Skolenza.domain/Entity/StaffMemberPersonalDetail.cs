using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberPersonalDetail
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public string? AadhaarNumber { get; set; }

    public string? PanNumber { get; set; }

    public string? PassportNumber { get; set; }

    public string? DrivingLicenseNumber { get; set; }

    public string? VoterId { get; set; }

    public string? BloodGroup { get; set; }

    public bool MaritalStatus { get; set; }

    public string? Nationality { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactNumber { get; set; }

    public bool IsActive { get; set; }

    public bool? IsEditAllowed { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public long? InfoVerifiedById { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public bool? IsInfoVerified { get; set; }

    public bool? HasPanIdUploaded { get; set; }

    public bool? HasAadhaarIdUploaded { get; set; }

    public bool? HasPassportIdUploaded { get; set; }

    public string? PanDocName { get; set; }

    public string? PassportDocName { get; set; }

    public string? AadhaarDocName { get; set; }

    public string? PanDocPath { get; set; }

    public string? PassportDocPath { get; set; }

    public string? AadhaarDocPath { get; set; }

    public string? EmergencyContactRelation { get; set; }

    public string? Uannumber { get; set; }

    public bool? HasEpfaccount { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;
}
