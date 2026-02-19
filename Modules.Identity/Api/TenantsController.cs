using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Identity.Application;
using Modules.Identity.DTO;
using Modules.Identity.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Modules.Identity.Api;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly AppIdentityDbContext _db;
    private readonly ITenantAuthService _tenantAuthService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public TenantsController(
        AppIdentityDbContext db,
        ITenantAuthService tenantAuthService,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _tenantAuthService = tenantAuthService;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId is null)        {
            return Unauthorized();
        }
        
        var result = await _tenantAuthService.CreateTenant(request, Guid.Parse(userId), ct);
        if (result != null)
        {
            return BadRequest(result);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);

        return Ok(new
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            user.Id,
            user.Email,
            user.BranchId,
            Roles = roles
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenants = await _db.Tenants.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        return Ok(tenants);
    }
}
