// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Candidate Registration.
// ================================================================

using skolenza.application.DTOs.Registration;
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
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Candidate Registration.
    /// </summary>
public class CandidateRegistrationCommand : IRequest<ApiResponse<CandidateResponseDTO>>
    {
        public CandidateRequestDTO RequestCandidateRegistrationDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CandidateRegistrationCommand"/> class.
        /// </summary>


        public CandidateRegistrationCommand(CandidateRequestDTO candidateRegistrationRequestDTO)
        {
            RequestCandidateRegistrationDTO = candidateRegistrationRequestDTO;
        }



    }

    #endregion
}

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    /// <summary>
    /// Handles the request to Candidate Registration.
    /// </summary>
public class CandidateRegistrationCommandHandler : IRequestHandler<CandidateRegistrationCommand, ApiResponse<CandidateResponseDTO>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CandidateRegistrationCommandHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CandidateRegistrationCommandHandler"/> class.
        /// </summary>


        public CandidateRegistrationCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<CandidateRegistrationCommandHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied CandidateRegistrationCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<CandidateResponseDTO>> Handle(CandidateRegistrationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                CandidateResponseDTO candidateResponse = new CandidateResponseDTO();

                // Validate the request
                if (request == null || request.RequestCandidateRegistrationDTO == null)
                {
                    return new ApiResponse<CandidateResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request or missing CandidateRegistrationDTO."
                    };
                }

                CandidateRequestDTO candidateResponseDTO = _mapper.Map<CandidateRequestDTO>(request.RequestCandidateRegistrationDTO);

                // Check for duplicate data
                var isBlacklistedOrDuplicate = await _unitOfWork.CandidatesRegistrationRepository.IsEmailPANAdharPhoneExistsAsync(candidateResponseDTO);
                if (isBlacklistedOrDuplicate)
                {
                    return new ApiResponse<CandidateResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Data already exists."
                    };
                }

   

                try
                {
                    List<int> skillSetList = candidateResponseDTO.SkillSet.Split(',').Select(int.Parse).ToList();
                    Candidate cc = _mapper.Map<Candidate>(request.RequestCandidateRegistrationDTO);

                    // 🔹 Step 1: Save Candidate
                    var id = await _unitOfWork.CandidatesRegistrationRepository.AddCandidateAsync(cc);

                    // 🔹 Step 2: Save Candidate Skills
                    List<CandidateCategorySkill> candidateSkillsList = skillSetList.Select(skill => new CandidateCategorySkill
                    {
                        CandidateId = id,
                        CategoryId = skill,
                        AddedDateTime = DateTime.Now,
                        IsActive = true
                    }).ToList();

                    var numberOfRecordInserted = await _unitOfWork.CandidateCategorySkillRepository.AddSkillsAsync(candidateSkillsList);

                    // 🔹 Step 3: Commit Transaction
                    await _unitOfWork.CommitTransactionAsync();

                    return new ApiResponse<CandidateResponseDTO>
                    {
                        IsSucceeded = true,
                        Message = "Candidate registration successful.",
                        Data = new CandidateResponseDTO { Success = true, CandidateId = id }
                    };
                }
                catch (Exception ex)
                {
                    // 🔹 Rollback if any step fails
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Transaction rolled back due to an error.");
                    return new ApiResponse<CandidateResponseDTO> { IsSucceeded = false, Message = "Transaction failed." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the registration request.");
                return new ApiResponse<CandidateResponseDTO> { IsSucceeded = false, Message = "An error occurred while processing the request." };
            }
        }



    
        #endregion
}
}
