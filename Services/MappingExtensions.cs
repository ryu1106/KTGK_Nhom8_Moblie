using Lenguyengiahuy_nhom8.Contracts;
using Lenguyengiahuy_nhom8.Models;

namespace Lenguyengiahuy_nhom8.Services;

public static class MappingExtensions
{
    public static UserResponse ToResponse(this AppUser user) => new(user.Id, user.FullName, user.Email, user.Role, user.IsEmailVerified, user.IsActive, user.AvatarUrl, user.CreatedAtUtc);
}
