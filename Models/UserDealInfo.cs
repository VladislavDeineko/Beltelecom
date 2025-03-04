using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Beltelecom.Models
{
    public class UserDealInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string LastName { get; set; } // Фамилия

        [Required]
        public string FirstName { get; set; } // Имя

        [Required]
        public string MiddleName { get; set; } // Отчество

        [Required]
        [RegularExpression(@"^[A-Z]{2}\d{7}$", ErrorMessage = "Формат паспорта: YYXXXXXXX")]
        public string PassportNumber { get; set; } // Серия и номер паспорта (YYXXXXXXX)

        [Required]
        public DateTime PassportIssuedDate { get; set; } // Когда выдан

        [Required]
        public string PassportIssuedBy { get; set; } // Кем выдан

        [Required]
        [RegularExpression(@"^\d{7}[A-Z]\d{3}[A-Z]{2}\d$", ErrorMessage = "Формат личного номера: XXXXXXXYXXXYYX")]
        public string PersonalNumber { get; set; } // Личный номер (XXXXXXXYXXXYYX)

        [Required]
        public DateTime PassportExpiryDate { get; set; } // Срок действия паспорта

        [Required]
        [RegularExpression(@"^\+375\d{9}$", ErrorMessage = "Формат телефона: +375YYXXXXXXX")]
        public string PhoneNumber { get; set; } // Телефон (+375YYXXXXXXX)

        // Внешний ключ для связи с пользователем
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
    }
}
