using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.AttachmentManagement;

public sealed class AttachmentRepositoryAdoNet : IAttachmentRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<AttachmentRepositoryAdoNet> _logger;

    public AttachmentRepositoryAdoNet(
        string defaultConnectionString,
        ILogger<AttachmentRepositoryAdoNet> logger)
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
        var now = DateTimeOffset.UtcNow;
        model.Active ??= true;
        model.CreatedAt ??= now;
        model.DateCreated ??= model.CreatedAt;

        const string sql = """
            INSERT INTO dbo.Attachments
            (
                Active, DateCreated, CreatedAt, CreatedBy,
                EmployeeID, VendorID, InvestigationID,
                FileName, Discriminator, Category, Notes
            )
            OUTPUT INSERTED.ID
            VALUES
            (
                @Active, @DateCreated, @CreatedAt, @CreatedBy,
                @EmployeeID, @VendorID, @InvestigationID,
                @FileName, @Discriminator, @Category, @Notes
            );
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, model);
        await connection.OpenAsync();
        model.Id = Convert.ToInt64(await command.ExecuteScalarAsync());
        _logger.LogInformation("Attachment {AttachmentId} created through ADO.NET.", model.Id);
        return model;
    }

    public async Task<List<AttachmentRecord>> GetAllAsync(string? connectionString = null)
    {
        const string sql = """
            SELECT ID, Active, DateCreated, CreatedAt, CreatedBy,
                   EmployeeID, VendorID, InvestigationID, FileName,
                   Discriminator, Category, Notes
            FROM dbo.Attachments
            ORDER BY ID DESC;
            """;

        var result = new List<AttachmentRecord>();
        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(Read(reader));
        }

        return result;
    }

    public async Task<AttachmentRecord?> GetByIdAsync(long id, string? connectionString = null)
    {
        const string sql = """
            SELECT ID, Active, DateCreated, CreatedAt, CreatedBy,
                   EmployeeID, VendorID, InvestigationID, FileName,
                   Discriminator, Category, Notes
            FROM dbo.Attachments
            WHERE ID = @ID;
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = id;
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
    }

    public async Task<bool> UpdateAsync(AttachmentRecord model, string? connectionString = null)
    {
        const string sql = """
            UPDATE dbo.Attachments SET
                Active = @Active,
                DateCreated = @DateCreated,
                CreatedAt = @CreatedAt,
                CreatedBy = @CreatedBy,
                EmployeeID = @EmployeeID,
                VendorID = @VendorID,
                InvestigationID = @InvestigationID,
                FileName = @FileName,
                Discriminator = @Discriminator,
                Category = @Category,
                Notes = @Notes
            WHERE ID = @ID;
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, model);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = model.Id;
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync() > 0;
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
                InvestigationID = @InvestigationID,
                Category = @Category,
                Notes = @Notes,
                CreatedBy = COALESCE(@ModifiedBy, CreatedBy)
            WHERE ID = @ID;
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@InvestigationID", SqlDbType.BigInt).Value = DbValue(investigationId);
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DbValue(category);
        command.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value = DbValue(notes);
        command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 70).Value = DbValue(modifiedBy);
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id, string? connectionString = null)
    {
        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand("DELETE FROM dbo.Attachments WHERE ID = @ID;", connection);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = id;
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<AttachmentRecord>> GetByInvestigationIdAsync(
        long investigationId,
        string? connectionString = null)
    {
        var all = await GetAllAsync(connectionString);
        return all.Where(m => m.InvestigationId == investigationId).ToList();
    }

    public async Task<ArticleSet<AttachmentRecord, long>> GetPagedAsync(
        AttachmentFilterOptions options,
        string? connectionString = null)
    {
        var query = (await GetAllAsync(connectionString)).AsEnumerable();

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
            query = query.Where(m => m.EffectiveActive);
        }
        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            var keyword = options.SearchQuery.Trim();
            query = query.Where(m =>
                Contains(m.FileName, keyword) ||
                Contains(m.Category, keyword) ||
                Contains(m.Notes, keyword) ||
                Contains(m.CreatedBy, keyword) ||
                Contains(m.Discriminator, keyword));
        }

        query = options.SortOrder switch
        {
            "FileName" => query.OrderBy(m => m.FileName),
            "FileNameDesc" => query.OrderByDescending(m => m.FileName),
            "Category" => query.OrderBy(m => m.Category),
            "CategoryDesc" => query.OrderByDescending(m => m.Category),
            "CreatedAt" => query.OrderBy(m => m.EffectiveCreatedAt),
            "CreatedAtDesc" => query.OrderByDescending(m => m.EffectiveCreatedAt),
            "InvestigationId" => query.OrderBy(m => m.InvestigationId),
            "InvestigationIdDesc" => query.OrderByDescending(m => m.InvestigationId),
            _ => query.OrderByDescending(m => m.Id)
        };

        var materialized = query.ToList();
        var pageSize = Math.Clamp(options.PageSize, 1, 200);
        var pageIndex = Math.Max(0, options.PageIndex);
        var page = materialized.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return new ArticleSet<AttachmentRecord, long>(page, materialized.Count);
    }

    private static bool Contains(string? value, string keyword)
        => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static void AddParameters(SqlCommand command, AttachmentRecord model)
    {
        command.Parameters.Add("@Active", SqlDbType.Bit).Value = DbValue(model.Active);
        command.Parameters.Add("@DateCreated", SqlDbType.DateTimeOffset).Value = DbValue(model.DateCreated);
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTimeOffset).Value = DbValue(model.CreatedAt);
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 70).Value = DbValue(model.CreatedBy);
        command.Parameters.Add("@EmployeeID", SqlDbType.BigInt).Value = DbValue(model.EmployeeId);
        command.Parameters.Add("@VendorID", SqlDbType.BigInt).Value = DbValue(model.VendorId);
        command.Parameters.Add("@InvestigationID", SqlDbType.BigInt).Value = DbValue(model.InvestigationId);
        command.Parameters.Add("@FileName", SqlDbType.NVarChar, -1).Value = DbValue(model.FileName);
        command.Parameters.Add("@Discriminator", SqlDbType.NVarChar, -1).Value = DbValue(model.Discriminator);
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DbValue(model.Category);
        command.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value = DbValue(model.Notes);
    }

    private static AttachmentRecord Read(SqlDataReader reader)
    {
        return new AttachmentRecord
        {
            Id = reader.GetInt64(0),
            Active = reader.IsDBNull(1) ? null : reader.GetBoolean(1),
            DateCreated = reader.IsDBNull(2) ? null : reader.GetDateTimeOffset(2),
            CreatedAt = reader.IsDBNull(3) ? null : reader.GetDateTimeOffset(3),
            CreatedBy = reader.IsDBNull(4) ? null : reader.GetString(4),
            EmployeeId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
            VendorId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            InvestigationId = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            FileName = reader.IsDBNull(8) ? null : reader.GetString(8),
            Discriminator = reader.IsDBNull(9) ? null : reader.GetString(9),
            Category = reader.IsDBNull(10) ? null : reader.GetString(10),
            Notes = reader.IsDBNull(11) ? null : reader.GetString(11)
        };
    }
}
