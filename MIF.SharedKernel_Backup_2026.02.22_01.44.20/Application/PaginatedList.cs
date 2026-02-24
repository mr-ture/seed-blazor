using Microsoft.EntityFrameworkCore;

namespace MIF.SharedKernel.Application;

/// <summary>
/// Lightweight helper for returning paged results from a query.
/// It keeps the items for the current page plus metadata needed for UI paging controls.
/// </summary>
public class PaginatedList<T>
{
    /// <summary>
    /// Items that belong to the current page.
    /// </summary>
    public List<T> Items { get; }
    /// <summary>
    /// 1-based page index currently being returned.
    /// </summary>
    public int PageNumber { get; }
    /// <summary>
    /// Total number of pages based on <paramref name="pageSize"/> and total count.
    /// </summary>
    public int TotalPages { get; }
    /// <summary>
    /// Total number of items in the full query (all pages).
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Creates a paginated list from an already materialized page of items.
    /// </summary>
    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    /// <summary>
    /// True when there is at least one page before the current page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>
    /// True when there is at least one page after the current page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Executes paging over an <see cref="IQueryable{T}"/> source using database-side
    /// skip/take to avoid loading the entire result set into memory.
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
