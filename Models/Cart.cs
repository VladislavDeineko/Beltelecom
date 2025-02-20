using Beltelecom.Models;
using System;
using System.ComponentModel.DataAnnotations;

public class Cart
{
    [Key]
    public int Id { get; set; }

    // Связь с пользователем, который владеет корзиной
    public int UserId { get; set; }
    public User User { get; set; }

    // Дата создания корзины
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Коллекция элементов в корзине
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
