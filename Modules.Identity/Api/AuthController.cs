using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Modules.Identity.Application;
using Modules.Identity.Infrastructure;
using Modules.Users.Contracts.Events;

namespace Modules.Identity.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPublisher _publisher;
    private readonly AppIdentityDbContext _identityDbContext;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        RoleManager<ApplicationRole> roleManager,
        IPublisher publisher,
        AppIdentityDbContext identityDbContext,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _roleManager = roleManager;
        _publisher = publisher;
        _identityDbContext = identityDbContext;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request.BranchId == Guid.Empty)
        {
            return BadRequest("BranchId is required.");
        }

        var role = string.IsNullOrWhiteSpace(request.Role) ? "waiter" : request.Role.Trim().ToLowerInvariant();
        if (await _roleManager.RoleExistsAsync(role) == false)
        {
            return BadRequest($"Role '{role}' does not exist.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.Phone,
            BranchId = request.BranchId,
            FullName = request.FullName,
            IsActive = request.IsActive ?? true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        await _userManager.AddToRoleAsync(user, role);

        try
        {
            await _publisher.Publish(
                new UserRegistered(
                    user.Id,
                    user.BranchId,
                    user.Email ?? string.Empty,
                    user.FullName,
                    request.Phone,
                    role,
                    user.IsActive,
                    user.CreatedAtUtc,
                    request.PinHash),
                cancellationToken);
        }
        catch
        {
            await _userManager.DeleteAsync(user);
            throw;
        }

        return Ok(new { user.Id, user.Email, user.BranchId, user.FullName, user.PhoneNumber, Role = role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var refreshToken = await CreateAndPersistRefreshToken(user, CancellationToken.None);

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresInMinutes = _jwtOptions.AccessTokenMinutes,
            user.Id,
            user.Email,
            user.BranchId,
            Roles = roles
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("RefreshToken is required.");
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _identityDbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.RevokedAtUtc.HasValue || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var newRefreshToken = _refreshTokenService.GenerateToken();
        var newRefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.ReplacedByTokenHash = newRefreshTokenHash;

        _identityDbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BranchId = user.BranchId,
            TokenHash = newRefreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await _identityDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresInMinutes = _jwtOptions.AccessTokenMinutes
        });
    }

    private async Task EnsureRoleExists(string role)
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                IsSystem = false
            });
        }
    }

    public sealed record RegisterRequest(
        string Email,
        string Password,
        Guid BranchId,
        string FullName,
        string? Phone,
        string? Role,
        string? PinHash,
        bool? IsActive);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);

    private async Task<string> CreateAndPersistRefreshToken(ApplicationUser user, CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenService.GenerateToken();
        var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);

        _identityDbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BranchId = user.BranchId,
            TokenHash = refreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await _identityDbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }
}
