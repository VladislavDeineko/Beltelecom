using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

public class Cart
{
    [Key]
    public int Id { get; set; }

    // Связь с пользователем, который владеет корзиной
    public int UserId { get; set; }
    public IdentityUser IdentityUser { get; set; }

    // Дата создания корзины
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Коллекция элементов в корзине
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
