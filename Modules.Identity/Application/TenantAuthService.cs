using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Modules.Identity.DTO;
using Modules.Identity.Infrastructure;

namespace Modules.Identity.Application
{
    public interface ITenantAuthService
    {
        Task<string?> CreateTenant(CreateTenantRequest request, Guid userId, CancellationToken ct);
    }
    public class TenantAuthService : ITenantAuthService
    {
        private readonly AppIdentityDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        
        public TenantAuthService(
            AppIdentityDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<string?> CreateTenant(CreateTenantRequest request, Guid userId, CancellationToken ct)
        {
            var exists = await _db.Tenants.AnyAsync(x => x.Name == request.Name, ct);
            if (exists)
            {
                return "Tenant name already exists.";
            }

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                LegalName = request.LegalName?.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _db.Tenants.Add(tenant);

            var tenantUser = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = userId,
                Role = "Owner",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.TenantUsers.Add(tenantUser);
            await _db.SaveChangesAsync(ct);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return "User not found.";
            }

            const string targetRole = "manager";
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                return $"Role '{targetRole}' does not exist.";
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, targetRole);
            if (!addRoleResult.Succeeded)
            {
                return string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
            }

            return null;

        }
    }
}