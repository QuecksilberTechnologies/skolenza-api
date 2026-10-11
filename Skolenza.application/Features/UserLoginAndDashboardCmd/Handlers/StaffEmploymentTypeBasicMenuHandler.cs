// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Processes the StaffEmploymentTypeBasicMenuCommand use case.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Constants;
using FluentValidation;
using Microsoft.Extensions.Logging;

using MediatR;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the command request for StaffMember Type Basic Menu.
    /// </summary>
public class StaffEmploymentTypeBasicMenuCommand : IRequest<ApiResponse<AccessDetailResponseDTO>>
    {
       
        public AccessDetailRequestDTO AccessDetailDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="StaffEmploymentTypeBasicMenuCommand"/> class.
        /// </summary>
        public StaffEmploymentTypeBasicMenuCommand(AccessDetailRequestDTO accessRequestDTO)
        {
            AccessDetailDTO = accessRequestDTO;
        }

    }

    #endregion
}

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    /// <summary>
    /// Handles the request for StaffMember Type Basic Menu.
    /// </summary>
public class AttendanceRequestHandler : IRequestHandler<StaffEmploymentTypeBasicMenuCommand, ApiResponse<AccessDetailResponseDTO>>
    {
        #region Fields

        private readonly IStaffEmploymentTypeBasicMenuRepository staffEmploymentTypeBasicMenuRepository;
        private readonly IMapper mapper;
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendanceRequestHandler"/> class.
        /// </summary>
   
        
        public AttendanceRequestHandler(IStaffEmploymentTypeBasicMenuRepository staffEmploymentTypeBasicMenuRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this.staffEmploymentTypeBasicMenuRepository = staffEmploymentTypeBasicMenuRepository;
            this.mapper = mapper;
            this.unitOfWork = unitOfWork;
    
        }
        #endregion

        #region Handler

        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The response produced by handling the request.</returns>

       
        public async  Task<ApiResponse<AccessDetailResponseDTO>> Handle(StaffEmploymentTypeBasicMenuCommand? request, CancellationToken cancellationToken)
        {
           
            // Fetch the basic menus for the given staffMember type and platform
            var basicMenuDTOs = await staffEmploymentTypeBasicMenuRepository.GetBasicMenuDTO(request.AccessDetailDTO.StaffEmploymentTypeId, request.AccessDetailDTO.ForPlatform);

             // Create the AccessDetailResponseDTO object and bind the fetched menus
            var accessDetailResponse = new AccessDetailResponseDTO
            {
                StaffMemberId = request.AccessDetailDTO.StaffMemberId,  // Assuming StaffMemberId is passed in the request
                ForPlatform = request.AccessDetailDTO.ForPlatform,  // Assuming this is for the platform value 2 (mobile or web)
                BasicMenus = basicMenuDTOs
            };

            // Construct the API response
            var apiResponse = new ApiResponse<AccessDetailResponseDTO>
            {
                IsSucceeded = ConstantValues.isSucceeded,  // Indicating the operation succeeded
                Message = "Menus fetched successfully.",
                Data = accessDetailResponse // Bind the AccessDetailResponseDTO
            };

            // Log the successful operation
           // logger?.LogInformation("Access detail response created successfully for StaffMemberId: {StaffMemberId}, Platform: {ForPlatform}",
             //   request.AccessDetailDTO.StaffMemberId, 2);

            // Return the response (example for a Web API method)
            return apiResponse;
 



        }
    
        #endregion
}
}
