// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves tender main categories.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Category;
using skolenza.application.Features.CategoryCmd.Command;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using skolenza.domain.Entity; using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.application.Features.CategoryCmd.Command
{
    #region Command

    /// <summary>
    /// Represents the request to retrieve tender main categories.
    /// </summary>
    public class GetTenderMainCategoryCommand : IRequest<ApiResponse<List<TenderCategoryResponseDTO>>>
    {
        public TenderCategoryRequestDTO TenderCategoryRequestDTO { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="GetTenderMainCategoryCommand"/> class.
        /// </summary>
        /// <param name="tenderCategoryRequestDTO">The tender category request data.</param>
        public GetTenderMainCategoryCommand(TenderCategoryRequestDTO tenderCategoryRequestDTO)
        {
            TenderCategoryRequestDTO = tenderCategoryRequestDTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.CategoryCmd.Handlers
{

    /// <summary>
    /// Handles the request to retrieve tender main categories.
    /// </summary>
    public class GetTenderMainCategoryRequestHandler : IRequestHandler<GetTenderMainCategoryCommand, ApiResponse<List<TenderCategoryResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetTenderMainCategoryRequestHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTenderMainCategoryRequestHandler"/> class.
        /// </summary>
        /// <param name="mapper">The object mapper.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="logger">The logger.</param>
        public GetTenderMainCategoryRequestHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetTenderMainCategoryRequestHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        //public GetTenderMainCategoryRequestHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetTenderMainCategoryRequestHandler> logger)
        //{
        //    _mapper = mapper;
        //    _unitOfWork = unitOfWork;
        //    _logger = logger;
        //}

        #endregion

        #region Handler

        /// <summary>
        /// Handles the tender main category request.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">A token to observe while handling the request.</param>
        /// <returns>The tender main category response.</returns>
        public Task<ApiResponse<List<TenderCategoryResponseDTO>>> Handle(GetTenderMainCategoryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
