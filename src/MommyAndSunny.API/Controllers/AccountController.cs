using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Auth.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Exceptions;
namespace MommyAndSunny.API.Controllers;

[ApiController, Authorize, Route("api/account")]
public class AccountController(IApplicationDbContext db, ICurrentUserService current) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == current.UserId, ct);
        return Ok(new { success = true, data = AuthService.Map(user), message = (string?)null });
    }

    [HttpPatch("me")]
    public async Task<IActionResult> Update(UpdateProfile request, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == current.UserId, ct);
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new DomainException("Họ tên không được để trống.");
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await db.SaveChangesAsync(ct);
        Response.Headers.CacheControl = "no-store";
        return Ok(new { success = true, data = AuthService.Map(user), message = (string?)null });
    }
}

[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProfile([Required, StringLength(150)] string FullName,
    [property: RegularExpression(@"^0\d{9,10}$")] string? PhoneNumber);
