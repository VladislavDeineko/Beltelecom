using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Beltelecom.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<UserDealInfo> UserDealInfos { get; set; }
    public DbSet<Order> Orders { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Связь между продуктом и брендом (One-to-Many)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)  // Продукт связан с одним брендом
            .WithMany(b => b.Products)  // Бренд может иметь много продуктов
            .HasForeignKey(p => p.BrandId);  // Внешний ключ для связи — это BrandId в таблице Product

        // Связь между продуктом и категорией (One-to-Many)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)  // Продукт связан с одной категорией
            .WithMany(c => c.Products)  // Категория может содержать много продуктов
            .HasForeignKey(p => p.CategoryId) // Внешний ключ для связи — это CategoryId в таблице Product
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Product>()
            .Property(p => p.CategoryId)
            .IsRequired();

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(10,2)");  // 10 - общее количество цифр, 2 - количество цифр после запятой

        base.OnModelCreating(modelBuilder);  // Вызов базового метода для выполнения любых дополнительных настроек
    }
}
