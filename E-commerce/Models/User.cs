using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
	[Column(TypeName = "decimal(18,2)")]
	public decimal Balance { get; set; } = 0; // Начальный баланс

	[Required]
	public bool WithdrawalEnabled { get; set; } = false; // Разрешен ли вывод средств администратором

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	// Навигационное свойство для транзакций
	[JsonIgnore]
	public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
