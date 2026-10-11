using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class StaffMemberCompletionRepository : IStaffMemberCompletionRepository
    {
        //private readonly IBankCompletionService _bankService;
        //private readonly IContactCompletionService _contactService;
        //private readonly IImageCompletionService _imageService;
        //private readonly IExperienceCompletionService _experienceService;

        //public StaffMemberCompletionService(
        //    IBankCompletionService bankService,
        //    IContactCompletionService contactService,
        //    IImageCompletionService imageService,
        //    IExperienceCompletionService experienceService
        //)
        //{
        //    _bankService = bankService;
        //    _contactService = contactService;
        //    _imageService = imageService;
        //    _experienceService = experienceService;
        //}

        //public async Task<StaffMemberCompletionResponseDTO> GetStaffMemberCompletionAsync(long staffMemberId)
        //{
        //    var result = new StaffMemberCompletionResponseDTO
        //    {
        //        StaffMemberId = staffMemberId
        //    };

        //    result.Sections.Add(await _bankService.GetBankCompletionAsync(staffMemberId));
        //    result.Sections.Add(await _contactService.GetContactCompletionAsync(staffMemberId));
        //    result.Sections.Add(await _imageService.GetImageCompletionAsync(staffMemberId));
        //    result.Sections.Add(await _experienceService.GetExperienceCompletionAsync(staffMemberId));

        //    return result;
        //}
        public Task<StaffMemberCompletionResponseDTO> GetStaffMemberCompletionAsync(long staffMemberId)
        {
            throw new NotImplementedException();
        }
    }
}
