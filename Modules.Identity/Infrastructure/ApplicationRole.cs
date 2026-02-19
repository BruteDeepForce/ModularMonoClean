using Microsoft.AspNetCore.Identity;

namespace Modules.Identity.Infrastructure;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}
