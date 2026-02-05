namespace SimpleEcommerce.Models;

public class User
{
	public int Id { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public DateTime BirthDate { get; set; }
	public decimal Balance { get; set; } = 10000; // Стартовый баланс

	public string FullName => $"{FirstName} {LastName}";
}
