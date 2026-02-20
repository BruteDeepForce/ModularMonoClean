using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.Identity.DTO;
using Modules.Identity.Infrastructure;
using Modules.Users.Contracts.Events;
using MediatR;

namespace Modules.Identity.Application;

public interface IAuthService
{
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<ServiceResult<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<ServiceResult<ManagerCreateUserResponse>> ManagerCreateUserAsync(ManagerCreateUserRequest request, CancellationToken ct);
    Task<ServiceResult<PhoneLoginResponse>> FirstPhoneLoginAsync(PhoneLoginRequest request, CancellationToken ct);
    Task<ServiceResult<PhoneLoginResponse>> PhoneLoginAsync(PhoneLoginRequest request, CancellationToken ct);
    Task<ServiceResult<string>> SetPinAsync(SetPinRequest request, Guid userId, CancellationToken ct);
    Task<ServiceResult<string>> SeedRolesAsync();
}

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPublisher _publisher;
    private readonly AppIdentityDbContext _identityDbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly IPhoneVerificationService _phoneVerificationService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        RoleManager<ApplicationRole> roleManager,
        IPublisher publisher,
        AppIdentityDbContext identityDbContext,
        IOptions<JwtOptions> jwtOptions,
        IPhoneVerificationService phoneVerificationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _roleManager = roleManager;
        _publisher = publisher;
        _identityDbContext = identityDbContext;
        _jwtOptions = jwtOptions.Value;
        _phoneVerificationService = phoneVerificationService;
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? "waiter" : request.Role.Trim().ToLowerInvariant();
        if (!await _roleManager.RoleExistsAsync(role))
        {
            return ServiceResult<RegisterResponse>.Fail($"Role '{role}' does not exist.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.Phone,
            BranchId = request.BranchId ?? Guid.Empty,
            FullName = request.FullName,
            IsActive = request.IsActive ?? true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<RegisterResponse>.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, role);

        try
        {
            await _publisher.Publish(
                new UserRegistered(
                    user.Id,
                    user.BranchId ?? Guid.Empty,
                    user.Email ?? string.Empty,
                    user.FullName,
                    request.Phone,
                    role,
                    user.IsActive,
                    user.CreatedAtUtc,
                    request.PinHash),
                ct);
        }
        catch
        {
            await _userManager.DeleteAsync(user);
            throw;
        }

        return ServiceResult<RegisterResponse>.Ok(new RegisterResponse(
            user.Id,
            user.Email,
            user.BranchId,
            user.FullName,
            user.PhoneNumber,
            role));
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail("Unauthorized");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return ServiceResult<LoginResponse>.Fail("Unauthorized");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var refreshToken = await _jwtTokenService.CreateAndPersistRefreshToken(user, CancellationToken.None);

        return ServiceResult<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshToken,
            "Bearer",
            _jwtOptions.AccessTokenMinutes,
            user.Id,
            user.Email,
            user.BranchId,
            roles.ToList()));
    }

    public async Task<ServiceResult<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ServiceResult<RefreshResponse>.Fail("RefreshToken is required.");
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _identityDbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        if (refreshToken is null || refreshToken.RevokedAtUtc.HasValue || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ServiceResult<RefreshResponse>.Fail("Unauthorized");
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return ServiceResult<RefreshResponse>.Fail("Unauthorized");
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
            BranchId = user.BranchId ?? Guid.Empty,
            TokenHash = newRefreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await _identityDbContext.SaveChangesAsync(ct);

        return ServiceResult<RefreshResponse>.Ok(new RefreshResponse(
            newAccessToken,
            newRefreshToken,
            "Bearer",
            _jwtOptions.AccessTokenMinutes));
    }

    public async Task<ServiceResult<ManagerCreateUserResponse>> ManagerCreateUserAsync(ManagerCreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return ServiceResult<ManagerCreateUserResponse>.Fail("Phone is required.");
        }

        var normalizedRole = string.IsNullOrWhiteSpace(request.Role)
            ? "waiter"
            : request.Role.Trim().ToLowerInvariant();

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
        {
            return ServiceResult<ManagerCreateUserResponse>.Fail($"Role '{normalizedRole}' does not exist.");
        }

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone, ct);
        if (existingUser is not null)
        {
            return ServiceResult<ManagerCreateUserResponse>.Fail("Phone number already registered.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Phone,
            Email = request.Email,
            PhoneNumber = request.Phone,
            BranchId = request.BranchId,
            FullName = request.FullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // kullanıcı oluşturulmadan önce doğrulama kodu gönderelim, böylece geçersiz telefon numaralarına sahip kullanıcılar oluşmaz
        var verificationResult = await _phoneVerificationService.SendCodeAsync(user.PhoneNumber ?? string.Empty, ct);
        if (!verificationResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult<ManagerCreateUserResponse>.Fail(verificationResult.Error ?? "Failed to send verification code.");
        }

        var createResult = await _userManager.CreateAsync(user, "Temp123");
        if (!createResult.Succeeded)
        {
            return ServiceResult<ManagerCreateUserResponse>.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, normalizedRole);

        await _publisher.Publish(
            new UserRegistered(
                user.Id,
                user.BranchId ?? Guid.Empty,
                user.Email ?? string.Empty,
                user.FullName,
                user.PhoneNumber,
                normalizedRole,
                user.IsActive,
                user.CreatedAtUtc,
                request.PinHash),
            ct);

        return ServiceResult<ManagerCreateUserResponse>.Ok(new ManagerCreateUserResponse(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            normalizedRole,
            verificationResult.VerificationSid));
    }

    public async Task<ServiceResult<PhoneLoginResponse>> FirstPhoneLoginAsync(PhoneLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Pin))
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Phone and code are required.");
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone, ct);
        if (user is null || !user.IsActive)
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Unauthorized");
        }

        var verificationResult = await _phoneVerificationService.CheckCodeAsync(request.Phone, request.Pin, ct);
        if (!verificationResult.Succeeded)
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Invalid or expired code.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var refreshToken = await _jwtTokenService.CreateAndPersistRefreshToken(user, ct);
        

    await _identityDbContext.SaveChangesAsync(ct);

        return ServiceResult<PhoneLoginResponse>.Ok(new PhoneLoginResponse(
            accessToken,
            refreshToken,
            "Bearer",
            _jwtOptions.AccessTokenMinutes,
            user.Id,
            user.PhoneNumber,
            user.BranchId,
            roles.ToList()));
    }

    public async Task<ServiceResult<string>> SetPinAsync(SetPinRequest request, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.NewPin))
        {
            return ServiceResult<string>.Fail("Phone and new PIN are required.");
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone && x.Id == userId, ct);
        if (user is null || !user.IsActive)
        {
            return ServiceResult<string>.Fail("Unauthorized");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPin);
        if (!resetResult.Succeeded)
        {
            return ServiceResult<string>.Fail(string.Join(", ", resetResult.Errors.Select(e => e.Description)));
        }

        return ServiceResult<string>.Ok("PIN updated successfully.");
    }

    public async Task<ServiceResult<PhoneLoginResponse>> PhoneLoginAsync(PhoneLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Pin))
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Phone and PIN are required.");
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone, ct);
        if (user is null || !user.IsActive)
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Unauthorized");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Pin, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return ServiceResult<PhoneLoginResponse>.Fail("Unauthorized");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var refreshToken = await _jwtTokenService.CreateAndPersistRefreshToken(user, ct);

        return ServiceResult<PhoneLoginResponse>.Ok(new PhoneLoginResponse(
            accessToken,
            refreshToken,
            "Bearer",
            _jwtOptions.AccessTokenMinutes,
            user.Id,
            user.PhoneNumber,
            user.BranchId,
            roles.ToList()));
    }

    public async Task<ServiceResult<string>> SeedRolesAsync()
    {
        var rolesToSeed = new[] { "admin", "manager", "stockmanager", "director", "waiter", "cashier", "chef" };

        foreach (var role in rolesToSeed)
        {
            if (await _roleManager.RoleExistsAsync(role))
                continue;

            var isSystem = role is "admin" or "manager" or "director";

            await _roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                IsSystem = isSystem
            });
        }

        return ServiceResult<string>.Ok("Roles seeded successfully.");
    }

}
