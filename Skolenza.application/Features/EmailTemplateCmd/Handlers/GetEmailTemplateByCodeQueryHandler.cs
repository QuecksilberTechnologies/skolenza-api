// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get Email Template By Code.
// ================================================================

using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using AutoMapper;
using skolenza.application.Features.EmailTemplateCmd.Queries;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.EmailTemplateCmd.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get Email Template By Code.
    /// </summary>
public class GetEmailTemplateByCodeQuery : IRequest<ApiResponse<EmailTemplateDTO>>
    {
        public string Code { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetEmailTemplateByCodeQuery"/> class.
        /// </summary>

        public GetEmailTemplateByCodeQuery(string code)
        {
            Code = code;
        }
    }

    #endregion
}

namespace skolenza.application.Features.EmailTemplateCmd.Handlers
{
    /// <summary>
    /// Handles the request to Get Email Template By Code.
    /// </summary>
public class GetEmailTemplateByCodeQueryHandler : IRequestHandler<GetEmailTemplateByCodeQuery, ApiResponse<EmailTemplateDTO>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetEmailTemplateByCodeQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetEmailTemplateByCodeQueryHandler"/> class.
        /// </summary>

        
        public GetEmailTemplateByCodeQueryHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<GetEmailTemplateByCodeQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetEmailTemplateByCodeQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<EmailTemplateDTO>> Handle(GetEmailTemplateByCodeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                EmailTemplate? templates = await _unitOfWork.EmailTemplateRepository
                    .GetTemplateByCodeAsync(request.Code, cancellationToken);

                if (templates == null)
                {
                    _logger.LogWarning("No email templates found for code: {Code}", request.Code);

                    return new ApiResponse<EmailTemplateDTO>
                    {
                        IsSucceeded = false,
                        Message = $"No templates found for code: {request.Code}",
                        Data = null
                    };
                }

                var templateDTOs = _mapper.Map<EmailTemplateDTO>(templates);

                _logger.LogInformation("Successfully retrieved {Count} email templates for code: {Code}", templateDTOs, request.Code);

                return new ApiResponse<EmailTemplateDTO>
                {
                    IsSucceeded = true,
                    Message = "Email templates fetched successfully.",
                    Data = templateDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching email templates for code: {Code}", request.Code);

                return new ApiResponse<EmailTemplateDTO>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while fetching email templates.",
                    Data = null
                };
            }
        }
    
        #endregion
}
}
