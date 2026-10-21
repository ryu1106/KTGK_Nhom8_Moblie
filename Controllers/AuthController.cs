using Lenguyengiahuy_nhom8.Contracts;
using Lenguyengiahuy_nhom8.Data;
using Lenguyengiahuy_nhom8.Models;
using Lenguyengiahuy_nhom8.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(ApplicationDbContext db, IEmailService email, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.Password.Length < 8)
            return BadRequest(new { message = "Họ tên bắt buộc và mật khẩu phải có ít nhất 8 ký tự." });
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, ct)) return Conflict(new { message = "Email đã được đăng ký." });
        var role = request.Role is UserRole.Customer ? UserRole.Customer : UserRole.Employee;
        var user = new AppUser { FullName = request.FullName.Trim(), Email = normalizedEmail, PasswordHash = SecurityHelper.Hash(request.Password), Role = role };
        db.Users.Add(user);
        await CreateAndSendOtpAsync(user, "Registration", ct);
        await db.SaveChangesAsync(ct);
        return Accepted(new { message = "Đăng ký thành công. Vui lòng kiểm tra email để lấy OTP.", email = user.Email });
    }

    [HttpPost("verify-registration")]
    public async Task<IActionResult> VerifyRegistration(VerifyOtpRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLower(), ct);
        if (user is null) return NotFound(new { message = "Không tìm thấy tài khoản." });
        var valid = await ValidateOtpAsync(user.Email, request.Code, "Registration", ct);
        if (!valid) return BadRequest(new { message = "OTP không đúng, đã hết hạn hoặc đã được sử dụng." });
        user.IsEmailVerified = true;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Xác minh email thành công." });
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest request, CancellationToken ct)
    {
        var emailAddress = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == emailAddress, ct);
        if (user is null) return Ok(new { message = "Nếu email tồn tại, OTP sẽ được gửi lại." });
        await CreateAndSendOtpAsync(emailAddress, user.Id, request.Purpose, ct);
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "OTP đã được gửi lại." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLower(), ct);
        if (user is null || !user.IsActive || !SecurityHelper.Verify(request.Password, user.PasswordHash)) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        if (!user.IsEmailVerified) return BadRequest(new { message = "Email chưa được xác minh." });
        await CreateAndSendOtpAsync(user, "Login", ct);
        await db.SaveChangesAsync(ct);
        return Accepted(new { message = "Mật khẩu đúng. Vui lòng nhập OTP được gửi qua email.", email = user.Email });
    }

    [HttpPost("verify-login")]
    public async Task<IActionResult> VerifyLogin(VerifyLoginOtpRequest request, CancellationToken ct)
    {
        var emailAddress = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == emailAddress, ct);
        if (user is null || !user.IsActive || !await ValidateOtpAsync(emailAddress, request.Code, "Login", ct)) return Unauthorized(new { message = "OTP không hợp lệ." });
        var token = tokens.CreateToken(user);
        return Ok(new AuthResponse(token.Token, token.ExpiresAtUtc, user.ToResponse()));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    private async Task<AppUser?> GetCurrentUserAsync(CancellationToken ct)
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var userId) ? await db.Users.FindAsync([userId], ct) : null;
    }

    private async Task CreateAndSendOtpAsync(AppUser user, string purpose, CancellationToken ct) => await CreateAndSendOtpAsync(user.Email, user.Id, purpose, ct);
    private async Task CreateAndSendOtpAsync(string emailAddress, Guid userId, string purpose, CancellationToken ct)
    {
        var code = SecurityHelper.GenerateOtp();
        db.EmailOtps.Add(new EmailOtp { Email = emailAddress, UserId = userId, Purpose = purpose, CodeHash = SecurityHelper.Hash(code), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10) });
        await email.SendOtpAsync(emailAddress, code, purpose, ct);
    }

    private async Task<bool> ValidateOtpAsync(string emailAddress, string code, string purpose, CancellationToken ct)
    {
        var otp = await db.EmailOtps.Where(x => x.Email == emailAddress && x.Purpose == purpose && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (otp is null || !SecurityHelper.Verify(code, otp.CodeHash)) return false;
        otp.UsedAtUtc = DateTime.UtcNow;
        return true;
    }
}
