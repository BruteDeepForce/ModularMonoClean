using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Orders.Contracts.Events;
using Modules.Orders.Infrastructure;

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
            var order = new Domain.Order
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            //db optimistic lock düşünülebilir, ancak bu örnekte basitçe ekleyip kaydediyoruz
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            await _mediator.Publish(new OrderCreated(order.Id, order.UserId));

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return order is null ? NotFound() : Ok(order);
        }

        public record CreateOrderRequest(Guid UserId);
    }
}