using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string? ImageUrl { get; set; } // Путь к изображению категории (если есть)

    public ICollection<Product> Products { get; set; } = new List<Product>(); // Связь с продуктами
}
