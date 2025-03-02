using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

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
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public string ItemsJson { get; set; } // JSON-строка для хранения списка товаров

        [NotMapped]
        public List<OrderItem> Items
        {
            get => string.IsNullOrEmpty(ItemsJson) ? new List<OrderItem>() : JsonSerializer.Deserialize<List<OrderItem>>(ItemsJson);
            set => ItemsJson = JsonSerializer.Serialize(value);
        }
    }
}