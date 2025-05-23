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

        public string? LastName { get; set; } // Фамилия

        public string? FirstName { get; set; } // Имя

        public string? MiddleName { get; set; } // Отчество

        [RegularExpression(@"^[A-Z]{2}\d{7}$", ErrorMessage = "Формат паспорта: YYXXXXXXX")]
        public string? PassportNumber { get; set; } // Серия и номер паспорта (YYXXXXXXX)

        public DateTime? PassportIssuedDate { get; set; } // Когда выдан

        public string? PassportIssuedBy { get; set; } // Кем выдан

        [RegularExpression(@"^\d{7}[A-Z]\d{3}[A-Z]{2}\d$", ErrorMessage = "Формат личного номера: XXXXXXXYXXXYYX")]
        public string? PersonalNumber { get; set; } // Личный номер (XXXXXXXYXXXYYX)

        public DateTime? PassportExpiryDate { get; set; } // Срок действия паспорта

        [RegularExpression(@"^\+375\d{9}$", ErrorMessage = "Формат телефона: +375YYXXXXXXX")]
        public string? PhoneNumber { get; set; } // Телефон (+375YYXXXXXXX)

        // Внешний ключ для связи с пользователем
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        public int? ServicePackageId { get; set; }

        [ForeignKey("ServicePackageId")]
        public ServicePackage? ServicePackage { get; set; }
    }
}
