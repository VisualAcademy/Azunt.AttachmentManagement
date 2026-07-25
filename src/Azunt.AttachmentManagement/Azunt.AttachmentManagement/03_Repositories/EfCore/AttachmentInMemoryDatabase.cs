using Microsoft.EntityFrameworkCore.Storage;

namespace Azunt.AttachmentManagement;

public static class AttachmentInMemoryDatabase
{
    public const string DefaultName = "AzuntAttachmentManagement";
    public static readonly InMemoryDatabaseRoot Root = new();
}
