using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Beltelecom.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Внешний ключ для пользователя

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public int ProductId { get; set; } // Внешний ключ для товара

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
