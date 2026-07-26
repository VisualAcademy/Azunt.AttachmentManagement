namespace Azunt.AttachmentManagement;

public interface IAttachmentRepository
{
    Task<AttachmentRecord> AddAsync(AttachmentRecord model, string? connectionString = null);
    Task<List<AttachmentRecord>> GetAllAsync(string? connectionString = null);
    Task<AttachmentRecord?> GetByIdAsync(long id, string? connectionString = null);
    Task<bool> UpdateAsync(AttachmentRecord model, string? connectionString = null);
    Task<bool> UpdateMetadataAsync(
        long id,
        long? investigationId,
        string? category,
        string? notes,
        string? modifiedBy,
        string? connectionString = null);
    Task<bool> DeleteAsync(long id, string? connectionString = null);
    Task<List<AttachmentRecord>> GetByInvestigationIdAsync(long investigationId, string? connectionString = null);
    Task<PagedResult<AttachmentRecord>> GetPagedAsync(
        AttachmentFilterOptions options,
        string? connectionString = null);
}
