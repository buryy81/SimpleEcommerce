using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleEcommerce.Models;

[Table("BlockedIps")]
public class BlockedIp
{
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(45)]
	public string Ip { get; set; } = null!;

	[MaxLength(500)]
	public string? Comment { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
