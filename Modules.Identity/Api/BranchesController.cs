using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Identity.Infrastructure;

namespace Modules.Identity.Api;

[ApiController]
[Route("api/tenants/{tenantId:guid}/branches")]
public class BranchesController : ControllerBase
{
    private readonly AppIdentityDbContext _db;

    public BranchesController(AppIdentityDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid tenantId, [FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var tenantExists = await _db.Tenants.AnyAsync(x => x.Id == tenantId, ct);
        if (!tenantExists)
        {
            return NotFound("Tenant not found.");
        }

        var exists = await _db.Branches.AnyAsync(x => x.TenantId == tenantId && x.Name == request.Name, ct);
        if (exists)
        {
            return Conflict("Branch name already exists for this tenant.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Code = request.Code?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { tenantId, id = branch.Id }, branch);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid tenantId, Guid id, CancellationToken ct)
    {
        var branch = await _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        return branch is null ? NotFound() : Ok(branch);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid tenantId, CancellationToken ct)
    {
        var branches = await _db.Branches.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(branches);
    }

    public sealed record CreateBranchRequest(string Name, string? Code);
}
