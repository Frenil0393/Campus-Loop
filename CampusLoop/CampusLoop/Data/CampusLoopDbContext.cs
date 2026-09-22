using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CampusLoop.Models;
using CampusLoop.Services;

namespace CampusLoop.Data;

public class CampusLoopDbContext : DbContext
{
    public CampusLoopDbContext(DbContextOptions<CampusLoopDbContext> options) : base(options)
    {
    }

    public DbSet<StudentUser> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<DbChatMessage> ChatMessages { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<PaymentOrder> PaymentOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store List<string> ImageUrls as delimited string in SQLite
        var stringListConverter = new ValueConverter<List<string>, string>(
            v => string.Join("|||", v),
            v => v.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries).ToList()
        );

        var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        modelBuilder.Entity<Product>()
            .Property(e => e.ImageUrls)
            .HasConversion(stringListConverter, stringListComparer);

        // Foreign keys configuration
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DbChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DbChatMessage>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void InitializeDatabase()
    {
        Database.EnsureCreated();

        if (!Users.Any())
        {
            // Seed Admin User (R.8)
            var admin = new StudentUser
            {
                Id = "admin_01",
                FullName = "DDU Campus Administrator",
                CollegeEmail = "admin@ddu.ac.in",
                PhoneNumber = "+91 98980 00000",
                Branch = "Administration Office",
                Semester = "Faculty / Staff",
                PasswordHash = "Admin@123",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            Users.Add(admin);

            // Seed Sample Students (R.1)
            var mockStudents = MockDataService.GetStudents();
            foreach (var s in mockStudents)
            {
                s.PasswordHash = "Student@123";
                Users.Add(s);
            }

            // Seed Categories (R.4 & R.11)
            var mockCategories = MockDataService.GetCategories();
            foreach (var c in mockCategories)
            {
                Categories.Add(c);
            }

            SaveChanges();

            // Seed Products (R.2, R.3, R.14)
            var mockProducts = MockDataService.GetProducts();
            foreach (var p in mockProducts)
            {
                p.Category = null;
                p.Seller = null;
                Products.Add(p);
            }

            SaveChanges();

            // Seed sample chat message for casio calculator
            var msg1 = new DbChatMessage
            {
                ProductId = 1,
                SenderId = "std_102",
                ReceiverId = "std_101",
                MessageText = "Hey Aarav! Is the Casio fx-991CW still available?",
                SentAt = DateTime.UtcNow.AddMinutes(-30),
                IsRead = true
            };
            var msg2 = new DbChatMessage
            {
                ProductId = 1,
                SenderId = "std_101",
                ReceiverId = "std_102",
                MessageText = "Yes Priya! It's in mint condition. We can meet near the library.",
                SentAt = DateTime.UtcNow.AddMinutes(-20),
                IsRead = true
            };
            ChatMessages.AddRange(msg1, msg2);
            SaveChanges();
        }
        else
        {
            // Sync existing users to DDU domain & ensure 24ceuos155@ddu.ac.in exists
            var existingUsers = Users.ToList();
            bool modified = false;
            foreach (var u in existingUsers)
            {
                if (u.CollegeEmail.Contains("@gec.ac.in"))
                {
                    u.CollegeEmail = u.CollegeEmail.Replace("@gec.ac.in", "@ddu.ac.in");
                    modified = true;
                }
                if (u.Id == "std_101" && u.CollegeEmail != "24ceuos155@ddu.ac.in")
                {
                    u.CollegeEmail = "24ceuos155@ddu.ac.in";
                    modified = true;
                }
                if (u.Id == "admin_01" && u.CollegeEmail != "admin@ddu.ac.in")
                {
                    u.CollegeEmail = "admin@ddu.ac.in";
                    modified = true;
                }
            }

            // If 24ceuos155@ddu.ac.in still doesn't exist, create it
            if (!Users.Any(u => u.CollegeEmail.ToLower() == "24ceuos155@ddu.ac.in"))
            {
                Users.Add(new StudentUser
                {
                    Id = "std_101",
                    FullName = "Aarav Patel",
                    CollegeEmail = "24ceuos155@ddu.ac.in",
                    PhoneNumber = "+91 98251 44520",
                    Branch = "Computer Engineering",
                    Semester = "5th Semester",
                    PasswordHash = "Student@123",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                modified = true;
            }

            if (modified)
            {
                SaveChanges();
            }
        }
    }
}
