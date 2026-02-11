using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Orders.Contracts.Events;
using Modules.Users.Infrastructure;

namespace Modules.Users.Application
{
    public class OrderCreatedHandler : INotificationHandler<Modules.Orders.Contracts.Events.OrderCreated>
    {
        private readonly UserDbContext _db;
        public OrderCreatedHandler(UserDbContext db) => _db = db;

        public async Task Handle(OrderCreated n, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == n.UserId, ct);
            if (user is null) return;

            user.OrderCount += 1;
            await _db.SaveChangesAsync(ct);
        }
    }
}