using System.ComponentModel.DataAnnotations;

public class CartItem
{
    [Key]
    public int Id { get; set; }

    // Связь с корзиной
    public int CartId { get; set; }
    public Cart Cart { get; set; }

    // Связь с товаром
    public int ProductId { get; set; }
    public Product Product { get; set; }

    // Количество товара в корзине
    public int Quantity { get; set; }

}
