using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Attributes;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberBankEditableFieldsDTO
    {
        [AccessControl(ReadOnly = true)]
        public int StaffMemberId { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? BankName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? AccountNumber { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? IFSCCode { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? BranchName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? AccountType { get; set; } // e.g., Savings, Current, etc.

        [AccessControl(ReadOnly = false)]
        public string? UPIId { get; set; }

        [AccessControl(ReadOnly = false)]
        public bool IsPrimaryAccount { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? AddedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? AddedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? UpdatedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? UpdatedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? SoftDeletedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? DeletedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsSoftDeleted { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? InfoVerifiedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsInfoVerified { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? InfoVerifiedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsEditAllowed { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsActive { get; set; }
    }
}
