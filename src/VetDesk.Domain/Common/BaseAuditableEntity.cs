namespace VetDesk.Domain.Common;

/// <summary>Records who created and last changed a row. Filled in by the database context on save.</summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public string? LastModifiedBy { get; set; }
}
