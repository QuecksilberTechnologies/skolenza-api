
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Registration
{
    //public class CandidateRequestDTO

    //    {

    //    public string FirstName { get; set; } = null!;

    //    public string? LastName { get; set; }

    //    public string Email { get; set; } = null!;

    //    public string PhoneNumber { get; set; } = null!;

    //    public string? Pan { get; set; }

    //    public string? Aadhaar { get; set; }

    //    public string CandidateReferenceCode { get; set; } = null!;

    //    public string? ResumeUrl { get; set; }

    //    public decimal ExperienceYears { get; set; }

    //    public string? CurrentLocation { get; set; }

    //    public decimal? ExpectedSalary { get; set; }

    //    public string? CurrentCompany { get; set; }

    //    public int? NoticePeriod { get; set; }

    //    public DateOnly? DateOfBirth { get; set; }

    //    public DateTime AppliedDate { get; set; }

    //    public string? SkillSet { get; set; }

    //    public bool IsActive { get; set; }

    //    public string? ActionStatus { get; set; }

    //    public string? Education { get; set; }

    //    public bool? IsFresher { get; set; }

    //    public byte[]? Resume { get; set; }


    //    //public bool IsActive { get; set; }

    //    //public string? ActionStatus { get; set; }

    //    //public string? Education { get; set; }

    //    //public bool? IsFresher { get; set; }

    //    //public byte[]? Resume { get; set; }


    //}

    public class CandidateRequestDTO
    {
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pan { get; set; }
        public string? Aadhaar { get; set; }
        public string CandidateReferenceCode { get; set; }
        public string? ResumeUrl { get; set; } = "https://example.com/resume/jane_smith.pdf";
        public decimal ExperienceYears { get; set; }
        public string? CurrentLocation { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public string? CurrentCompany { get; set; }
        public int? NoticePeriod { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public string? SkillSet { get; set; }
        public string? SkillSetID { get; set; }
        public bool IsActive { get; set; }
        public string? ActionStatus { get; set; }
        public string? Education { get; set; }
        public bool? IsFresher { get; set; } = false;
        public string? ResumeUpload { get; set; } = null; // Assume null for now
    }

}

