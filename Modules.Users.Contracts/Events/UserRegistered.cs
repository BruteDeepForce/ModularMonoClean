using MediatR;

namespace Modules.Users.Contracts.Events;

public record UserRegistered(
    Guid UserId,
    Guid? BranchId,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    bool IsActive,
    DateTime RegisteredAtUtc,
    string? PinHash) : INotification;
