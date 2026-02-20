using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SimpleEcommerce.Models;

public class PendingRequest
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int UserId { get; set; }

	[ForeignKey("UserId")]
	[JsonIgnore]
	public User User { get; set; } = null!;

	[Required]
	[MaxLength(50)]
	public string Type { get; set; } = null!; // "TopUp" или "Withdrawal"

	[Required]
	[Column(TypeName = "decimal(18,2)")]
	public decimal Amount { get; set; }

	[MaxLength(200)]
	public string? Bank { get; set; }

	[MaxLength(100)]
	public string? CardOrPhone { get; set; }

	[Required]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[Required]
	public bool IsCompleted { get; set; } = false;
}
