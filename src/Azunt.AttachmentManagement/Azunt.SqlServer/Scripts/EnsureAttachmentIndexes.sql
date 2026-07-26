IF OBJECT_ID(N'[dbo].[Attachments]', N'U') IS NULL
BEGIN
    THROW 50001, 'dbo.Attachments must exist before indexes can be created.', 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'IX_Attachments_EmployeeID'
      AND [object_id] = OBJECT_ID(N'dbo.Attachments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attachments_EmployeeID]
        ON [dbo].[Attachments] ([EmployeeID]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'IX_Attachments_VendorID'
      AND [object_id] = OBJECT_ID(N'dbo.Attachments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attachments_VendorID]
        ON [dbo].[Attachments] ([VendorID]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'IX_Attachments_InvestigationID'
      AND [object_id] = OBJECT_ID(N'dbo.Attachments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attachments_InvestigationID]
        ON [dbo].[Attachments] ([InvestigationID]);
END;
GO
