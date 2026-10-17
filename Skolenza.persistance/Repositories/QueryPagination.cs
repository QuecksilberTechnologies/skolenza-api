using Microsoft.EntityFrameworkCore;
using skolenza.application.DTOS.Pagination;

namespace skolenza.persistance.Repositories;

/// <summary>Pages an already scoped and deterministically ordered database query.</summary>
public static class QueryPagination
{
    #region Database Pagination

    /// <summary>
    /// Counts the filtered query before retrieving one page. Callers retain ownership
    /// of authorization, filtering and stable ordering. No completion metadata is calculated.
    /// </summary>
    public static async Task<PagedResponseDTO<T>> ToPageAsync<T>(
        this IQueryable<T> query,
        int pageNumber = PaginationDefaults.PageNumber,
        int pageSize = PaginationDefaults.PageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        pageNumber = pageNumber > 0 ? pageNumber : PaginationDefaults.PageNumber;
        pageSize = pageSize > 0 ? pageSize : PaginationDefaults.PageSize;
        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (long)(pageNumber - 1) * pageSize;
        var data = offset >= totalCount
            ? new List<T>()
            : await query.Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResponseDTO<T>(data, totalCount, pageNumber, pageSize)
        {
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    #endregion
}
