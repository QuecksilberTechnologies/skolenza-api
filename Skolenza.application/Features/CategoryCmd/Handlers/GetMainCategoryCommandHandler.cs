using AutoMapper;
using skolenza.application.DTOs.Category;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.CategoryCmd.Handlers
{
    public class GetMainCategoryCommand : IRequest<ApiResponse<List<CategoryResponseDTO>>>
    {
        public CategoryRequestDTO CategoryRequestDTO { get; set; }


        public GetMainCategoryCommand(CategoryRequestDTO categoryRequestDTO)
        {
            CategoryRequestDTO = categoryRequestDTO;
        }

    }
        public class GetMainCategoryCommandHandler : IRequestHandler<GetMainCategoryCommand, ApiResponse<List<CategoryResponseDTO>>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetMainCategoryCommandHandler> _logger;

        public GetMainCategoryCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetMainCategoryCommandHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<List<CategoryResponseDTO>>> Handle(GetMainCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                int Id = Convert.ToInt32(request.CategoryRequestDTO.CategoryId);

                // Validate the request
                if (request == null || request.CategoryRequestDTO == null || Id == 0)
                {
                    return new ApiResponse<List<CategoryResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request. Category ID is required.",
                        Data = null
                    };
                }

                var categoryRequestDto = request.CategoryRequestDTO;

                // Validate user authorization
                //if (!await _unitOfWork.UserLoginReopsitory.IsValidUserAsync(categoryRequestDto.Id))
                //{
                //    return new ApiResponse<List<CategoryResponseDTO>>
                //    {
                //        IsSuccecced = false,
                //        Message = "User is not authenticated or authorized to perform this action.",
                //        Data = null
                //    };
                //}

                // Fetch all main categories (where ParentCategoryId is NULL)
                var categories = await _unitOfWork.CategoryRepository.GetAllMainCategoriesAsync();

                // Map the domain model to the response DTO
                var categoryResponseDTOs = _mapper.Map<List<CategoryResponseDTO>>(categories);

                // Return a success response
                return new ApiResponse<List<CategoryResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Categories fetched successfully.",
                    Data = categoryResponseDTOs
                };
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "An error occurred while processing the category request.");

                // Return a failure response
                return new ApiResponse<List<CategoryResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while processing the category request.",
                    Data = null
                };
            }
        }


    }


}
