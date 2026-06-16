using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<MonthlyTrackingEntry> MonthlyTrackingEntries => Set<MonthlyTrackingEntry>();
    public DbSet<MaterialTrackingEntry> MaterialTrackingEntries => Set<MaterialTrackingEntry>();
    public DbSet<TrackingProject> TrackingProjects => Set<TrackingProject>();
    public DbSet<TrackingVendor> TrackingVendors => Set<TrackingVendor>();
    public DbSet<MaterialInvoiceItem> MaterialInvoiceItems => Set<MaterialInvoiceItem>();
    public DbSet<ImpressWeeklyAmount> ImpressWeeklyAmounts => Set<ImpressWeeklyAmount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.EmployeeId);
            e.HasIndex(x => x.EmployeeName);
        });

        modelBuilder.Entity<Payroll>(p =>
        {
            p.HasKey(x => x.PayrollId);
            p.HasOne(x => x.Employee)
                .WithMany(e => e.Payrolls)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            p.HasIndex(x => new { x.EmployeeId, x.Month, x.Year }).IsUnique();
        });

        modelBuilder.Entity<Attendance>(a =>
        {
            a.HasKey(x => x.AttendanceId);
            a.HasOne(x => x.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            a.HasIndex(x => new { x.EmployeeId, x.Month, x.Year }).IsUnique();
        });

        modelBuilder.Entity<Invoice>(i =>
        {
            i.HasKey(x => x.InvoiceId);
            i.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            i.Property(x => x.StoredFileName).HasMaxLength(80).IsRequired();
            i.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            i.Property(x => x.ExtractionStatus).HasMaxLength(20).IsRequired();
            i.Property(x => x.InvoiceNumber).HasMaxLength(80);
            i.Property(x => x.VendorName).HasMaxLength(200);
            i.Property(x => x.ProjectName).HasMaxLength(200);
            i.HasMany(x => x.MaterialItems)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            i.Property(x => x.SgstAmount).HasColumnType("decimal(18,2)");
            i.Property(x => x.CgstAmount).HasColumnType("decimal(18,2)");
            i.Property(x => x.IgstAmount).HasColumnType("decimal(18,2)");
            i.Property(x => x.TransportCharges).HasColumnType("decimal(18,2)");
            i.Property(x => x.BasicTotal).HasColumnType("decimal(18,2)");
            i.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            i.HasIndex(x => x.UploadedAtUtc);
        });

        modelBuilder.Entity<MonthlyTrackingEntry>(m =>
        {
            m.HasKey(x => x.MonthlyTrackingId);
            m.Property(x => x.ProjectSiteName).HasMaxLength(200).IsRequired();
            m.Property(x => x.WorkDescription).HasMaxLength(500);
            m.Property(x => x.Status).HasMaxLength(50).IsRequired();
            m.Property(x => x.Remarks).HasMaxLength(300);
            m.HasIndex(x => new { x.Month, x.Year });
        });

        modelBuilder.Entity<MaterialTrackingEntry>(m =>
        {
            m.HasKey(x => x.MaterialTrackingId);
            m.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
            m.Property(x => x.Unit).HasMaxLength(30).IsRequired();
            m.Property(x => x.SupplierName).HasMaxLength(200);
            m.Property(x => x.Remarks).HasMaxLength(300);
            m.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            m.Property(x => x.UnitRate).HasColumnType("decimal(18,2)");
            m.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            m.HasIndex(x => new { x.Month, x.Year });
        });

        modelBuilder.Entity<TrackingProject>(p =>
        {
            p.HasKey(x => x.ProjectId);
            p.Property(x => x.ProjectName).HasMaxLength(200).IsRequired();
            p.HasIndex(x => x.ProjectName).IsUnique();
        });

        modelBuilder.Entity<TrackingVendor>(v =>
        {
            v.HasKey(x => x.VendorId);
            v.Property(x => x.VendorName).HasMaxLength(200).IsRequired();
            v.HasIndex(x => x.VendorName).IsUnique();
        });

        modelBuilder.Entity<MaterialInvoiceItem>(m =>
        {
            m.HasKey(x => x.MaterialInvoiceItemId);
            m.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
            m.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            m.HasIndex(x => new { x.InvoiceId, x.MaterialName });
        });

        modelBuilder.Entity<ImpressWeeklyAmount>(w =>
        {
            w.HasKey(x => x.ImpressWeeklyAmountId);
            w.HasOne(x => x.Payroll)
                .WithOne()
                .HasForeignKey<ImpressWeeklyAmount>(x => x.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);
            w.HasIndex(x => x.PayrollId).IsUnique();
            w.Property(x => x.Week1).HasColumnType("decimal(18,2)");
            w.Property(x => x.Week2).HasColumnType("decimal(18,2)");
            w.Property(x => x.Week3).HasColumnType("decimal(18,2)");
            w.Property(x => x.Week4).HasColumnType("decimal(18,2)");
        });
    }
}
