using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.AttachmentManagement;

public sealed class AttachmentRepositoryDapper : IAttachmentRepository
{
    private const string Columns = """
        ID AS Id, Active, DateCreated, CreatedAt, CreatedBy,
        ModifiedAt, ModifiedBy,
        EmployeeID AS EmployeeId, VendorID AS VendorId,
        InvestigationID AS InvestigationId, FileName,
        Discriminator, Category, Notes
        """;

    private readonly string _defaultConnectionString;
    private readonly ILogger<AttachmentRepositoryDapper> _logger;

    public AttachmentRepositoryDapper(
        string defaultConnectionString,
        ILogger<AttachmentRepositoryDapper> logger)
    {
        _defaultConnectionString = string.IsNullOrWhiteSpace(defaultConnectionString)
            ? throw new ArgumentException("Connection string is required.", nameof(defaultConnectionString))
            : defaultConnectionString;
        _logger = logger;
    }

    private SqlConnection CreateConnection(string? connectionString)
        => new(connectionString ?? _defaultConnectionString);

    public async Task<AttachmentRecord> AddAsync(
        AttachmentRecord model,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var now = DateTimeOffset.UtcNow;
        model.Active ??= true;
        model.CreatedAt ??= now;
        model.DateCreated ??= model.CreatedAt;

        const string sql = """
            INSERT INTO dbo.Attachments
            (
                Active, DateCreated, CreatedAt, CreatedBy,
                ModifiedAt, ModifiedBy,
                EmployeeID, VendorID, InvestigationID,
                FileName, Discriminator, Category, Notes
            )
            OUTPUT INSERTED.ID
            VALUES
            (
                @Active, @DateCreated, @CreatedAt, @CreatedBy,
                @ModifiedAt, @ModifiedBy,
                @EmployeeId, @VendorId, @InvestigationId,
                @FileName, @Discriminator, @Category, @Notes
            );
            """;

        await using var connection = CreateConnection(connectionString);
        model.Id = await connection.ExecuteScalarAsync<long>(sql, model);
        _logger.LogInformation("Attachment {AttachmentId} created through Dapper.", model.Id);
        return model;
    }

    public async Task<List<AttachmentRecord>> GetAllAsync(string? connectionString = null)
    {
        await using var connection = CreateConnection(connectionString);
        var items = await connection.QueryAsync<AttachmentRecord>(
            $"SELECT {Columns} FROM dbo.Attachments ORDER BY ID DESC;");
        return items.ToList();
    }

    public async Task<AttachmentRecord?> GetByIdAsync(long id, string? connectionString = null)
    {
        await using var connection = CreateConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<AttachmentRecord>(
            $"SELECT {Columns} FROM dbo.Attachments WHERE ID = @Id;",
            new { Id = id });
    }

    public async Task<bool> UpdateAsync(AttachmentRecord model, string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.ModifiedAt = DateTimeOffset.UtcNow;

        const string sql = """
            UPDATE dbo.Attachments SET
                Active = @Active,
                ModifiedAt = @ModifiedAt,
                ModifiedBy = COALESCE(NULLIF(@ModifiedBy, N''), ModifiedBy),
                EmployeeID = @EmployeeId,
                VendorID = @VendorId,
                InvestigationID = @InvestigationId,
                FileName = @FileName,
                Discriminator = @Discriminator,
                Category = @Category,
                Notes = @Notes
            WHERE ID = @Id;
            """;

        await using var connection = CreateConnection(connectionString);
        var changed = await connection.ExecuteAsync(sql, model) > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} updated through Dapper by {ModifiedBy}.",
                model.Id,
                model.ModifiedBy);
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
        const string sql = """
            UPDATE dbo.Attachments SET
                InvestigationID = @InvestigationId,
                Category = @Category,
                Notes = @Notes,
                ModifiedAt = @ModifiedAt,
                ModifiedBy = COALESCE(NULLIF(@ModifiedBy, N''), ModifiedBy)
            WHERE ID = @Id;
            """;

        await using var connection = CreateConnection(connectionString);
        var changed = await connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                InvestigationId = investigationId,
                Category = category,
                Notes = notes,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = modifiedBy
            }) > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} metadata updated through Dapper by {ModifiedBy}.",
                id,
                modifiedBy);
        }

        return changed;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        await using var connection = CreateConnection(connectionString);
        return await connection.ExecuteAsync(
            "DELETE FROM dbo.Attachments WHERE ID = @Id;",
            new { Id = id }) > 0;
    }

    public async Task<List<AttachmentRecord>> GetByInvestigationIdAsync(
        long investigationId,
        string? connectionString = null)
    {
        await using var connection = CreateConnection(connectionString);
        var items = await connection.QueryAsync<AttachmentRecord>(
            $"SELECT {Columns} FROM dbo.Attachments WHERE InvestigationID = @InvestigationId ORDER BY ID DESC;",
            new { InvestigationId = investigationId });
        return items.ToList();
    }

    public async Task<PagedResult<AttachmentRecord>> GetPagedAsync(
        AttachmentFilterOptions options,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (options.EmployeeId.HasValue)
        {
            where.Add("EmployeeID = @EmployeeId");
            parameters.Add("EmployeeId", options.EmployeeId.Value);
        }

        if (options.VendorId.HasValue)
        {
            where.Add("VendorID = @VendorId");
            parameters.Add("VendorId", options.VendorId.Value);
        }

        if (options.InvestigationId.HasValue)
        {
            where.Add("InvestigationID = @InvestigationId");
            parameters.Add("InvestigationId", options.InvestigationId.Value);
        }

        if (options.ActiveOnly)
        {
            where.Add("ISNULL(Active, 1) = 1");
        }

        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            var keyword = options.SearchQuery.Trim();
            parameters.Add("Search", $"%{keyword}%");

            var search = """
                (FileName LIKE @Search
                 OR Category LIKE @Search
                 OR Notes LIKE @Search
                 OR CreatedBy LIKE @Search
                 OR ModifiedBy LIKE @Search
                 OR Discriminator LIKE @Search)
                """;

            if (long.TryParse(keyword, out var numericId))
            {
                parameters.Add("NumericId", numericId);
                search = $"({search} OR ID = @NumericId OR EmployeeID = @NumericId OR VendorID = @NumericId OR InvestigationID = @NumericId)";
            }

            where.Add(search);
        }

        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        var orderSql = GetOrderBy(options.SortOrder);
        var pageIndex = Math.Max(0, options.PageIndex);
        var pageSize = Math.Clamp(options.PageSize, 1, 200);
        parameters.Add("Offset", pageIndex * pageSize);
        parameters.Add("PageSize", pageSize);

        var sql = $"""
            SELECT COUNT_BIG(1) FROM dbo.Attachments{whereSql};
            SELECT {Columns}
            FROM dbo.Attachments{whereSql}
            ORDER BY {orderSql}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = CreateConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(sql, parameters);
        var count = await grid.ReadSingleAsync<long>();
        var items = (await grid.ReadAsync<AttachmentRecord>()).ToList();
        return new PagedResult<AttachmentRecord>(items, count);
    }

    private static string GetOrderBy(string? sortOrder) => sortOrder switch
    {
        "FileName" => "FileName ASC",
        "FileNameDesc" => "FileName DESC",
        "Category" => "Category ASC",
        "CategoryDesc" => "Category DESC",
        "CreatedAt" => "COALESCE(CreatedAt, DateCreated) ASC",
        "CreatedAtDesc" => "COALESCE(CreatedAt, DateCreated) DESC",
        "ModifiedAt" => "ModifiedAt ASC",
        "ModifiedAtDesc" => "ModifiedAt DESC",
        "InvestigationId" => "InvestigationID ASC",
        "InvestigationIdDesc" => "InvestigationID DESC",
        "Active" => "Active ASC",
        "ActiveDesc" => "Active DESC",
        _ => "ID DESC"
    };
}
