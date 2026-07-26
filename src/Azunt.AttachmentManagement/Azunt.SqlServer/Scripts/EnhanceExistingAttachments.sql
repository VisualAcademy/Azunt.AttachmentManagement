IF OBJECT_ID(N'[dbo].[Attachments]', N'U') IS NULL
BEGIN
    PRINT 'dbo.Attachments does not exist. No existing table was changed.';
    RETURN;
END;
GO

IF COL_LENGTH('dbo.Attachments', 'Active') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [Active] BIT NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'DateCreated') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [DateCreated] DATETIMEOFFSET(7) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'CreatedAt') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [CreatedAt] DATETIMEOFFSET(7) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'CreatedBy') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [CreatedBy] NVARCHAR(70) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'ModifiedAt') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [ModifiedAt] DATETIMEOFFSET(7) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'ModifiedBy') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [ModifiedBy] NVARCHAR(70) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'EmployeeID') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [EmployeeID] BIGINT NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'VendorID') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [VendorID] BIGINT NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'InvestigationID') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [InvestigationID] BIGINT NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'FileName') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [FileName] NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'Discriminator') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [Discriminator] NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'Category') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [Category] NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Attachments', 'Notes') IS NULL
    ALTER TABLE [dbo].[Attachments] ADD [Notes] NVARCHAR(MAX) NULL;
GO
