using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Azunt.AttachmentManagement;

public static class AttachmentServicesRegistrationExtensions
{
    public enum RepositoryMode
    {
        EfCoreInMemory,
        EfCoreSqlServer,
        EfCore,
        Dapper,
        AdoNet
    }

    public static IServiceCollection AddDependencyInjectionContainerForAttachmentApp(
        this IServiceCollection services,
        string? connectionString = null,
        RepositoryMode mode = RepositoryMode.EfCoreInMemory,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Transient)
    {
        switch (mode)
        {
            case RepositoryMode.EfCoreInMemory:
                services.AddDbContext<AttachmentAppDbContext>(
                    options => options.UseInMemoryDatabase(
                        AttachmentInMemoryDatabase.DefaultName,
                        AttachmentInMemoryDatabase.Root),
                    dbContextLifetime);
                services.AddTransient(_ => new AttachmentAppDbContextFactory());
                services.AddTransient<IAttachmentRepository, AttachmentRepository>();
                break;

            case RepositoryMode.EfCoreSqlServer:
            case RepositoryMode.EfCore:
                EnsureConnectionString(connectionString, mode);
                services.AddDbContext<AttachmentAppDbContext>(
                    options => options.UseSqlServer(connectionString),
                    dbContextLifetime);
                services.AddTransient(_ => new AttachmentAppDbContextFactory(connectionString!));
                services.AddTransient<IAttachmentRepository, AttachmentRepository>();
                break;

            case RepositoryMode.Dapper:
                EnsureConnectionString(connectionString, mode);
                services.AddTransient<IAttachmentRepository>(provider =>
                    new AttachmentRepositoryDapper(
                        connectionString!,
                        provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttachmentRepositoryDapper>>()));
                break;

            case RepositoryMode.AdoNet:
                EnsureConnectionString(connectionString, mode);
                services.AddTransient<IAttachmentRepository>(provider =>
                    new AttachmentRepositoryAdoNet(
                        connectionString!,
                        provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AttachmentRepositoryAdoNet>>()));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported repository mode.");
        }

        services.AddTransient<AttachmentsTableBuilder>();
        return services;
    }

    private static void EnsureConnectionString(string? connectionString, RepositoryMode mode)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Repository mode {mode} requires a SQL Server connection string.");
        }
    }
}
