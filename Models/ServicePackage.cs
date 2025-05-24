using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Beltelecom.Models
{
    public class ServicePackage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // Название пакета
        public string? Internet { get; set; }
        public string? PhoneServices { get; set; }
        public string? TVServices { get; set; }
        public string? VideoMonitoring { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; } // Тариф с учетом НДС
    }

}
