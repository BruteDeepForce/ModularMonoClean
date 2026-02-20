using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Modules.Identity.Infrastructure;

namespace Modules.Identity.Application;

public interface IJwtTokenService
{
    string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    Task<string> CreateAndPersistRefreshToken(ApplicationUser user, CancellationToken cancellationToken);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly AppIdentityDbContext _identityDbContext;


    public JwtTokenService(IOptions<JwtOptions> options, IRefreshTokenService refreshTokenService, AppIdentityDbContext identityDbContext)
    {
        _options = options.Value;
        _refreshTokenService = refreshTokenService;
        _identityDbContext = identityDbContext;
    }

    public string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim("branch_id", user.BranchId.Value.ToString()));
        }

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
        public async Task<string> CreateAndPersistRefreshToken(ApplicationUser user, CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenService.GenerateToken();
        var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);

        _identityDbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BranchId = user.BranchId ?? Guid.Empty,
            TokenHash = refreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays)
        });

        await _identityDbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }
}
