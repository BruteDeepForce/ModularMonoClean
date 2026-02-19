namespace Modules.Identity.Infrastructure;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<TenantUser> Users { get; set; } = new List<TenantUser>();
}
