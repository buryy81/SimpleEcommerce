using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleEcommerce.Models;

[Table("Users")]
public class User
{
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(255)]
	[EmailAddress]
	public string Email { get; set; }

	[Required]
	[MaxLength(255)]
	public string Password { get; set; }

	[Required]
	[MaxLength(100)]
	public string FirstName { get; set; }

	[Required]
	[MaxLength(100)]
	public string LastName { get; set; }

	[Required]
	public DateTime BirthDate { get; set; }

	[Required]
	[Column(TypeName = "decimal(18,2)")]
	public decimal Balance { get; set; } = 10000; // Стартовый баланс

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	// Навигационное свойство для транзакций
	public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
