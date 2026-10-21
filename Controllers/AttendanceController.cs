using System.Security.Claims;
using Lenguyengiahuy_nhom8.Contracts;
using Lenguyengiahuy_nhom8.Data;
using Lenguyengiahuy_nhom8.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Controllers;

[ApiController, Authorize, Route("api/attendance")]
public sealed class AttendanceController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(CheckInRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        if (!string.IsNullOrWhiteSpace(request.QrCode) && !await db.QrCodes.AnyAsync(x => x.Code == request.QrCode && x.IsActive, ct)) return BadRequest(new { message = "Mã QR/Barcode không hợp lệ hoặc đã bị khóa." });
        var today = DateTime.UtcNow.Date;
        if (await db.AttendanceRecords.AnyAsync(x => x.UserId == userId && x.CheckInAtUtc >= today && x.CheckOutAtUtc == null, ct)) return Conflict(new { message = "Bạn đang có một phiên chấm công chưa check-out." });
        var record = new AttendanceRecord { UserId = userId.Value, WorkShiftId = request.WorkShiftId, CheckInAtUtc = DateTime.UtcNow, CheckInLatitude = request.Latitude, CheckInLongitude = request.Longitude, CheckInQrCode = request.QrCode, FaceVerified = request.FaceVerified, Note = request.Note };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return Ok(record);
    }

    [HttpPost("{id:guid}/check-out")]
    public async Task<IActionResult> CheckOut(Guid id, CheckOutRequest request, CancellationToken ct)
    {
        var record = await db.AttendanceRecords.FindAsync([id], ct);
        if (record is null || (record.UserId != CurrentUserId() && !User.IsInRole("Admin") && !User.IsInRole("Manager"))) return NotFound();
        if (record.CheckOutAtUtc.HasValue) return Conflict(new { message = "Bản ghi đã check-out." });
        record.CheckOutAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note)) record.Note = request.Note;
        await db.SaveChangesAsync(ct);
        return Ok(record);
    }

    [HttpGet("my-history")]
    public async Task<IActionResult> MyHistory(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var start = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = to?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(1);
        return Ok(await db.AttendanceRecords.AsNoTracking().Include(x => x.WorkShift).Where(x => x.UserId == userId && x.CheckInAtUtc >= start && x.CheckInAtUtc < end).OrderByDescending(x => x.CheckInAtUtc).ToListAsync(ct));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("all")]
    public async Task<IActionResult> All(DateTime? date, CancellationToken ct)
    {
        var day = (date ?? DateTime.UtcNow).Date;
        return Ok(await db.AttendanceRecords.AsNoTracking().Include(x => x.User).Include(x => x.WorkShift).Where(x => x.CheckInAtUtc >= day && x.CheckInAtUtc < day.AddDays(1)).OrderBy(x => x.CheckInAtUtc).ToListAsync(ct));
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
