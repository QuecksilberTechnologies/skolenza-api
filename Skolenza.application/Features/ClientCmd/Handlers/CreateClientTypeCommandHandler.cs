using AutoMapper;
using skolenza.application.DTOs.Client;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.ClientCmd.Handlers
{
    public class CreateClientTypeCommand : IRequest<ApiResponse<List<GetClientTypeDTO>>>
    {

        public CreateClientTypeDTO createClientTypeDTO { get; set; }

        public CreateClientTypeCommand(CreateClientTypeDTO createClientTypeDTO)
        {
            this.createClientTypeDTO = createClientTypeDTO;
        }
    }
        public class CreateClientTypeCommandHandler : IRequestHandler<CreateClientTypeCommand, ApiResponse<List<GetClientTypeDTO>>>
        {
            private readonly IClientRepository _clienttypeRepository;
            private readonly IMapper _mapper;
            private readonly IUnitOfWork _unitOfWork;

            public CreateClientTypeCommandHandler(IClientRepository clientRepository, IMapper mapper, IUnitOfWork unitOfWork)
            {
                _clienttypeRepository = clientRepository;
                _mapper = mapper;
                _unitOfWork = unitOfWork;
            }


            public async Task<ApiResponse<List<GetClientTypeDTO>>> Handle(CreateClientTypeCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    ClientType clientType = _mapper.Map<ClientType>(request.createClientTypeDTO);
                    List<ClientType> clientTypes = await _clienttypeRepository.CreateClientTypeAsync(clientType);

                    if (clientTypes == null || !clientTypes.Any())
                    {
                        return new ApiResponse<List<GetClientTypeDTO>>
                        {
                            IsSucceeded = false,
                            Message = "No client were created.",
                            Data = new List<GetClientTypeDTO>()
                        };
                    }

                    List<GetClientTypeDTO> leaveTypeDTOs = _mapper.Map<List<GetClientTypeDTO>>(clientTypes);

                    return new ApiResponse<List<GetClientTypeDTO>>
                    {
                        IsSucceeded = true,
                        Message = "Client created successfully",
                        Data = leaveTypeDTOs
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
        }


    }
 
 