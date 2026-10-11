using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{

    public class GetBankAccessResponseDTO
    {
        public FieldWithAccess<long> Id { get; set; } = new();
        public FieldWithAccess<long?> StaffMemberId { get; set; } = new();
        public FieldWithAccess<string?> BankName { get; set; } = new();
        public FieldWithAccess<string?> AccountNumber { get; set; } = new();
        public FieldWithAccess<string?> IFSCCode { get; set; } = new();
        public FieldWithAccess<string?> BranchName { get; set; } = new();
        public FieldWithAccess<string?> AccountType { get; set; } = new();
        public FieldWithAccess<string?> UPIId { get; set; } = new();
        public FieldWithAccess<bool?> IsPrimaryAccount { get; set; } = new();

        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();

        public FieldWithAccess<bool?> IsSoftDeleted { get; set; } = new();
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();

        public FieldWithAccess<bool?> IsEditAllowed { get; set; } = new();
    }

}


