using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SimpleEcommerce.Models;

public class Transaction
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int UserId { get; set; }

	[ForeignKey("UserId")]
	[JsonIgnore]
	public User? User { get; set; }

	[Required]
	[MaxLength(100)]
	public string Type { get; set; } = null!; // "Пополнение" или "Покупка"

	[Required]
	public decimal Amount { get; set; }

	[MaxLength(500)]
	public string? Description { get; set; }

	[Required]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public int? ProductId { get; set; }

	[MaxLength(200)]
	public string ProductName { get; set; } = null!;
}
