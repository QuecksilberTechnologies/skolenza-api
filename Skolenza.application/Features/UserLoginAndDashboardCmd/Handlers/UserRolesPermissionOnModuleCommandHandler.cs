// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to User Roles Permission On Module.
// ================================================================

using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces;
using FluentValidation;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to User Roles Permission On Module.
    /// </summary>
public class UserRolesPermissionOnModuleCommand : IRequest<ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>>
    {
        //till completed
        public AccessDetailRequestDTO AccessDetailDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRolesPermissionOnModuleCommand"/> class.
        /// </summary>
        public UserRolesPermissionOnModuleCommand(AccessDetailRequestDTO accessRequestDTO)
        {
            AccessDetailDTO = accessRequestDTO;
        }

    
    }

    #endregion
}

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    /// <summary>
    /// Handles the request to User Roles Permission On Module.
    /// </summary>
public class UserRolesPermissionOnModuleCommandHandler : IRequestHandler<UserRolesPermissionOnModuleCommand, ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>>
    {
        #region Fields

        private readonly IUserRolesPermissionOnModuleRepository userRolesPermissionOnModuleRepository; // Add repository here
        private readonly IMapper mapper;
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRolesPermissionOnModuleCommandHandler"/> class.
        /// </summary>


        public UserRolesPermissionOnModuleCommandHandler(IUserRolesPermissionOnModuleRepository userRolesPermissionOnModuleRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this.userRolesPermissionOnModuleRepository = userRolesPermissionOnModuleRepository; // Initialize repository
            this.mapper = mapper;
            this.unitOfWork = unitOfWork;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied UserRolesPermissionOnModuleCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>> Handle(UserRolesPermissionOnModuleCommand? request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate the request
                if (request == null || request.AccessDetailDTO == null)
                {
                    return new ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request or missing AccessDetailDTO."
                    };
                }

                // Fetch the basic menus for the given staffMember type and platform
                IEnumerable<UserRolesPermissionOnModuleDTO> userRolesPermissionOnModuleDTOs = await userRolesPermissionOnModuleRepository
                    .GetModuleListAndOperationByRollIdAsync(request.AccessDetailDTO.roleInfo.ToList(), request.AccessDetailDTO.ForPlatform);

                // Construct the API response
                var apiResponse = new ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>
                {
                    IsSucceeded = ConstantValues.isSucceeded,  // Indicating the operation succeeded
                    Message = "Menus fetched successfully.",
                    Data = userRolesPermissionOnModuleDTOs // Return the fetched data as IEnumerable
                };

                // Log the successful operation
                // logger?.LogInformation("Access detail response created successfully for StaffMemberId: {StaffMemberId}, Platform: {ForPlatform}",
                //   request.AccessDetailDTO.StaffMemberId, request.AccessDetailDTO.ForPlatform);

                return apiResponse;
            }
            catch (Exception ex)
            {
                // Log the error
                // logger?.LogError(ex, "An error occurred while processing the request.");

                // Return a failure response
                return new ApiResponse<IEnumerable<UserRolesPermissionOnModuleDTO>>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while processing the request. Please try again later."
                };
            }
        }
    
        #endregion
}
}
