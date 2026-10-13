using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMember
{
    public long Id { get; set; }

    public long? InstitutionId { get; set; }

    public int? StaffMemberDocumentId { get; set; }

    public string? EmployementCode { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public int? GenderId { get; set; }

    public string? FirstName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public DateTime? DateOfOnBoarding { get; set; }

    public DateTime? DateOfExit { get; set; }

    public int? DesignationId { get; set; }

    public int? StaffEmploymentTypeId { get; set; }

    public short? StaffCategoryId { get; set; }

    public int? DepartmentId { get; set; }

    public string? OfficialEmail { get; set; }

    public bool HasPermanent { get; set; }

    public bool IsActive { get; set; }

    public string? Remark { get; set; }

    public bool IsEditAllowed { get; set; }

    public bool IsInfoVerified { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? InfoVerifiedById { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public string? Description { get; set; }

    public int CountryId { get; set; }

    public string? EmergencyContactNumber { get; set; }

    public int? Relation { get; set; }

    public string? BloodGroup { get; set; }

    public string? MobileNumber { get; set; }

    public bool? IsMarried { get; set; }

    public string? EmergencyContactPerson { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual Designation? Designation { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<StaffMemberBankDetail> StaffMemberBankDetail { get; set; } = new List<StaffMemberBankDetail>();

    public virtual ICollection<StaffMemberCategorySkill> StaffMemberCategorySkill { get; set; } = new List<StaffMemberCategorySkill>();

    public virtual ICollection<StaffMemberContact> StaffMemberContact { get; set; } = new List<StaffMemberContact>();

    public virtual ICollection<StaffMemberDailyAttendance> StaffMemberDailyAttendance { get; set; } = new List<StaffMemberDailyAttendance>();

    public virtual ICollection<StaffMemberDependent> StaffMemberDependent { get; set; } = new List<StaffMemberDependent>();

    public virtual ICollection<StaffMemberExperience> StaffMemberExperience { get; set; } = new List<StaffMemberExperience>();

    public virtual ICollection<StaffMemberIdentity> StaffMemberIdentity { get; set; } = new List<StaffMemberIdentity>();

    public virtual ICollection<StaffMemberImage> StaffMemberImage { get; set; } = new List<StaffMemberImage>();


    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMappingStaffMember { get; set; } = new List<StaffMemberManagerMapping>();

    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMappingManager { get; set; } = new List<StaffMemberManagerMapping>();

    public virtual ICollection<StaffMemberPersonalDetail> StaffMemberPersonalDetail { get; set; } = new List<StaffMemberPersonalDetail>();


    public virtual ICollection<StaffMemberSalary> StaffMemberSalary { get; set; } = new List<StaffMemberSalary>();

    public virtual ICollection<StaffMemberTaxProfile> StaffMemberTaxProfile { get; set; } = new List<StaffMemberTaxProfile>();

    public virtual StaffEmploymentType? StaffEmploymentType { get; set; }

    public virtual StaffCategory? StaffCategory { get; set; }

    public virtual ICollection<StaffMembersChangedTypeHistory> StaffMembersChangedTypeHistory { get; set; } = new List<StaffMembersChangedTypeHistory>();

    public virtual Gender? Gender { get; set; }

    public virtual ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();

    public virtual ICollection<LoginCredential> LoginCredential { get; set; } = new List<LoginCredential>();

    public virtual ICollection<PayrollStaffMember> PayrollStaffMember { get; set; } = new List<PayrollStaffMember>();

    public virtual Institution? Institution { get; set; }

    public virtual ICollection<ThreadMessage> ThreadMessage { get; set; } = new List<ThreadMessage>();

    public virtual ICollection<Ticket> TicketApprovedByUser { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketAssignedToUser { get; set; } = new List<Ticket>();

    public virtual ICollection<TicketAttachment> TicketAttachment { get; set; } = new List<TicketAttachment>();

    public virtual ICollection<TicketHistory> TicketHistory { get; set; } = new List<TicketHistory>();

    public virtual ICollection<Ticket> TicketRecommendedByUser { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketRequestedByUser { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketRequestedForUser { get; set; } = new List<Ticket>();

    public virtual ICollection<UserAttendanceSetting> UserAttendanceSetting { get; set; } = new List<UserAttendanceSetting>();

    public virtual ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();
}
