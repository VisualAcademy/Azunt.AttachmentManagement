using Microsoft.EntityFrameworkCore;

namespace Azunt.AttachmentManagement;

public class AttachmentAppDbContext : DbContext
{
    public AttachmentAppDbContext(DbContextOptions<AttachmentAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttachmentRecord> Attachments => Set<AttachmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AttachmentRecord>();

        entity.ToTable("Attachments");
        entity.HasKey(m => m.Id);

        entity.Property(m => m.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        entity.Property(m => m.Active).HasColumnName("Active");
        entity.Property(m => m.DateCreated).HasColumnName("DateCreated");
        entity.Property(m => m.CreatedAt).HasColumnName("CreatedAt");
        entity.Property(m => m.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(70);
        entity.Property(m => m.EmployeeId).HasColumnName("EmployeeID");
        entity.Property(m => m.VendorId).HasColumnName("VendorID");
        entity.Property(m => m.InvestigationId).HasColumnName("InvestigationID");
        entity.Property(m => m.FileName).HasColumnName("FileName");
        entity.Property(m => m.Discriminator).HasColumnName("Discriminator");
        entity.Property(m => m.Category).HasColumnName("Category").HasMaxLength(100);
        entity.Property(m => m.Notes).HasColumnName("Notes");
    }
}
