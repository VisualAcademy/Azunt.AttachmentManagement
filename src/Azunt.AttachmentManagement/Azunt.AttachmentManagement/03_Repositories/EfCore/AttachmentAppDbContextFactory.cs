using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.AttachmentManagement;

public class AttachmentAppDbContextFactory
{
    private readonly IConfiguration? _configuration;
    private readonly string? _defaultConnectionString;

    public AttachmentAppDbContextFactory()
    {
    }

    public AttachmentAppDbContextFactory(string defaultConnectionString)
    {
        _defaultConnectionString = defaultConnectionString;
    }

    public AttachmentAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AttachmentAppDbContext CreateDbContext()
    {
        if (!string.IsNullOrWhiteSpace(_defaultConnectionString))
        {
            return CreateSqlServerDbContext(_defaultConnectionString);
        }

        var configuredConnection = _configuration?.GetConnectionString("DefaultConnection");

        return string.IsNullOrWhiteSpace(configuredConnection)
            ? CreateInMemoryDbContext()
            : CreateSqlServerDbContext(configuredConnection);
    }

    public AttachmentAppDbContext CreateDbContext(string? connectionString)
    {
        return string.IsNullOrWhiteSpace(connectionString)
            ? CreateDbContext()
            : CreateSqlServerDbContext(connectionString);
    }

    public AttachmentAppDbContext CreateInMemoryDbContext(
        string databaseName = AttachmentInMemoryDatabase.DefaultName)
    {
        var options = new DbContextOptionsBuilder<AttachmentAppDbContext>()
            .UseInMemoryDatabase(databaseName, AttachmentInMemoryDatabase.Root)
            .Options;

        return new AttachmentAppDbContext(options);
    }

    public AttachmentAppDbContext CreateSqlServerDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        var options = new DbContextOptionsBuilder<AttachmentAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AttachmentAppDbContext(options);
    }
}
