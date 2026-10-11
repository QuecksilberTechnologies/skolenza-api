// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves all company information.
// ================================================================

using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries
{
    #region Query

    /// <summary>
    /// Represents the request to retrieve all company information.
    /// </summary>
    public class GetAllCompanyQuery : IRequest<string>
    {
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles the request to retrieve all company information.
    /// </summary>
    public class GetAllCompanyQueryHandler : IRequestHandler<GetAllCompanyQuery, string>
    {
        /// <summary>
        /// Handles the company retrieval request.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">A token to observe while handling the request.</param>
        /// <returns>The company information response.</returns>
        public Task<string> Handle(GetAllCompanyQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
