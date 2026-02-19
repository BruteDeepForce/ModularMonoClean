using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Identity.Application;
using Modules.Identity.DTO;

namespace Modules.Identity.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {

        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request, CancellationToken.None);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(result.Data);
    }

    [HttpPost("seed-roles")]
    public async Task<IActionResult> SeedRoles()
    {
        var result = await _authService.SeedRolesAsync();
        if (!result.Succeeded)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("manager-create-user")]
    [Authorize(Roles = "manager,director")]
    public async Task<IActionResult> ManagerCreateUser([FromBody] ManagerCreateUserRequest request, CancellationToken ct)
    {
        var result = await _authService.ManagerCreateUserAsync(request, ct);
        if (!result.Succeeded)
        {
            if (result.Error == "Phone number already registered.")
            {
                return Conflict(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("phone-first-login")]
    public async Task<IActionResult> PhoneFirstLogin([FromBody] PhoneLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.FirstPhoneLoginAsync(request, ct);
        if (!result.Succeeded)
        {
            if (result.Error is "Unauthorized" or "Invalid or expired code.")
            {
                return Unauthorized(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpPost("phone-set-pin")] //authorize olmalı yoksa herkes herkesin telefonuna pin koyabilir. hatta direkt token claimdan phone number alıcaz
    public async Task<IActionResult> PhoneSetPin([FromBody] SetPinRequest request, CancellationToken ct)
    {
        var result = await _authService.SetPinAsync(request, ct);
        if (!result.Succeeded)
        {
            if (result.Error is "Unauthorized")
            {
                return Unauthorized(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }


    // DTOs moved to Modules.Identity.DTO
}
