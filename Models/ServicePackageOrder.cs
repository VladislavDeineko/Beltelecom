using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Beltelecom.Models
{
    public class ServicePackageOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public int ServicePackageId { get; set; }

        [ForeignKey("ServicePackageId")]
        public ServicePackage ServicePackage { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerMonth { get; set; } // Стоимость подписки в месяц

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedDate { get; set; }
        public bool IsConfirmed { get; set; } = false;
    }
}
