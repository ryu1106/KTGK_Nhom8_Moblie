using Lenguyengiahuy_nhom8.Models;
using Microsoft.EntityFrameworkCore;

namespace Lenguyengiahuy_nhom8.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LocationShare> LocationShares => Set<LocationShare>();
    public DbSet<FaceProfile> FaceProfiles => Set<FaceProfile>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<AppUser>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<AppUser>().Property(x => x.Email).HasMaxLength(320);
        modelBuilder.Entity<EmailOtp>().HasIndex(x => new { x.Email, x.Purpose, x.CreatedAtUtc });
        modelBuilder.Entity<EmailOtp>().HasOne(x => x.User).WithMany(x => x.OtpCodes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttendanceRecord>().HasOne(x => x.User).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttendanceRecord>().HasOne(x => x.WorkShift).WithMany().HasForeignKey(x => x.WorkShiftId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<LocationShare>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FaceProfile>().HasOne(x => x.User).WithOne().HasForeignKey<FaceProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QrCode>().HasIndex(x => x.Code).IsUnique();
    }
}
