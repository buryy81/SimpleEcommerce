namespace SimpleEcommerce.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? StoreName { get; set; } // Название магазина-продавца (только для отображения, не хранится в БД)
}
