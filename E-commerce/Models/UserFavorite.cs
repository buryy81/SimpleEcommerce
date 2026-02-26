using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleEcommerce.Models;

[Table("UserFavorites")]
public class UserFavorite
{
	[Key]
	public int Id { get; set; }

	public int UserId { get; set; }
	public int ProductId { get; set; }

	[ForeignKey(nameof(UserId))]
	public virtual User User { get; set; } = null!;
}
