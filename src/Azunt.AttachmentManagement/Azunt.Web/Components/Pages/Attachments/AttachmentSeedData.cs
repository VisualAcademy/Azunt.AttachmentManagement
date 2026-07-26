using Azunt.AttachmentManagement;

namespace Azunt.Web.Components.Pages.Attachments;

public static class AttachmentSeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();

        if ((await repository.GetAllAsync()).Count > 0)
        {
            return;
        }

        await repository.AddAsync(new AttachmentRecord
        {
            Active = true,
            CreatedBy = "Sample User",
            EmployeeId = 1001,
            FileName = "profile-reference.pdf",
            Discriminator = "PersonDocument",
            Category = "Reference",
            Notes = "Example document linked to a person record."
        });

        await repository.AddAsync(new AttachmentRecord
        {
            Active = true,
            CreatedBy = "Sample User",
            VendorId = 2001,
            FileName = "service-agreement.pdf",
            Discriminator = "OrganizationDocument",
            Category = "Agreement",
            Notes = "Example document linked to an organization record."
        });

        var reviewDocument = await repository.AddAsync(new AttachmentRecord
        {
            Active = true,
            CreatedBy = "Sample User",
            InvestigationId = 3001,
            FileName = "review-supporting-document.pdf",
            Discriminator = "ReviewDocument",
            Category = "Supporting Document",
            Notes = "Example document linked to a review record."
        });

        await repository.UpdateMetadataAsync(
            reviewDocument.Id,
            reviewDocument.InvestigationId,
            "Reviewed Document",
            "Example metadata update demonstrating ModifiedAt and ModifiedBy.",
            "Sample Reviewer");

        await repository.AddAsync(new AttachmentRecord
        {
            Active = null,
            CreatedBy = "Sample Import",
            FileName = "imported-document.txt",
            Discriminator = "ImportedDocument",
            Category = "Imported",
            Notes = "Active is intentionally null to demonstrate backward-compatible handling."
        });
    }
}
