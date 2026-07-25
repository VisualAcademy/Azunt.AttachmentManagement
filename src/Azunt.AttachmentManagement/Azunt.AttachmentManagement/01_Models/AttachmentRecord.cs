using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Azunt.AttachmentManagement;

/// <summary>
/// Unified model for the dbo.Attachments table used by employee, vendor,
/// investigation, and future entity attachment scenarios.
/// </summary>
[Table("Attachments")]
public class AttachmentRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    public long Id { get; set; }

    public bool? Active { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    [StringLength(70)]
    public string? CreatedBy { get; set; }

    [Column("EmployeeID")]
    public long? EmployeeId { get; set; }

    [Column("VendorID")]
    public long? VendorId { get; set; }

    [Column("InvestigationID")]
    public long? InvestigationId { get; set; }

    public string? FileName { get; set; }

    public string? Discriminator { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public string? Notes { get; set; }

    [NotMapped]
    public DateTimeOffset? EffectiveCreatedAt => CreatedAt ?? DateCreated;

    [NotMapped]
    public bool EffectiveActive => Active != false;
}
