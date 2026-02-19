namespace Modules.Identity.DTO;

public sealed record RegisterResponse(
    Guid Id,
    string? Email,
    Guid? BranchId,
    string FullName,
    string? PhoneNumber,
    string Role);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresInMinutes,
    Guid Id,
    string? Email,
    Guid? BranchId,
    IReadOnlyCollection<string> Roles);

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresInMinutes);

public sealed record ManagerCreateUserResponse(
    Guid Id,
    string? PhoneNumber,
    string FullName,
    string Role,
    string? VerificationSid);

public sealed record PhoneLoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresInMinutes,
    Guid Id,
    string? PhoneNumber,
    Guid? BranchId,
    IReadOnlyCollection<string> Roles);
