using BCrypt.Net;
using Lenguyengiahuy_nhom8.Contracts;
using Lenguyengiahuy_nhom8.Data;
using Lenguyengiahuy_nhom8.Models;
using Lenguyengiahuy_nhom8.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Controllers;

[ApiController, Authorize(Roles = "Admin,Manager"), Route("api/admin")]
public sealed class AdminController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users(UserRole? role, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (role.HasValue) query = query.Where(x => x.Role == role);
        return Ok((await query.OrderBy(x => x.FullName).ToListAsync(ct)).Select(x => x.ToResponse()));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.AvatarUrl is not null) user.AvatarUrl = request.AvatarUrl;
        await db.SaveChangesAsync(ct);
        return Ok(user.ToResponse());
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(CancellationToken ct) => Ok(await db.WorkShifts.AsNoTracking().OrderBy(x => x.StartTime).ToListAsync(ct));

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift(CreateShiftRequest request, CancellationToken ct)
    {
        var shift = new WorkShift { Name = request.Name.Trim(), StartTime = request.StartTime, EndTime = request.EndTime };
        db.WorkShifts.Add(shift);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetShifts), new { id = shift.Id }, shift);
    }

    [HttpPut("shifts/{id:guid}")]
    public async Task<IActionResult> UpdateShift(Guid id, UpdateShiftRequest request, CancellationToken ct)
    {
        var shift = await db.WorkShifts.FindAsync([id], ct);
        if (shift is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Name)) shift.Name = request.Name.Trim();
        if (request.StartTime.HasValue) shift.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) shift.EndTime = request.EndTime.Value;
        if (request.IsActive.HasValue) shift.IsActive = request.IsActive.Value;
        await db.SaveChangesAsync(ct);
        return Ok(shift);
    }

    [HttpDelete("shifts/{id:guid}")]
    public async Task<IActionResult> DeleteShift(Guid id, CancellationToken ct)
    {
        var shift = await db.WorkShifts.FindAsync([id], ct);
        if (shift is null) return NotFound();
        db.WorkShifts.Remove(shift);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
