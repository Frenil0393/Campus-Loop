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

        // Unique index for category name
        builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // Product relationships & indexes
        builder.Entity<Product>(entity =>
        {
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.SellerId);
            entity.HasIndex(p => p.CategoryId);
        });

        // ProductImage relationships
        builder.Entity<ProductImage>(entity =>
        {
            entity.HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Wishlist relationships & indexes
        builder.Entity<Wishlist>(entity =>
        {
            entity.HasIndex(w => new { w.StudentId, w.ProductId })
                .IsUnique();

            entity.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.Student)
                .WithMany()
                .HasForeignKey(w => w.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ChatConversation relationships & indexes
        builder.Entity<ChatConversation>(entity =>
        {
            entity.HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Seller)
                .WithMany()
                .HasForeignKey(c => c.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.ProductId);
        });

        // ChatMessage relationships & indexes
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.ConversationId);
        });

        // Order relationships & indexes
        builder.Entity<Order>(entity =>
        {
            entity.HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Seller)
                .WithMany()
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => o.BuyerId);
            entity.HasIndex(o => o.SellerId);
            entity.HasIndex(o => o.TransactionId)
                .IsUnique();
            entity.HasIndex(o => o.ProductId)
                .IsUnique();
        });
    }
}
