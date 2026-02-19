using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Users.Contracts.Events;
using Modules.Users.Domain;
using Modules.Users.Infrastructure;

namespace Modules.Users.Application;

public class UserRegisteredHandler : INotificationHandler<UserRegistered>
{
    private readonly UserDbContext _db;

    public UserRegisteredHandler(UserDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UserRegistered n, CancellationToken ct)
    {
        var existing = await _db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == n.UserId, ct);
        if (existing)
        {
            return;
        }

        var user = new User
        {
            Id = n.UserId,
            BranchId = n.BranchId,
            Email = n.Email,
            FullName = n.FullName,
            Phone = n.Phone,
            Role = n.Role,
            IsActive = n.IsActive,
            PinHash = n.PinHash,
            CreatedAtUtc = n.RegisteredAtUtc,
            OrderCount = 0
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }
}
