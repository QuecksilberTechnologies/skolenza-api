// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Update Client Type.
// ================================================================

using skolenza.application.DTOs.Client;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.ClientCmd.Commands;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces;

namespace skolenza.application.Features.ClientCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Update Client Type.
    /// </summary>
public class UpdateClientTypeCommand : IRequest<ApiResponse<List<GetClientTypeDTO>>>
    {

        public UpdateClientTypeDTO updateClientTypeCommand { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateClientTypeCommand"/> class.
        /// </summary>

        public UpdateClientTypeCommand(UpdateClientTypeDTO updateClientTypeCommand)
        {
            this.updateClientTypeCommand = updateClientTypeCommand;
        }
    }

    #endregion
}

namespace skolenza.application.Features.ClientCmd.Handlers
{
    /// <summary>
    /// Handles the request to Update Client Type.
    /// </summary>
public class UpdateClientTypeCommandHandler : IRequestHandler<UpdateClientTypeCommand, ApiResponse<List<GetClientTypeDTO>>>
    {
        #region Fields

        private readonly IClientRepository _ClientRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateClientTypeCommandHandler"/> class.
        /// </summary>


        public UpdateClientTypeCommandHandler(IClientRepository ClientRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _ClientRepository = ClientRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied UpdateClientTypeCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>

        public async Task<ApiResponse<List<GetClientTypeDTO>>> Handle(UpdateClientTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                ClientType ClienttypeEntity = _mapper.Map<ClientType>(request.updateClientTypeCommand);
                List<ClientType> ClientTypes = await _ClientRepository.UpdateClientTypeAsync(ClienttypeEntity);

                if (ClientTypes == null || !ClientTypes.Any())
                {
                    return new ApiResponse<List<GetClientTypeDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No Client were update.",
                        Data = new List<GetClientTypeDTO>()
                    };
                }

                List<GetClientTypeDTO> ClientTypeDTOs = _mapper.Map<List<GetClientTypeDTO>>(ClientTypes);

                return new ApiResponse<List<GetClientTypeDTO>>
                {
                    IsSucceeded = true,
                    Message = "Client update successfully",
                    Data = ClientTypeDTOs
                };
            }
            catch (Exception ex)
            {
                //  _logger.LogError(ex, "Error occurred while creating role.");
                return new ApiResponse<List<GetClientTypeDTO>>

                {
                    IsSucceeded = false,
                    Message = $"An error occurred: {ex.Message}",
                    Data = null
                };
            }
        }


    
        #endregion
}
}
