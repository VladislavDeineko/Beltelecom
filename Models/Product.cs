using Beltelecom.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Beltelecom.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [Required]
        public int BrandId { get; set; }
        [ForeignKey("BrandId")]
        public Brand Brand { get; set; }
        public string? ImageUrl { get; set; }
        public int WarrantyMonths { get; set; } // Гарантия в месяцах
        public int InstallmentMonths { get; set; } // Рассрочка в месяцах
        public DateTime CreatedAt { get; set; }
        public List<ProductSpecification> Specifications { get; set; }
    }
}