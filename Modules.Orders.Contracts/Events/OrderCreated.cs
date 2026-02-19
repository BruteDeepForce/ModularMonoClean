using MediatR;

namespace Modules.Orders.Contracts.Events;

public record OrderCreated(Guid OrderId, Guid UserId, Guid BranchId) : INotification;
