namespace EngiFlow.Application.Ecos.Dtos;

/// <summary>
/// Response DTO for a one-based page of application results.
/// </summary>
/// <typeparam name="TItem">The item DTO contained in the page.</typeparam>
/// <param name="Items">The page items.</param>
/// <param name="PageNumber">The one-based page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="TotalCount">The total number of items available.</param>
public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    /// <summary>
    /// Gets the total number of pages represented by the result set.
    /// </summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1 && TotalPages > 0;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
