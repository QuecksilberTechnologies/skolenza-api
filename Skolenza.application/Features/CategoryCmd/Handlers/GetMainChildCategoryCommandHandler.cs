// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Processes the GetMainChildCategoryCommand use case.
// ================================================================

using skolenza.application.DTOs.Category;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using skolenza.application.Features.CategoryCmd.Command;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;

using MediatR;

namespace skolenza.application.Features.CategoryCmd.Command
{
    #region Command

    /// <summary>
    /// Represents the command request for Get Main Child Category.
    /// </summary>
public class GetMainChildCategoryCommand : IRequest<ApiResponse<List<CategoryResponseDTO>>>
    {
        public CategoryRequestDTO CategoryRequestDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMainChildCategoryCommand"/> class.
        /// </summary>


        public GetMainChildCategoryCommand(CategoryRequestDTO categoryRequestDTO)
        {
            CategoryRequestDTO = categoryRequestDTO;
        }
     

    }

    #endregion
}

namespace skolenza.application.Features.CategoryCmd.Handlers
{
    /// <summary>
    /// Handles the request for Get Main Child Category.
    /// </summary>
public class GetMainCategoryChildRequestCommandHandler : IRequestHandler<GetMainChildCategoryCommand, ApiResponse<List<CategoryResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetMainCategoryCommandHandler> _logger;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMainCategoryChildRequestCommandHandler"/> class.
        /// </summary>

        public GetMainCategoryChildRequestCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetMainCategoryCommandHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler

        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The response produced by handling the request.</returns>

        public async Task<ApiResponse<List<CategoryResponseDTO>>> Handle(GetMainChildCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {

                // Validate the request
                if (request == null || request.CategoryRequestDTO == null || request.CategoryRequestDTO.CategoryId == 0)
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

        //        fetch all main categories(where parentcategoryid is null)
                var categories = await _unitOfWork.CategoryRepository.GetAllChildCategoryByIdAsync(categoryRequestDto.CategoryId, categoryRequestDto.CategoryId);

            //    map the domain model to the response dto
                var categoryresponsedtos = _mapper.Map<List<CategoryResponseDTO>>(categories);

              
                return new ApiResponse<List<CategoryResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Categories fetched successfully.",
                    Data = categoryresponsedtos
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

       
    
        #endregion
}
}
