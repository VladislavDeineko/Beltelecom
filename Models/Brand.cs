using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Brand
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string? ImageUrl { get; set; } // Если у бренда есть картинка

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
