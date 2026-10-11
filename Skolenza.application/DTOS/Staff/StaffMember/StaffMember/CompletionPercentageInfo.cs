using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    public class CompletionPercentageInfo 
    {       
    public  bool? hasBaseStaffMemberDocUploaded {  get; set; }
    public  bool? hasEducationDocUploaded {  get; set; }
    public  bool? hasExperienceDocUploaded {  get; set; }
    public  bool? hasContactDocUploaded {  get; set; }
    public  bool? hasDependentDocUploaded {  get; set; }
    public  bool? hasInsuranceDocUploaded {  get; set; }
    public  bool? hasSensitiveDocUploaded {  get; set; }
    public  bool? hasProfileImageUploaded {  get; set; }    
    public  bool? hasBankDocUploaded {  get; set; }    
    public  double? BaseStaffMemberoInfoCompletionPercentage { get; set; }
    public  double? BankCompletionPercentage { get; set; }
    public  double? EducationInfoCompletionPercentage { get; set; }
    public  double? ExperienceInfoCompletionPercentage { get; set; }  
    public  double? ContactInfoCompletionPercentage { get; set; }
    public  double? DependentInfoCompletionPercentage { get; set; }
    public  double? InsuranceInfoCompletionPercentage { get; set; }
    public  double? SensitiveInfoCompletionPercentage { get; set; }
    

    }
}


 