using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantMS.Models.Domain;

namespace RestaurantMS.Data;

public class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<MenuItem>      MenuItems      { get; set; }
    public DbSet<Category>      Categories     { get; set; }
    public DbSet<PosOrder>      PosOrders      { get; set; }
    public DbSet<PosOrderItem>  PosOrderItems  { get; set; }
    public DbSet<Payment>       Payments       { get; set; }
    public DbSet<CashHandover>  CashHandovers  { get; set; }
    public DbSet<ShiftTemplate> ShiftTemplates { get; set; }
    public DbSet<EmployeeShift> EmployeeShifts { get; set; }
    public DbSet<PrinterProfile> PrinterProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Global soft-delete query filters ───────────────────────────────
        builder.Entity<MenuItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PosOrder>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PosOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CashHandover>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ShiftTemplate>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<EmployeeShift>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PrinterProfile>().HasQueryFilter(e => !e.IsDeleted);

        // ── Unicode on all string columns ───────────────────────────────────
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties()
                         .Where(p => p.ClrType == typeof(string)))
            {
                prop.SetIsUnicode(true);
            }
        }

        // ── Category ────────────────────────────────────────────────────────
        builder.Entity<Category>(e =>
        {
            e.Property(x => x.NameAr).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.NameEn).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.IconClass).HasColumnType("nvarchar(100)");
        });

        // ── MenuItem ────────────────────────────────────────────────────────
        builder.Entity<MenuItem>(e =>
        {
            e.Property(x => x.NameAr).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.NameEn).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.DescriptionAr).HasColumnType("nvarchar(500)");
            e.Property(x => x.DescriptionEn).HasColumnType("nvarchar(500)");
            e.Property(x => x.Price).HasColumnType("decimal(18,3)");
            e.Property(x => x.ImageUrl).HasColumnType("nvarchar(500)");
            e.Property(x => x.Barcode).HasColumnType("nvarchar(100)");

            e.HasIndex(x => x.Barcode)
             .IsUnique()
             .HasFilter("[Barcode] IS NOT NULL");

            e.HasOne(x => x.Category)
             .WithMany(c => c.MenuItems)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PosOrder ────────────────────────────────────────────────────────
        builder.Entity<PosOrder>(e =>
        {
            e.Property(x => x.OrderNumber).HasColumnType("nvarchar(50)").IsRequired();
            e.Property(x => x.TableNumber).HasColumnType("nvarchar(20)");
            e.Property(x => x.Notes).HasColumnType("nvarchar(500)");
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,3)");
            e.Property(x => x.TaxAmount).HasColumnType("decimal(18,3)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,3)");
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,3)");

            e.HasIndex(x => x.OrderNumber).IsUnique();

            e.HasOne(x => x.Cashier)
             .WithMany()
             .HasForeignKey(x => x.CashierId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PosOrderItem ────────────────────────────────────────────────────
        builder.Entity<PosOrderItem>(e =>
        {
            e.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,3)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,3)");
            e.Property(x => x.Notes).HasColumnType("nvarchar(300)");

            e.HasOne(x => x.Order)
             .WithMany(o => o.OrderItems)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.MenuItem)
             .WithMany(m => m.OrderItems)
             .HasForeignKey(x => x.MenuItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payment ─────────────────────────────────────────────────────────
        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,3)");
            e.Property(x => x.CashReceived).HasColumnType("decimal(18,3)");
            e.Property(x => x.ChangeGiven).HasColumnType("decimal(18,3)");
            e.Property(x => x.ReferenceNumber).HasColumnType("nvarchar(100)");

            e.HasOne(x => x.Order)
             .WithMany(o => o.Payments)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CashHandover ────────────────────────────────────────────────────
        builder.Entity<CashHandover>(e =>
        {
            e.Property(x => x.OpenFloat).HasColumnType("decimal(18,3)");
            e.Property(x => x.CashSalesTotal).HasColumnType("decimal(18,3)");
            e.Property(x => x.CashRefundsTotal).HasColumnType("decimal(18,3)");
            e.Property(x => x.ActualCash).HasColumnType("decimal(18,3)");
            e.Property(x => x.Difference).HasColumnType("decimal(18,3)");
            e.Property(x => x.CashierNotes).HasColumnType("nvarchar(500)");
            e.Property(x => x.ManagerNotes).HasColumnType("nvarchar(500)");

            // ExpectedCash is a computed C# property — ignore in DB
            e.Ignore(x => x.ExpectedCash);

            e.HasOne(x => x.Cashier)
             .WithMany()
             .HasForeignKey(x => x.CashierId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Manager)
             .WithMany()
             .HasForeignKey(x => x.ManagerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ShiftTemplate ───────────────────────────────────────────────────
        builder.Entity<ShiftTemplate>(e =>
        {
            e.Property(x => x.NameAr).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.NameEn).HasColumnType("nvarchar(200)").IsRequired();
        });

        // ── EmployeeShift ───────────────────────────────────────────────────
        builder.Entity<EmployeeShift>(e =>
        {
            e.Property(x => x.Notes).HasColumnType("nvarchar(500)");

            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Template)
             .WithMany(t => t.EmployeeShifts)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Manager)
             .WithMany()
             .HasForeignKey(x => x.ManagerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PrinterProfile ──────────────────────────────────────────────────
        builder.Entity<PrinterProfile>(e =>
        {
            e.Property(x => x.Name).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.IpAddress).HasColumnType("nvarchar(50)");
        });

        // ── ApplicationUser extra columns ───────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullNameAr).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.FullNameEn).HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.ManagerPin).HasColumnType("nvarchar(200)");
        });

        // ── Seed Roles ──────────────────────────────────────────────────────
        var roles = new[]
        {
            new IdentityRole<int> { Id = 1, Name = "SuperAdmin",    NormalizedName = "SUPERADMIN"    },
            new IdentityRole<int> { Id = 2, Name = "Manager",       NormalizedName = "MANAGER"       },
            new IdentityRole<int> { Id = 3, Name = "Cashier",       NormalizedName = "CASHIER"       },
            new IdentityRole<int> { Id = 4, Name = "Waiter",        NormalizedName = "WAITER"        },
            new IdentityRole<int> { Id = 5, Name = "KitchenStaff",  NormalizedName = "KITCHENSTAFF"  },
            new IdentityRole<int> { Id = 6, Name = "Accountant",    NormalizedName = "ACCOUNTANT"    },
        };
        builder.Entity<IdentityRole<int>>().HasData(roles);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
