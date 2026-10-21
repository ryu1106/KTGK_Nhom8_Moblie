using Lenguyengiahuy_nhom8.Models;

namespace Lenguyengiahuy_nhom8.Contracts;

public sealed record RegisterRequest(string FullName, string Email, string Password, UserRole Role = UserRole.Employee);
public sealed record VerifyOtpRequest(string Email, string Code, string Purpose = "Registration");
public sealed record LoginRequest(string Email, string Password);
public sealed record VerifyLoginOtpRequest(string Email, string Code);
public sealed record ResendOtpRequest(string Email, string Purpose = "Registration");
public sealed record UserResponse(Guid Id, string FullName, string Email, UserRole Role, bool IsEmailVerified, bool IsActive, string? AvatarUrl, DateTime CreatedAtUtc);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
public sealed record UpdateUserRequest(string? FullName, UserRole? Role, bool? IsActive, string? AvatarUrl);
public sealed record CreateShiftRequest(string Name, TimeSpan StartTime, TimeSpan EndTime);
public sealed record UpdateShiftRequest(string? Name, TimeSpan? StartTime, TimeSpan? EndTime, bool? IsActive);
public sealed record CheckInRequest(Guid? WorkShiftId, double? Latitude, double? Longitude, string? QrCode, bool FaceVerified, string? Note);
public sealed record CheckOutRequest(string? Note);
public sealed record LocationRequest(double Latitude, double Longitude, double? AccuracyMeters, bool IsLive);
public sealed record FaceEnrollmentRequest(string TemplateReference);
public sealed record CreateQrRequest(string Code, string Name, string? Location);
