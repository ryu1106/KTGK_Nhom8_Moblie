using System.Security.Claims;
using Lenguyengiahuy_nhom8.Contracts;
using Lenguyengiahuy_nhom8.Data;
using Lenguyengiahuy_nhom8.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Controllers;

[ApiController, Authorize, Route("api/features")]
public sealed class FeaturesController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("location")]
    public async Task<IActionResult> ShareLocation(LocationRequest request, CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var location = new LocationShare { UserId = id.Value, Latitude = request.Latitude, Longitude = request.Longitude, AccuracyMeters = request.AccuracyMeters, IsLive = request.IsLive };
        db.LocationShares.Add(location);
        await db.SaveChangesAsync(ct);
        return Ok(location);
    }

    [HttpGet("location/{userId:guid}")]
    public async Task<IActionResult> GetLocation(Guid userId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && CurrentUserId() != userId) return Forbid();
        var location = await db.LocationShares.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.SharedAtUtc).FirstOrDefaultAsync(ct);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost("face/enroll")]
    public async Task<IActionResult> EnrollFace(FaceEnrollmentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateReference)) return BadRequest(new { message = "TemplateReference bắt buộc." });
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var profile = await db.FaceProfiles.SingleOrDefaultAsync(x => x.UserId == id, ct);
        if (profile is null) db.FaceProfiles.Add(new FaceProfile { UserId = id.Value, TemplateReference = request.TemplateReference });
        else { profile.TemplateReference = request.TemplateReference; profile.EnrolledAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã lưu đăng ký khuôn mặt.", faceVerified = true });
    }

    [HttpGet("face/status")]
    public async Task<IActionResult> FaceStatus(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        return Ok(new { enrolled = await db.FaceProfiles.AnyAsync(x => x.UserId == id, ct) });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("qr")]
    public async Task<IActionResult> GetQrCodes(CancellationToken ct) => Ok(await db.QrCodes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [Authorize(Roles = "Admin")]
    [HttpPost("qr")]
    public async Task<IActionResult> CreateQr(CreateQrRequest request, CancellationToken ct)
    {
        if (await db.QrCodes.AnyAsync(x => x.Code == request.Code, ct)) return Conflict(new { message = "Mã QR/Barcode đã tồn tại." });
        var qr = new QrCode { Code = request.Code.Trim(), Name = request.Name.Trim(), Location = request.Location };
        db.QrCodes.Add(qr);
        await db.SaveChangesAsync(ct);
        return Created("api/features/qr", qr);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("qr/{id:guid}")]
    public async Task<IActionResult> DeleteQr(Guid id, CancellationToken ct)
    {
        var qr = await db.QrCodes.FindAsync([id], ct);
        if (qr is null) return NotFound();
        db.QrCodes.Remove(qr);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
