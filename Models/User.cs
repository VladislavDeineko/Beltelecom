using Beltelecom.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } // Хэш пароля (не храните пароли в открытом виде)

    public int RoleId { get; set; }
    public Role Role { get; set; } // Роль пользователя (например, "admin", "user")

    // Дополнительные поля, если нужно, например, имя, дата регистрации и т.д.
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateRegistered { get; set; }

    // Если пользователь может быть связан с корзиной
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
}
