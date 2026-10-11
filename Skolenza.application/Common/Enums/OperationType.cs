using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Enums
{
    
        public enum OperationType
        {
            Add = 1,
            Update = 2,
            Delete = 3,
            View = 4,
            Approve = 5,
            Reject = 6,
            Assign = 7,
            Transfer = 8,
            Activate = 9,
            Deactivate = 10,
            Export = 11,
            Import = 12,
            Download = 13,
            Upload = 14,
            Print = 15,
            Share = 16,
            Reassign = 17,
            Verify = 18,
            Submit = 19,
            Review = 20,
            Close = 21,
            Reopen = 22,
            Apply = 23,
            Request = 24,
            Deposit = 25,
            Restore = 26,
            Resend = 27,
            Publish = 28,
            Archive = 29,
            Acknowledge = 30
        }

    public enum TabInfoType
    {
        StaffMember = 1,
        Bank = 2,
        Contact = 3,
        Experience = 4,
        Identity = 5,
        Education = 6,
        Dependent = 7,
       
    }
  
public enum RelationDependant
    {
    SPOUSE = 1,
    PARENT = 2,
    IN_LAWS = 3,
    CHILDREN = 4,
    Other = 99
    }
    public enum EmergencyContactRelation
    {
        Father = 1,
        Mother = 2,
        Husband = 3,
        Wife = 4,
        Son = 5,
        Daughter = 6,
        Brother = 7,
        Sister = 8,
        Grandfather = 9,
        Grandmother = 10,
        Uncle = 11,
        Aunt = 12,
        Cousin = 13,
        Guardian = 14,
        Landlord = 15,
        Owner = 16,

        Other = 99
    }

    public enum ExperienceDocumentType
    {
        Payslip = 1,
        ExperienceLetter = 2,
        JoiningLetter = 3,
        OfferLetter = 4,
        BankStatement = 5,
        TaxDocument = 6,
        GapCertificate = 7,
        Visa = 8,
        WorkPermit = 9,
        ImmigrationStamp = 10,
        ForeignContract = 11
    }
    public enum TicketStatus
    {
        PendingApproval =  1,
                Open = 2,
        Closed = 3,
        Rejected = 4,   
            
    }
    public enum MessageType
    {
        System = 1,
        User = 2,
        Internal = 3
    }
    public enum ThreadEntityType
    {
        Ticket = 1,
        Asset = 2,
        Leave = 3,
        Payroll = 4,
        StaffMember = 5,
        Notification = 6
    }
    public enum CurrencyCode
    {
        INR = 1,   // Indian Rupee
        USD = 2,   // US Dollar
        EUR = 3,   // Euro
        GBP = 4,   // British Pound
        AED = 5,   // UAE Dirham
        SGD = 6,   // Singapore Dollar
        AUD = 7,   // Australian Dollar
        CAD = 8,   // Canadian Dollar
        JPY = 9,   // Japanese Yen
        CNY = 10,  // Chinese Yuan
        CHF = 11,  // Swiss Franc
        ZAR = 12   // South African Rand
    }
}
