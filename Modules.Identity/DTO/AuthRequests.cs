namespace Modules.Identity.DTO;

public sealed record RegisterRequest(
    string Email,
    string Password,
    Guid? BranchId,
    string FullName,
    string? Phone,
    string? Role,
    string? PinHash,
    bool? IsActive);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record ManagerCreateUserRequest(
    string FullName,
    string Phone,
    string? Email,
    Guid? BranchId,
    string? Role,
    string? PinHash);

public sealed record PhoneLoginRequest(string Phone, string Pin);

public sealed record SetPinRequest(string Phone, string NewPin);
