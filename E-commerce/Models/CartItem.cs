namespace SimpleEcommerce.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? StoreName { get; set; }
    public int Quantity { get; set; } = 1;

    public decimal Total => Price * Quantity;
}
