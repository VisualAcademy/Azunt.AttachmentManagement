using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azunt.AttachmentManagement;

public sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly AttachmentAppDbContextFactory _factory;
    private readonly ILogger<AttachmentRepository> _logger;

    public AttachmentRepository(
        AttachmentAppDbContextFactory factory,
        ILogger<AttachmentRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    private AttachmentAppDbContext CreateContext(string? connectionString)
        => _factory.CreateDbContext(connectionString);

    public async Task<AttachmentRecord> AddAsync(
        AttachmentRecord model,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var context = CreateContext(connectionString);

        var now = DateTimeOffset.UtcNow;
        model.Active ??= true;
        model.CreatedAt ??= now;
        model.DateCreated ??= model.CreatedAt;

        context.Attachments.Add(model);
        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Attachment {AttachmentId} created for Employee {EmployeeId}, Vendor {VendorId}, Investigation {InvestigationId}.",
            model.Id,
            model.EmployeeId,
            model.VendorId,
            model.InvestigationId);

        return model;
    }

    public async Task<List<AttachmentRecord>> GetAllAsync(string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        return await context.Attachments
            .AsNoTracking()
            .OrderByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<AttachmentRecord?> GetByIdAsync(long id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        return await context.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> UpdateAsync(
        AttachmentRecord model,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        await using var context = CreateContext(connectionString);
        var entity = await context.Attachments.SingleOrDefaultAsync(m => m.Id == model.Id);

        if (entity is null)
        {
            return false;
        }

        entity.Active = model.Active;
        entity.EmployeeId = model.EmployeeId;
        entity.VendorId = model.VendorId;
        entity.InvestigationId = model.InvestigationId;
        entity.FileName = model.FileName;
        entity.Discriminator = model.Discriminator;
        entity.Category = model.Category;
        entity.Notes = model.Notes;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedBy = string.IsNullOrWhiteSpace(model.ModifiedBy)
            ? entity.ModifiedBy
            : model.ModifiedBy;

        var changed = await context.SaveChangesAsync() > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} updated by {ModifiedBy}.",
                model.Id,
                entity.ModifiedBy);
        }

        return changed;
    }

    public async Task<bool> UpdateMetadataAsync(
        long id,
        long? investigationId,
        string? category,
        string? notes,
        string? modifiedBy,
        string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        var entity = await context.Attachments.SingleOrDefaultAsync(m => m.Id == id);

        if (entity is null)
        {
            return false;
        }

        entity.InvestigationId = investigationId;
        entity.Category = category;
        entity.Notes = notes;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedBy = string.IsNullOrWhiteSpace(modifiedBy)
            ? entity.ModifiedBy
            : modifiedBy;

        var changed = await context.SaveChangesAsync() > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} metadata updated by {ModifiedBy}.",
                id,
                entity.ModifiedBy);
        }

        return changed;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        var entity = await context.Attachments.SingleOrDefaultAsync(m => m.Id == id);

        if (entity is null)
        {
            return false;
        }

        context.Attachments.Remove(entity);
        var changed = await context.SaveChangesAsync() > 0;

        if (changed)
        {
            _logger.LogInformation("Attachment {AttachmentId} deleted.", id);
        }

        return changed;
    }

    public async Task<List<AttachmentRecord>> GetByInvestigationIdAsync(
        long investigationId,
        string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        return await context.Attachments
            .AsNoTracking()
            .Where(m => m.InvestigationId == investigationId)
            .OrderByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<PagedResult<AttachmentRecord>> GetPagedAsync(
        AttachmentFilterOptions options,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        await using var context = CreateContext(connectionString);
        var query = context.Attachments.AsNoTracking().AsQueryable();

        query = ApplyFilters(query, options);
        query = ApplySort(query, options.SortOrder);

        var totalCount = await query.LongCountAsync();
        var pageIndex = Math.Max(0, options.PageIndex);
        var pageSize = Math.Clamp(options.PageSize, 1, 200);

        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AttachmentRecord>(items, totalCount);
    }

    private static IQueryable<AttachmentRecord> ApplyFilters(
        IQueryable<AttachmentRecord> query,
        AttachmentFilterOptions options)
    {
        if (options.EmployeeId.HasValue)
        {
            query = query.Where(m => m.EmployeeId == options.EmployeeId.Value);
        }

        if (options.VendorId.HasValue)
        {
            query = query.Where(m => m.VendorId == options.VendorId.Value);
        }

        if (options.InvestigationId.HasValue)
        {
            query = query.Where(m => m.InvestigationId == options.InvestigationId.Value);
        }

        if (options.ActiveOnly)
        {
            query = query.Where(m => m.Active != false);
        }

        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            var keyword = options.SearchQuery.Trim();
            var numeric = long.TryParse(keyword, out var numericId);

            query = query.Where(m =>
                (m.FileName != null && m.FileName.Contains(keyword)) ||
                (m.Category != null && m.Category.Contains(keyword)) ||
                (m.Notes != null && m.Notes.Contains(keyword)) ||
                (m.CreatedBy != null && m.CreatedBy.Contains(keyword)) ||
                (m.ModifiedBy != null && m.ModifiedBy.Contains(keyword)) ||
                (m.Discriminator != null && m.Discriminator.Contains(keyword)) ||
                (numeric && (m.Id == numericId ||
                             m.EmployeeId == numericId ||
                             m.VendorId == numericId ||
                             m.InvestigationId == numericId)));
        }

        return query;
    }

    private static IQueryable<AttachmentRecord> ApplySort(
        IQueryable<AttachmentRecord> query,
        string? sortOrder)
    {
        return sortOrder switch
        {
            "FileName" => query.OrderBy(m => m.FileName),
            "FileNameDesc" => query.OrderByDescending(m => m.FileName),
            "Category" => query.OrderBy(m => m.Category),
            "CategoryDesc" => query.OrderByDescending(m => m.Category),
            "CreatedAt" => query.OrderBy(m => m.CreatedAt ?? m.DateCreated),
            "CreatedAtDesc" => query.OrderByDescending(m => m.CreatedAt ?? m.DateCreated),
            "ModifiedAt" => query.OrderBy(m => m.ModifiedAt),
            "ModifiedAtDesc" => query.OrderByDescending(m => m.ModifiedAt),
            "InvestigationId" => query.OrderBy(m => m.InvestigationId),
            "InvestigationIdDesc" => query.OrderByDescending(m => m.InvestigationId),
            "Active" => query.OrderBy(m => m.Active),
            "ActiveDesc" => query.OrderByDescending(m => m.Active),
            _ => query.OrderByDescending(m => m.Id)
        };
    }
}
