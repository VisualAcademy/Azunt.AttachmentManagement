using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.AttachmentManagement;

public sealed class AttachmentRepositoryAdoNet : IAttachmentRepository
{
    private const string Columns = """
        ID, Active, DateCreated, CreatedAt, CreatedBy,
        ModifiedAt, ModifiedBy,
        EmployeeID, VendorID, InvestigationID, FileName,
        Discriminator, Category, Notes
        """;

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
        var result = new List<AttachmentRecord>();
        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(
            $"SELECT {Columns} FROM dbo.Attachments ORDER BY ID DESC;",
            connection);

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
        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(
            $"SELECT {Columns} FROM dbo.Attachments WHERE ID = @ID;",
            connection);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
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
        var changed = await command.ExecuteNonQueryAsync() > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} updated through ADO.NET by {ModifiedBy}.",
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
                InvestigationID = @InvestigationID,
                Category = @Category,
                Notes = @Notes,
                ModifiedAt = @ModifiedAt,
                ModifiedBy = COALESCE(NULLIF(@ModifiedBy, N''), ModifiedBy)
            WHERE ID = @ID;
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ID", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@InvestigationID", SqlDbType.BigInt).Value = DbValue(investigationId);
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DbValue(category);
        command.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value = DbValue(notes);
        command.Parameters.Add("@ModifiedAt", SqlDbType.DateTimeOffset).Value = DateTimeOffset.UtcNow;
        command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 70).Value = DbValue(modifiedBy);

        await connection.OpenAsync();
        var changed = await command.ExecuteNonQueryAsync() > 0;

        if (changed)
        {
            _logger.LogInformation(
                "Attachment {AttachmentId} metadata updated through ADO.NET by {ModifiedBy}.",
                id,
                modifiedBy);
        }

        return changed;
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
        var result = new List<AttachmentRecord>();
        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(
            $"SELECT {Columns} FROM dbo.Attachments WHERE InvestigationID = @InvestigationID ORDER BY ID DESC;",
            connection);
        command.Parameters.Add("@InvestigationID", SqlDbType.BigInt).Value = investigationId;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(Read(reader));
        }

        return result;
    }

    public async Task<PagedResult<AttachmentRecord>> GetPagedAsync(
        AttachmentFilterOptions options,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var where = new List<string>();
        var parameterValues = new List<Action<SqlParameterCollection>>();

        if (options.EmployeeId.HasValue)
        {
            where.Add("EmployeeID = @EmployeeID");
            parameterValues.Add(parameters =>
                parameters.Add("@EmployeeID", SqlDbType.BigInt).Value = options.EmployeeId.Value);
        }

        if (options.VendorId.HasValue)
        {
            where.Add("VendorID = @VendorID");
            parameterValues.Add(parameters =>
                parameters.Add("@VendorID", SqlDbType.BigInt).Value = options.VendorId.Value);
        }

        if (options.InvestigationId.HasValue)
        {
            where.Add("InvestigationID = @InvestigationID");
            parameterValues.Add(parameters =>
                parameters.Add("@InvestigationID", SqlDbType.BigInt).Value = options.InvestigationId.Value);
        }

        if (options.ActiveOnly)
        {
            where.Add("ISNULL(Active, 1) = 1");
        }

        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            var keyword = options.SearchQuery.Trim();
            var search = """
                (FileName LIKE @Search
                 OR Category LIKE @Search
                 OR Notes LIKE @Search
                 OR CreatedBy LIKE @Search
                 OR ModifiedBy LIKE @Search
                 OR Discriminator LIKE @Search)
                """;

            parameterValues.Add(parameters =>
                parameters.Add("@Search", SqlDbType.NVarChar, -1).Value = $"%{keyword}%");

            if (long.TryParse(keyword, out var numericId))
            {
                search = $"({search} OR ID = @NumericID OR EmployeeID = @NumericID OR VendorID = @NumericID OR InvestigationID = @NumericID)";
                parameterValues.Add(parameters =>
                    parameters.Add("@NumericID", SqlDbType.BigInt).Value = numericId);
            }

            where.Add(search);
        }

        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);
        var orderBy = GetOrderBy(options.SortOrder);
        var pageSize = Math.Clamp(options.PageSize, 1, 200);
        var pageIndex = Math.Max(0, options.PageIndex);

        var sql = $"""
            SELECT COUNT_BIG(1)
            FROM dbo.Attachments{whereSql};

            SELECT {Columns}
            FROM dbo.Attachments{whereSql}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = CreateConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);

        foreach (var addParameters in parameterValues)
        {
            addParameters(command.Parameters);
        }

        command.Parameters.Add("@Offset", SqlDbType.Int).Value = pageIndex * pageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        long totalCount = 0;
        if (await reader.ReadAsync())
        {
            totalCount = reader.GetInt64(0);
        }

        var items = new List<AttachmentRecord>();
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                items.Add(Read(reader));
            }
        }

        return new PagedResult<AttachmentRecord>(items, totalCount);
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

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static void AddParameters(SqlCommand command, AttachmentRecord model)
    {
        command.Parameters.Add("@Active", SqlDbType.Bit).Value = DbValue(model.Active);
        command.Parameters.Add("@DateCreated", SqlDbType.DateTimeOffset).Value = DbValue(model.DateCreated);
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTimeOffset).Value = DbValue(model.CreatedAt);
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 70).Value = DbValue(model.CreatedBy);
        command.Parameters.Add("@ModifiedAt", SqlDbType.DateTimeOffset).Value = DbValue(model.ModifiedAt);
        command.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 70).Value = DbValue(model.ModifiedBy);
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
            ModifiedAt = reader.IsDBNull(5) ? null : reader.GetDateTimeOffset(5),
            ModifiedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
            EmployeeId = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            VendorId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
            InvestigationId = reader.IsDBNull(9) ? null : reader.GetInt64(9),
            FileName = reader.IsDBNull(10) ? null : reader.GetString(10),
            Discriminator = reader.IsDBNull(11) ? null : reader.GetString(11),
            Category = reader.IsDBNull(12) ? null : reader.GetString(12),
            Notes = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }
}
