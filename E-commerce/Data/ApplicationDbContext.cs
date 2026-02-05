using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users { get; set; }
	public DbSet<Transaction> Transactions { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Настройка автоинкремента для User.Id
		modelBuilder.Entity<User>()
			.Property(u => u.Id)
			.ValueGeneratedOnAdd();

		// Настройка индекса для Email (уникальность)
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();

		// Настройка BirthDate для сохранения как UTC
		modelBuilder.Entity<User>()
			.Property(u => u.BirthDate)
			.HasConversion(
				v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
				v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

		// Настройка автоинкремента для Transaction.Id
		modelBuilder.Entity<Transaction>()
			.Property(t => t.Id)
			.ValueGeneratedOnAdd();

		// Настройка CreatedAt для сохранения как UTC
		modelBuilder.Entity<Transaction>()
			.Property(t => t.CreatedAt)
			.HasConversion(
				v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
				v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

		// Настройка связей
		modelBuilder.Entity<Transaction>()
			.HasOne(t => t.User)
			.WithMany(u => u.Transactions)
			.HasForeignKey(t => t.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
