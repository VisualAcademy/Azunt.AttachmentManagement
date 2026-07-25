namespace Azunt.AttachmentManagement;

/// <summary>
/// A lightweight paged result that keeps the current items and total record count together.
/// </summary>
public readonly struct ArticleSet<T, TCount>
{
    public ArticleSet(IEnumerable<T> items, TCount totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public IEnumerable<T> Items { get; }
    public TCount TotalCount { get; }
}
