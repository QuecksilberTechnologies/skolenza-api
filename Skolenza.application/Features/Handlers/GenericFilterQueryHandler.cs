// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines a generic filtered retrieval request.
// ================================================================

using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.application.Features.Queries
{
    #region Query

    /// <summary>
    /// Represents a generic request to retrieve entities matching a filter.
    /// </summary>
    /// <typeparam name="T">The entity type to retrieve.</typeparam>
    public class GenericFilterQuery<T> : IRequest<List<T>> where T : class
    {
        public Expression<Func<T, bool>> Filter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericFilterQuery{T}"/> class.
        /// </summary>
        /// <param name="filter">The entity filter.</param>
        public GenericFilterQuery(Expression<Func<T, bool>> filter)
        {
            Filter = filter;
        }
    }

    #endregion
}

// Handler implementation is intentionally pending.
// Request was relocated here as part of CQRS structural consolidation.
