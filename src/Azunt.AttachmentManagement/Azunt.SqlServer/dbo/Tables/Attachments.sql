CREATE TABLE [dbo].[Attachments]
(
    [ID]              BIGINT             IDENTITY (1, 1) NOT NULL,
    [Active]          BIT                NULL,
    [DateCreated]     DATETIMEOFFSET (7) NULL,
    [CreatedAt]       DATETIMEOFFSET (7) NULL,
    [CreatedBy]       NVARCHAR (70)      NULL,
    [ModifiedAt]      DATETIMEOFFSET (7) NULL,
    [ModifiedBy]      NVARCHAR (70)      NULL,
    [EmployeeID]      BIGINT             NULL,
    [VendorID]        BIGINT             NULL,
    [InvestigationID] BIGINT             NULL,
    [FileName]        NVARCHAR (MAX)     NULL,
    [Discriminator]   NVARCHAR (MAX)     NULL,
    [Category]        NVARCHAR (100)     NULL,
    [Notes]           NVARCHAR (MAX)     NULL,
    CONSTRAINT [PK_Attachments] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_Attachments_EmployeeID]
    ON [dbo].[Attachments] ([EmployeeID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Attachments_VendorID]
    ON [dbo].[Attachments] ([VendorID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Attachments_InvestigationID]
    ON [dbo].[Attachments] ([InvestigationID] ASC);
GO
