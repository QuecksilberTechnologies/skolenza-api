// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Create Travel Mode Type.
// ================================================================

using skolenza.application.DTOs.Transport;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.TransportCmd.Commands;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces;

namespace skolenza.application.Features.TransportCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Create Travel Mode Type.
    /// </summary>
public class CreateTravelModeTypeCommand : IRequest<ApiResponse<List<GetAllTravelModeDTO>>>
    {

        public CreateTravelModeDTO createTravelModeDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTravelModeTypeCommand"/> class.
        /// </summary>

        public CreateTravelModeTypeCommand(CreateTravelModeDTO createClientTypeDTO)
        {
            this.createTravelModeDTO = createClientTypeDTO;
        }
   
    }

    #endregion
}

namespace skolenza.application.Features.TransportCmd.Handlers
{
    /// <summary>
    /// Handles the request to Create Travel Mode Type.
    /// </summary>
public class CreateTravelModeTypeCommandHandler :IRequestHandler<CreateTravelModeTypeCommand, ApiResponse<List<GetAllTravelModeDTO>>>
    {
        #region Fields

        private readonly ITravelRepository travelRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTravelModeTypeCommandHandler"/> class.
        /// </summary>


    public CreateTravelModeTypeCommandHandler(ITravelRepository travelRepository, IMapper mapper, IUnitOfWork unitOfWork)
    {
        this.travelRepository = travelRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied CreateTravelModeTypeCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>

    public async Task<ApiResponse<List<GetAllTravelModeDTO>>> Handle(CreateTravelModeTypeCommand request, CancellationToken cancellationToken)
    {
        try
        {
                 
            TravelMode travelMode = _mapper.Map<TravelMode>(request.createTravelModeDTO);
            List<TravelMode> travelModes = await travelRepository.CreateTravelTypeAsync(travelMode);

            if (travelModes == null || !travelModes.Any())
            {
                return new ApiResponse<List<GetAllTravelModeDTO>>
                {
                    IsSucceeded = false,
                    Message = "No Travel were created.",
                    Data = new List<GetAllTravelModeDTO>()
                };
            }

            List<GetAllTravelModeDTO> getAllTravelModeDTOs = _mapper.Map<List<GetAllTravelModeDTO>>(travelModes);

            return new ApiResponse<List<GetAllTravelModeDTO>>
            {
                IsSucceeded = true,
                Message = "Travel created successfully",
                Data = getAllTravelModeDTOs
            };
        }
        catch (Exception ex)
        {
            //  _logger.LogError(ex, "Error occurred while creating role.");
            return new ApiResponse<List<GetAllTravelModeDTO>>

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
