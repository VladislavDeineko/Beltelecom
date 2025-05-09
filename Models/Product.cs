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
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Цена, руб.")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "В наличии, шт.)")]
        public int Stock { get; set; }

        [Required]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Required]
        [Display(Name = "Бренд")]
        public int BrandId { get; set; }

        [ForeignKey("BrandId")]
        public Brand? Brand { get; set; }

        [Display(Name = "Изображение")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Гарантия, мес.")]
        public int WarrantyMonths { get; set; }

        [Display(Name = "Рассрочка, мес.")]
        public int InstallmentMonths { get; set; }

        [Display(Name = "Дата добавления")]
        public DateTime CreatedAt { get; set; }
        public List<ProductSpecification>? Specifications { get; set; }
    }
}