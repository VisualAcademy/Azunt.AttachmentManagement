using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.AttachmentManagement;

/// <summary>
/// Creates dbo.Attachments or safely adds missing compatibility columns as nullable columns.
/// Existing columns, data, defaults, foreign keys, and nullability are not modified.
/// Index creation is opt-in for existing databases.
/// </summary>
public sealed class AttachmentsTableBuilder
{
    private static readonly IReadOnlyList<ColumnDefinition> ExpectedColumns =
    [
        new("Active", "BIT"),
        new("DateCreated", "DATETIMEOFFSET(7)"),
        new("CreatedAt", "DATETIMEOFFSET(7)"),
        new("CreatedBy", "NVARCHAR(70)"),
        new("ModifiedAt", "DATETIMEOFFSET(7)"),
        new("ModifiedBy", "NVARCHAR(70)"),
        new("EmployeeID", "BIGINT"),
        new("VendorID", "BIGINT"),
        new("InvestigationID", "BIGINT"),
        new("FileName", "NVARCHAR(MAX)"),
        new("Discriminator", "NVARCHAR(MAX)"),
        new("Category", "NVARCHAR(100)"),
        new("Notes", "NVARCHAR(MAX)")
    ];

    private readonly ILogger<AttachmentsTableBuilder> _logger;

    public AttachmentsTableBuilder(ILogger<AttachmentsTableBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates or enhances dbo.Attachments without creating optional indexes.
    /// This overload preserves compatibility with earlier callers.
    /// </summary>
    public Task EnsureAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
        => EnsureAsync(connectionString, ensureIndexes: false, cancellationToken: cancellationToken);

    /// <summary>
    /// Creates or enhances dbo.Attachments and optionally creates lookup indexes.
    /// </summary>
    public async Task EnsureAsync(
        string connectionString,
        bool ensureIndexes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, cancellationToken))
        {
            await CreateTableAsync(connection, cancellationToken);
        }
        else
        {
            foreach (var column in ExpectedColumns)
            {
                await EnsureNullableColumnAsync(connection, column, cancellationToken);
            }
        }

        if (ensureIndexes)
        {
            await EnsureIndexesAsync(connection, cancellationToken);
        }

        _logger.LogInformation(
            "Attachments table creation or enhancement completed for database {Database}. Indexes requested: {EnsureIndexes}.",
            connection.Database,
            ensureIndexes);
    }

    /// <summary>
    /// Creates the recommended lookup indexes without changing table columns.
    /// </summary>
    public async Task EnsureIndexesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, cancellationToken))
        {
            throw new InvalidOperationException("dbo.Attachments must exist before indexes can be created.");
        }

        await EnsureIndexesAsync(connection, cancellationToken);

        _logger.LogInformation(
            "Attachments indexes completed for database {Database}.",
            connection.Database);
    }

    public Task EnsureDatabasesAsync(
        IEnumerable<string> connectionStrings,
        CancellationToken cancellationToken = default)
        => EnsureDatabasesAsync(connectionStrings, ensureIndexes: false, cancellationToken: cancellationToken);

    /// <summary>
    /// Applies the same safe schema operation to an explicit collection of databases.
    /// The library does not assume a tenant catalog or registry table.
    /// </summary>
    public async Task EnsureDatabasesAsync(
        IEnumerable<string> connectionStrings,
        bool ensureIndexes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionStrings);

        foreach (var connectionString in connectionStrings
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await EnsureAsync(connectionString, ensureIndexes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or enhance an Attachments table.");
            }
        }
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'Attachments';
            """;

        await using var command = new SqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task CreateTableAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE [dbo].[Attachments]
            (
                [ID]              BIGINT IDENTITY(1,1) NOT NULL,
                [Active]          BIT NULL,
                [DateCreated]     DATETIMEOFFSET(7) NULL,
                [CreatedAt]       DATETIMEOFFSET(7) NULL,
                [CreatedBy]       NVARCHAR(70) NULL,
                [ModifiedAt]      DATETIMEOFFSET(7) NULL,
                [ModifiedBy]      NVARCHAR(70) NULL,
                [EmployeeID]      BIGINT NULL,
                [VendorID]        BIGINT NULL,
                [InvestigationID] BIGINT NULL,
                [FileName]        NVARCHAR(MAX) NULL,
                [Discriminator]   NVARCHAR(MAX) NULL,
                [Category]        NVARCHAR(100) NULL,
                [Notes]           NVARCHAR(MAX) NULL,
                CONSTRAINT [PK_Attachments] PRIMARY KEY CLUSTERED ([ID] ASC)
            );
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Created dbo.Attachments in database {Database}.", connection.Database);
    }

    private async Task EnsureNullableColumnAsync(
        SqlConnection connection,
        ColumnDefinition column,
        CancellationToken cancellationToken)
    {
        const string checkSql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'Attachments'
              AND COLUMN_NAME = @ColumnName;
            """;

        await using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@ColumnName", column.Name);
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            return;
        }

        var alterSql = $"ALTER TABLE [dbo].[Attachments] ADD [{column.Name}] {column.SqlType} NULL;";
        await using var alterCommand = new SqlCommand(alterSql, connection);
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Added nullable column {ColumnName} ({ColumnType}) to dbo.Attachments.",
            column.Name,
            column.SqlType);
    }

    private static async Task EnsureIndexesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(connection, "IX_Attachments_EmployeeID", "EmployeeID", cancellationToken);
        await EnsureIndexAsync(connection, "IX_Attachments_VendorID", "VendorID", cancellationToken);
        await EnsureIndexAsync(connection, "IX_Attachments_InvestigationID", "InvestigationID", cancellationToken);
    }

    private static async Task EnsureIndexAsync(
        SqlConnection connection,
        string indexName,
        string columnName,
        CancellationToken cancellationToken)
    {
        const string checkSql = """
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE [name] = @IndexName
              AND [object_id] = OBJECT_ID(N'dbo.Attachments');
            """;

        await using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@IndexName", indexName);
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            return;
        }

        var createSql = $"CREATE NONCLUSTERED INDEX [{indexName}] ON [dbo].[Attachments] ([{columnName}] ASC);";
        await using var createCommand = new SqlCommand(createSql, connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public static Task RunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
        => RunAsync(services, ensureIndexes: false, cancellationToken: cancellationToken);

    public static async Task RunAsync(
        IServiceProvider services,
        bool ensureIndexes,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var builder = scope.ServiceProvider.GetRequiredService<AttachmentsTableBuilder>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        await builder.EnsureAsync(connectionString, ensureIndexes, cancellationToken);
    }

    private sealed record ColumnDefinition(string Name, string SqlType);
}
