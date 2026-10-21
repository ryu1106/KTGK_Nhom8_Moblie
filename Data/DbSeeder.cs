using BCrypt.Net;
using Lenguyengiahuy_nhom8.Models;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IConfiguration configuration)
    {
        await db.Database.EnsureCreatedAsync();
        if (!await db.Users.AnyAsync())
        {
            var email = configuration["SeedAdmin:Email"] ?? "admin@example.com";
            var password = configuration["SeedAdmin:Password"] ?? "Admin@12345";
            db.Users.Add(new AppUser { FullName = "System Administrator", Email = email.ToLowerInvariant(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = UserRole.Admin, IsEmailVerified = true });
        }
        if (!await db.WorkShifts.AnyAsync())
            db.WorkShifts.AddRange(new WorkShift { Name = "Ca hành chính", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0) });
        await db.SaveChangesAsync();
    }
}
