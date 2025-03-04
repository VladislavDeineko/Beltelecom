using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Beltelecom.Models
{
    public class UserAddress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Region { get; set; } // Область

        [Required]
        public string City { get; set; } // Населенный пункт

        [Required]
        public string Street { get; set; } // Улица

        [Required]
        public string House { get; set; } // Дом

        public string? Apartment { get; set; } // Квартира (необязательно)

        // Внешний ключ для связи с пользователем
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
    }
}
