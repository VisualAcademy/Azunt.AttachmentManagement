# Azunt.AttachmentManagement

Azunt.AttachmentManagement is a reusable .NET 8 module for managing attachment metadata with EF Core, SQL Server, Dapper, ADO.NET, Blazor, and Open XML Excel export.

## Features

- Unified `AttachmentRecord` model
- EF Core In-Memory and SQL Server support
- Dapper and ADO.NET repository implementations
- Server-side search, filtering, sorting, and paging
- Employee, vendor, and investigation relationship metadata
- Creation and modification audit fields
- Safe creation or nullable-column enhancement of `dbo.Attachments`
- Optional lookup-index creation
- Blazor Server CRUD demonstration
- Excel export with the Open XML SDK
- MIT license

## Audit fields

The module preserves creation metadata and records later changes separately:

```text
CreatedAt
CreatedBy
ModifiedAt
ModifiedBy
```

`UpdateAsync` and `UpdateMetadataAsync` automatically set `ModifiedAt` to UTC. They update `ModifiedBy` only when a non-empty modifier is supplied. Update operations preserve `CreatedAt`, `DateCreated`, and `CreatedBy` so creation audit metadata remains immutable.

## Install

```powershell
dotnet add package Azunt.AttachmentManagement --version 1.0.0
```

## Register in a .NET 8 MVC project using Startup.cs

```csharp
using Azunt.AttachmentManagement;

public void ConfigureServices(IServiceCollection services)
{
    var connectionString = Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    services.AddDependencyInjectionContainerForAttachmentApp(
        connectionString,
        AttachmentServicesRegistrationExtensions.RepositoryMode.EfCoreSqlServer);

    services.AddControllersWithViews();
}
```

Use `RepositoryMode.Dapper` or `RepositoryMode.AdoNet` to select another SQL Server repository.

## Create or enhance the table at startup

The default operation creates the table when missing or adds only missing nullable columns. It does not create optional indexes.

```csharp
public void Configure(
    IApplicationBuilder app,
    IWebHostEnvironment env)
{
    AttachmentsTableBuilder
        .RunAsync(app.ApplicationServices)
        .GetAwaiter()
        .GetResult();

    // Remaining middleware configuration...
}
```

To create the recommended relationship indexes at the same time:

```csharp
AttachmentsTableBuilder
    .RunAsync(app.ApplicationServices, ensureIndexes: true)
    .GetAwaiter()
    .GetResult();
```

For production scale-out deployments, prefer running schema enhancement once in a deployment step or administrator process rather than from every application instance.

## Direct builder usage

```csharp
using var scope = app.ApplicationServices.CreateScope();
var tableBuilder = scope.ServiceProvider.GetRequiredService<AttachmentsTableBuilder>();

await tableBuilder.EnsureAsync(connectionString);
await tableBuilder.EnsureIndexesAsync(connectionString);
```

## Pack

```powershell
dotnet restore .\src\Azunt.AttachmentManagement\Azunt.AttachmentManagement\Azunt.AttachmentManagement.csproj

dotnet build .\src\Azunt.AttachmentManagement\Azunt.AttachmentManagement\Azunt.AttachmentManagement.csproj `
    -c Release `
    --no-restore

dotnet pack .\src\Azunt.AttachmentManagement\Azunt.AttachmentManagement\Azunt.AttachmentManagement.csproj `
    -c Release `
    --no-build `
    -o .\artifacts
```

The package project is configured to create both `.nupkg` and `.snupkg` files.

## Security notice

The included `Azunt.Web` project is a development demonstration. Add authentication, authorization, tenant isolation, request validation, secure file storage, file type and size checks, malware scanning, audit logging, and rate limiting before production deployment.

## License

MIT
