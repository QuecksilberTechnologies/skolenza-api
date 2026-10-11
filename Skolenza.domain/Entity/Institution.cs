using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Institution
{
    public long Id { get; set; }

    public int InstitutionTypeId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? InstitutionCode { get; set; }

    public string CompanyEmailDomain { get; set; } = null!;

    public string InstitutionEmail { get; set; } = null!;

    public string? ContactPersonName { get; set; }

    public int? GenderId { get; set; }

    public string? ContactNumber { get; set; }

    public int CountryId { get; set; }

    public bool IsVerified { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public int? DefaultCurrency { get; set; }

    public virtual ICollection<AssetCategory> AssetCategory { get; set; } = new List<AssetCategory>();

    public virtual ICollection<ComplianceRule> ComplianceRule { get; set; } = new List<ComplianceRule>();


    public virtual ICollection<Department> Department { get; set; } = new List<Department>();

    public virtual ICollection<Designation> Designation { get; set; } = new List<Designation>();

    public virtual ICollection<StaffMember> StaffMember { get; set; } = new List<StaffMember>();

    public virtual ICollection<StaffMemberCodePattern> StaffMemberCodePattern { get; set; } = new List<StaffMemberCodePattern>();


    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMapping { get; set; } = new List<StaffMemberManagerMapping>();

    public virtual Gender? Gender { get; set; }

    public virtual ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();


    public virtual ICollection<LeaveType> LeaveType { get; set; } = new List<LeaveType>();

    public virtual ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();

    public virtual ICollection<PayrollRun> PayrollRun { get; set; } = new List<PayrollRun>();

    public virtual ICollection<PolicyType> PolicyType { get; set; } = new List<PolicyType>();

    public virtual ICollection<RequestType> RequestType { get; set; } = new List<RequestType>();

    public virtual ICollection<SalaryComponentMaster> SalaryComponentMaster { get; set; } = new List<SalaryComponentMaster>();

    public virtual ICollection<SalaryStructure> SalaryStructure { get; set; } = new List<SalaryStructure>();

    public virtual ICollection<TaxRule> TaxRule { get; set; } = new List<TaxRule>();

    public virtual ICollection<TaxSlab> TaxSlab { get; set; } = new List<TaxSlab>();

    public virtual ICollection<InstitutionEmailConfig> InstitutionEmailConfig { get; set; } = new List<InstitutionEmailConfig>();

    public virtual ICollection<InstitutionEnabledModule> InstitutionEnabledModule { get; set; } = new List<InstitutionEnabledModule>();

    public virtual ICollection<InstitutionEnabledOperation> InstitutionEnabledOperation { get; set; } = new List<InstitutionEnabledOperation>();

    public virtual InstitutionType InstitutionType { get; set; } = null!;

    public virtual ICollection<InstitutionProfile> InstitutionProfile { get; set; } = new List<InstitutionProfile>();

    public virtual ICollection<InstitutionSubscription> InstitutionSubscription { get; set; } = new List<InstitutionSubscription>();

    public virtual ICollection<Ticket> Ticket { get; set; } = new List<Ticket>();

    public virtual ICollection<TicketClassification> TicketClassification { get; set; } = new List<TicketClassification>();

    public virtual ICollection<TicketHeader> TicketHeader { get; set; } = new List<TicketHeader>();

    public virtual ICollection<TicketType> TicketType { get; set; } = new List<TicketType>();
}
