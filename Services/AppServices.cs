using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.IdentityModel.Tokens;
using Lenguyengiahuy_nhom8.Models;

namespace Lenguyengiahuy_nhom8.Services;

public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiresMinutes { get; set; } = 120;
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromName { get; set; } = "Attendance App";
    public bool EnableSsl { get; set; } = true;
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user);
}

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    public (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Role, user.Role.ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

public interface IEmailService
{
    Task SendOtpAsync(string recipient, string code, string purpose, CancellationToken cancellationToken);
}

public sealed class EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;
    public async Task SendOtpAsync(string recipient, string code, string purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.UserName) || _options.UserName.Contains("your-gmail", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("SMTP chưa được cấu hình. Không gửi OTP tới {Email}.", recipient);
            return;
        }
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.UserName));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = $"Mã OTP - {purpose}";
        message.Body = new BodyBuilder { HtmlBody = $"<h2>Attendance App</h2><p>Mã OTP của bạn là:</p><h1>{code}</h1><p>Mã có hiệu lực trong 10 phút.</p>" }.ToMessageBody();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
        await smtp.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}

public static class SecurityHelper
{
    public static string Hash(string value) => BCrypt.Net.BCrypt.HashPassword(value);
    public static bool Verify(string value, string hash) => BCrypt.Net.BCrypt.Verify(value, hash);
    public static string GenerateOtp() => Random.Shared.Next(100000, 1000000).ToString();
}
