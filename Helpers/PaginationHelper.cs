using Microsoft.EntityFrameworkCore;

namespace RestaurantMS.Helpers;

public class PagedResult<T>
{
    public List<T> Items      { get; set; } = new();
    public int TotalCount     { get; set; }
    public int PageNumber     { get; set; }
    public int PageSize       { get; set; }
    public int TotalPages     => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasNext       => PageNumber < TotalPages;
    public bool HasPrev       => PageNumber > 1;
}

public static class PaginationHelper
{
    /// <summary>
    /// Materialises a paged slice of an <see cref="IQueryable{T}"/> asynchronously.
    /// PageNumber is 1-based. PageSize is clamped to [1, 200].
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize   = Math.Clamp(pageSize, 1, 200);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items      = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize   = pageSize,
        };
    }
}
