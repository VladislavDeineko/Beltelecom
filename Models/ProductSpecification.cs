using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Beltelecom.Models
{
    public class ProductSpecification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        public string Name { get; set; } // Название характеристики

        [Required]
        public string Value { get; set; } // Значение характеристики
        public string Group { get; set; }
    }

}
