# Changelog

All notable changes to Azunt.AttachmentManagement are documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

## [1.0.0] - 2026-07-27

### Added

- Added a reusable attachment metadata management library for .NET 8
- Added EF Core In-Memory and SQL Server repository support
- Added Dapper and ADO.NET repository implementations
- Added server-side filtering sorting and paging
- Added creation and modification audit fields
- Added safe SQL Server table creation and nullable-column enhancement
- Added optional relationship index creation
- Added Open XML Excel export
- Added a Blazor Web App demonstration
- Added Startup.cs integration examples
- Added NuGet package metadata symbol package support and MIT licensing

### Changed

- Preserved creation audit values during updates
- Moved ADO.NET filtering sorting and paging to SQL Server
- Renamed the public paged response type from `ArticleSet` to `PagedResult`
- Standardized package company metadata on VisualAcademy

### Fixed

- Aligned `Microsoft.Extensions.Logging.Abstractions` with transitive dependency requirements
- Corrected SQL project build configuration for Visual Studio and SSDT
