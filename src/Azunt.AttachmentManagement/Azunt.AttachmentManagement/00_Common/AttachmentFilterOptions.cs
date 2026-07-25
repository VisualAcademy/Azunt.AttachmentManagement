namespace Azunt.AttachmentManagement;

/// <summary>
/// Search, filter, sorting, and paging options for attachment records.
/// </summary>
public sealed class AttachmentFilterOptions
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 10;
    public string SearchQuery { get; set; } = string.Empty;
    public string SortOrder { get; set; } = string.Empty;
    public long? EmployeeId { get; set; }
    public long? VendorId { get; set; }
    public long? InvestigationId { get; set; }
    public bool ActiveOnly { get; set; }
}
