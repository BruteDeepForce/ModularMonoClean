using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Modules.Identity.DTO;
using Modules.Identity.Infrastructure;

namespace Modules.Identity.Application
{
    public interface ITenantAuthService
    {
        Task<ServiceResult<CreateTenantResponse>> CreateTenant(CreateTenantRequest request, Guid userId, CancellationToken ct);
    }
    public class TenantAuthService : ITenantAuthService
    {
        private readonly AppIdentityDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly JwtTokenService _jwtTokenService;

        
        public TenantAuthService(
            AppIdentityDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            JwtTokenService jwtTokenService)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
        }
        public async Task<ServiceResult<CreateTenantResponse>> CreateTenant(CreateTenantRequest request, Guid userId, CancellationToken ct)
        {
            var exists = await _db.Tenants.AnyAsync(x => x.Name == request.Name, ct);
            if (exists)
            {
                return ServiceResult<CreateTenantResponse>.Fail("Tenant name already exists.");
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
                return ServiceResult<CreateTenantResponse>.Fail("User not found.");
            }

            const string targetRole = "manager";
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                return ServiceResult<CreateTenantResponse>.Fail($"Role '{targetRole}' does not exist.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, targetRole);
            if (!addRoleResult.Succeeded)
            {
                return ServiceResult<CreateTenantResponse>.Fail(string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
            }

            //token dönelim 
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
            var refreshToken = await _jwtTokenService.CreateAndPersistRefreshToken(user, CancellationToken.None);

            return ServiceResult<CreateTenantResponse>.Ok(new CreateTenantResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                TokenType: "Bearer",
                ExpiresInMinutes: 60, 
                Id: tenant.Id));

        }
    }
}