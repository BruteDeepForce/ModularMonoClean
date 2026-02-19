using Microsoft.AspNetCore.Identity;

namespace Modules.Identity.Infrastructure;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? BranchId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
