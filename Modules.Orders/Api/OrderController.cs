using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Orders.Contracts.Events;
using Modules.Orders.Infrastructure;
using System.Security.Claims;

namespace Modules.Orders.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _db;
        private readonly IMediator _mediator;

        public OrderController(OrderDbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            if (!TryGetBranchId(out var branchId))
            {
                return Unauthorized("BranchId is required in claim (branch_id/branchId) or X-Branch-Id header.");
            }

            var order = new Domain.Order
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                UserId = req.UserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            //db optimistic lock düşünülebilir, ancak bu örnekte basitçe ekleyip kaydediyoruz
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            await _mediator.Publish(new OrderCreated(order.Id, order.UserId, order.BranchId));

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetBranchId(out var branchId))
            {
                return Unauthorized("BranchId is required in claim (branch_id/branchId) or X-Branch-Id header.");
            }

            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId);
            return order is null ? NotFound() : Ok(order);
        }

        public record CreateOrderRequest(Guid UserId);

        private bool TryGetBranchId(out Guid branchId)
        {
            branchId = Guid.Empty;
            var claimValue =
                User.FindFirstValue("branch_id") ??
                User.FindFirstValue("branchId");

            if (string.IsNullOrWhiteSpace(claimValue))
            {
                if (!Request.Headers.TryGetValue("X-Branch-Id", out var headerValue))
                {
                    return false;
                }

                claimValue = headerValue.ToString();
            }

            return Guid.TryParse(claimValue, out branchId);
        }
    }
}
