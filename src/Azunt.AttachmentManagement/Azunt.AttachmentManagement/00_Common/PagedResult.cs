namespace Azunt.AttachmentManagement;

/// <summary>
/// Represents one page of items together with the total number of matching records.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, long totalCount)
    {
        Items = items?.ToArray() ?? Array.Empty<T>();
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total number of records matching the current filter.
    /// </summary>
    public long TotalCount { get; }
}
