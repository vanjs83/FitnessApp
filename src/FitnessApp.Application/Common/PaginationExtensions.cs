using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Common;

/// <summary>
/// Skip/Take pagination helpers shared by every list query. Page size is capped
/// at <see cref="MaxPageSize"/> so a client can never pull more than one screen.
/// </summary>
public static class PaginationExtensions
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 25;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return (page, pageSize);
    }

    /// <summary>Pages a database query, translating Skip/Take into SQL.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<T>.Create(items, page, pageSize, total);
    }

    /// <summary>Pages an already-materialized collection (e.g. results from a non-queryable service).</summary>
    public static PagedResult<T> ToPagedResult<T>(this IReadOnlyList<T> source, int page, int pageSize)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return PagedResult<T>.Create(items, page, pageSize, source.Count);
    }
}
