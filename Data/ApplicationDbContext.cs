using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Beltelecom.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }

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

        // Связь между товаром в корзине и корзиной (One-to-Many)
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)  // Товар в корзине связан с одной корзиной
            .WithMany(c => c.CartItems)  // Корзина может содержать много товаров
            .HasForeignKey(ci => ci.CartId)  // Внешний ключ для связи — это CartId в таблице CartItem
            .OnDelete(DeleteBehavior.Cascade);  // Каскадное удаление: при удалении корзины, все её товары удаляются

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(10,2)");  // 10 - общее количество цифр, 2 - количество цифр после запятой

        base.OnModelCreating(modelBuilder);  // Вызов базового метода для выполнения любых дополнительных настроек
    }


}
