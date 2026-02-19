using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Users.Domain;
using Modules.Users.Infrastructure;
using System.Security.Claims;

namespace Modules.Users.Api
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserDbContext _db;
        public UsersController(UserDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            if (!TryGetBranchId(out var branchId))
            {
                return Unauthorized("BranchId is required in claim (branch_id/branchId) or X-Branch-Id header.");
            }

            var user = new User { Id = Guid.NewGuid(), BranchId = branchId, Email = req.Email, OrderCount = 0 };
            user.CreatedAtUtc = DateTime.UtcNow;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetBranchId(out var branchId))
            {
                return Unauthorized("BranchId is required in claim (branch_id/branchId) or X-Branch-Id header.");
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId);
            return user is null ? NotFound() : Ok(user);
        }

        public record CreateUserRequest(string Email);

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
