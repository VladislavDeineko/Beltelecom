using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Beltelecom.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public bool RequiresSetup { get; set; } // Требуется настройка

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } // Сумма заказа

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaidAmount { get; set; } // Выплаченная сумма

        public int InstallmentMonths { get; set; } // Рассрочка (в месяцах)

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow; // Дата заказа

        public bool IsActive { get; set; } = false; // Активен
    }
}
