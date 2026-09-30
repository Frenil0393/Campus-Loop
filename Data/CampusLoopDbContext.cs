using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Models;

namespace CampusLoop.Data;

public class CampusLoopDbContext : IdentityDbContext<ApplicationUser>
{
    public CampusLoopDbContext(DbContextOptions<CampusLoopDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique index for wishlist per student per product
        builder.Entity<Wishlist>()
            .HasIndex(w => new { w.StudentId, w.ProductId })
            .IsUnique();

        // Unique index for category name
        builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();
    }
}
