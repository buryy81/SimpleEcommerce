using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;
using System.Text.Json;
using System;

namespace SimpleEcommerce.Controllers;

// имитация работы с реальными платежами для тестирования потенциального сайта и работы с настоящим балансом
public class AdminController : Controller
{
	private readonly ApplicationDbContext _context;
	private const string AdminEmail = "admin@admin.com"; // Email администратора

	public AdminController(ApplicationDbContext context)
	{
		_context = context;
	}

	// Проверка, является ли текущий пользователь администратором
	private bool IsAdmin()
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return false;

		var user = JsonSerializer.Deserialize<User>(userJson);
		return user?.Email == AdminEmail;
	}

	// GET: /Admin
	public async Task<IActionResult> Index(string search = "")
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		IQueryable<User> usersQuery = _context.Users;

		// Поиск по ID, Email, имени или фамилии
		if (!string.IsNullOrEmpty(search))
		{
			if (int.TryParse(search, out int userId))
			{
				usersQuery = usersQuery.Where(u => u.Id == userId);
			}
			else
			{
				usersQuery = usersQuery.Where(u =>
					u.Email.Contains(search) ||
					u.FirstName.Contains(search) ||
					u.LastName.Contains(search));
			}
		}

		var users = await usersQuery
			.OrderBy(u => u.Id)
			.ToListAsync();

		// Получаем все активные заявки для отображения по пользователям
		var allPendingRequests = await _context.PendingRequests
			.Where(p => !p.IsCompleted)
			.Include(p => p.User)
			.ToListAsync();

		// Группируем заявки по пользователям
		var userRequests = allPendingRequests
			.GroupBy(p => p.UserId)
			.ToDictionary(g => g.Key, g => g.ToList());

		// Получаем количество активных заявок на вывод
		var pendingWithdrawalsCount = await _context.PendingRequests
			.Where(p => p.Type == "Withdrawal" && !p.IsCompleted)
			.CountAsync();

		// Получаем список активных заявок на вывод с информацией о пользователях
		var pendingWithdrawals = await _context.PendingRequests
			.Where(p => p.Type == "Withdrawal" && !p.IsCompleted)
			.Include(p => p.User)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		// Получаем количество активных заявок на пополнение
		var pendingTopUpsCount = await _context.PendingRequests
			.Where(p => p.Type == "TopUp" && !p.IsCompleted)
			.CountAsync();

		// Получаем список активных заявок на пополнение с информацией о пользователях
		var pendingTopUps = await _context.PendingRequests
			.Where(p => p.Type == "TopUp" && !p.IsCompleted)
			.Include(p => p.User)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		ViewBag.Search = search;
		ViewBag.PendingWithdrawalsCount = pendingWithdrawalsCount;
		ViewBag.PendingWithdrawals = pendingWithdrawals;
		ViewBag.PendingTopUpsCount = pendingTopUpsCount;
		ViewBag.PendingTopUps = pendingTopUps;
		ViewBag.UserRequests = userRequests;
		return View(users);
	}

	// GET: /Admin/EditBalance/{id}
	public async Task<IActionResult> EditBalance(int id)
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		var user = await _context.Users.FindAsync(id);
		if (user == null)
		{
			TempData["ErrorMessage"] = "Пользователь не найден";
			return RedirectToAction("Index");
		}

		// Получаем последние транзакции пользователя
		var transactions = await _context.Transactions
			.Where(t => t.UserId == id)
			.OrderByDescending(t => t.CreatedAt)
			.Take(10)
			.ToListAsync();

		// Получаем активные заявки пользователя
		var pendingRequests = await _context.PendingRequests
			.Where(p => p.UserId == id && !p.IsCompleted)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		var pendingTopUps = pendingRequests.Where(p => p.Type == "TopUp").ToList();
		var pendingWithdrawals = pendingRequests.Where(p => p.Type == "Withdrawal").ToList();
		
		// Вычисляем полный баланс (текущий + заблокированные на вывод)
		var blockedAmount = pendingWithdrawals.Sum(w => w.Amount);
		var fullBalance = user.Balance + blockedAmount;

		ViewBag.Transactions = transactions;
		ViewBag.PendingTopUps = pendingTopUps;
		ViewBag.PendingWithdrawals = pendingWithdrawals;
		ViewBag.FullBalance = fullBalance;
		ViewBag.BlockedAmount = blockedAmount;
		return View(user);
	}

	// POST: /Admin/UpdateBalance
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateBalance(int userId, decimal newBalance, string reason = "")
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return Json(new { success = false, message = "Пользователь не найден" });

			var oldBalance = user.Balance;
			var balanceChange = newBalance - oldBalance;

			// Получаем активные заявки на вывод для проверки
			var activeWithdrawals = await _context.PendingRequests
				.Where(p => p.UserId == userId && p.Type == "Withdrawal" && !p.IsCompleted)
				.ToListAsync();
			var totalWithdrawalAmount = activeWithdrawals.Sum(w => w.Amount);

			// Обновляем баланс
			user.Balance = newBalance;
			await _context.SaveChangesAsync();

			// Проверяем и закрываем активные заявки
			bool topUpConfirmed = false;
			bool withdrawalConfirmed = false;
			
			// Проверяем заявки на вывод: если баланс установлен с учетом заявки (новый баланс >= старый + сумма заявок)
			// или баланс увеличился на сумму заявок, или баланс установлен в 0/отрицательный при наличии заявок
			if (activeWithdrawals.Any() && 
				(newBalance >= oldBalance + totalWithdrawalAmount || 
				 balanceChange >= totalWithdrawalAmount ||
				 (newBalance <= 0 && oldBalance > 0)))
			{
				foreach (var pendingWithdrawal in activeWithdrawals)
				{
					// Закрываем заявку
					pendingWithdrawal.IsCompleted = true;
					withdrawalConfirmed = true;
					
					// Находим транзакцию о заявке и обновляем её описание
					var existingTransaction = await _context.Transactions
						.FirstOrDefaultAsync(t => t.UserId == userId 
							&& t.Amount == -pendingWithdrawal.Amount 
							&& t.Description.Contains("Заявка на вывод средств на сумму")
							&& t.Type == "Вывод средств"
							&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
							&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));
					
					if (existingTransaction != null)
					{
						// Обновляем описание транзакции
						existingTransaction.Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
					}
					else
					{
						// Если транзакцию не нашли, создаем новую
						var confirmationTransaction = new Transaction
						{
							UserId = userId,
							Type = "Вывод средств",
							Amount = -pendingWithdrawal.Amount,
							Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
							CreatedAt = DateTime.UtcNow,
							ProductName = ""
						};
						_context.Transactions.Add(confirmationTransaction);
					}
				}
			}
			
			if (balanceChange > 0)
			{
				// Если баланс увеличился - закрываем заявки на пополнение
				var pendingTopUps = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == "TopUp" && !p.IsCompleted)
					.ToListAsync();

			foreach (var pendingTopUp in pendingTopUps)
			{
				// Закрываем заявку, если баланс увеличился (независимо от суммы)
				pendingTopUp.IsCompleted = true;
				topUpConfirmed = true;
				
				// Находим транзакцию о заявке и обновляем её описание
				var existingTransaction = await _context.Transactions
					.FirstOrDefaultAsync(t => t.UserId == userId 
						&& t.Amount == pendingTopUp.Amount 
						&& t.Description.Contains("Заявка на пополнение баланса на сумму")
						&& t.Type == "Пополнение"
						&& t.CreatedAt >= pendingTopUp.CreatedAt.AddMinutes(-1)
						&& t.CreatedAt <= pendingTopUp.CreatedAt.AddMinutes(1));
				
				if (existingTransaction != null)
				{
					// Обновляем описание транзакции
					existingTransaction.Description = $"Пополнение счета на сумму {pendingTopUp.Amount:N0} ₽";
				}
				else
				{
					// Если транзакцию не нашли, создаем новую
					var confirmationTransaction = new Transaction
					{
						UserId = userId,
						Type = "Пополнение",
						Amount = pendingTopUp.Amount,
						Description = $"Пополнение счета на сумму {pendingTopUp.Amount:N0} ₽",
						CreatedAt = DateTime.UtcNow,
						ProductName = ""
					};
					_context.Transactions.Add(confirmationTransaction);
				}
			}
			}
			
			if (balanceChange < 0 && !withdrawalConfirmed)
			{
				// Если баланс уменьшился и заявки еще не закрыты - закрываем заявки на вывод И на пополнение
				
				// Закрываем заявки на пополнение (если баланс уменьшился, значит пополнение не прошло)
				var pendingTopUps = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == "TopUp" && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingTopUp in pendingTopUps)
				{
					pendingTopUp.IsCompleted = true;
				}
				
				// Закрываем заявки на вывод (если они еще не были закрыты выше)
				var pendingWithdrawals = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == "Withdrawal" && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingWithdrawal in pendingWithdrawals)
				{
					// Закрываем заявку, если баланс уменьшился (независимо от суммы)
					pendingWithdrawal.IsCompleted = true;
					withdrawalConfirmed = true;
					
					// Находим транзакцию о заявке и обновляем её описание
					var existingTransaction = await _context.Transactions
						.FirstOrDefaultAsync(t => t.UserId == userId 
							&& t.Amount == -pendingWithdrawal.Amount 
							&& t.Description.Contains("Заявка на вывод средств на сумму")
							&& t.Type == "Вывод средств"
							&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
							&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));
					
					if (existingTransaction != null)
					{
						// Обновляем описание транзакции
						existingTransaction.Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
					}
					else
					{
						// Если транзакцию не нашли, создаем новую
						var confirmationTransaction = new Transaction
						{
							UserId = userId,
							Type = "Вывод средств",
							Amount = -pendingWithdrawal.Amount,
							Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
							CreatedAt = DateTime.UtcNow,
							ProductName = ""
						};
						_context.Transactions.Add(confirmationTransaction);
					}
				}
			}

			await _context.SaveChangesAsync();

			// Создаем транзакцию для истории (только если это не подтверждение заявки)
			if (!withdrawalConfirmed && !topUpConfirmed)
			{
				var transaction = new Transaction
				{
					UserId = userId,
					Type = balanceChange > 0 ? "Пополнение" : "Списание",
					Amount = balanceChange,
					Description = balanceChange > 0 
						? (string.IsNullOrEmpty(reason) 
							? $"Поступление средств на сумму {balanceChange:N0} ₽"
							: $"Поступление средств: {reason}")
						: (string.IsNullOrEmpty(reason)
							? $"Списание средств на сумму {Math.Abs(balanceChange):N0} ₽"
							: $"Списание средств: {reason}"),
					CreatedAt = DateTime.UtcNow,
					ProductName = ""
				};

				_context.Transactions.Add(transaction);
				await _context.SaveChangesAsync();
			}

		// Если это текущий пользователь, обновляем сессию
		var userJson = HttpContext.Session.GetString("User");
		if (!string.IsNullOrEmpty(userJson))
		{
			var sessionUser = JsonSerializer.Deserialize<User>(userJson);
			if (sessionUser?.Id == userId)
			{
				HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, new JsonSerializerOptions
				{
					ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
				}));
			}
		}

			return Json(new { success = true, message = $"Баланс успешно изменен с {oldBalance:N0} ₽ на {newBalance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	// POST: /Admin/ToggleWithdrawal
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ToggleWithdrawal(int userId)
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return Json(new { success = false, message = "Пользователь не найден" });

			user.WithdrawalEnabled = !user.WithdrawalEnabled;
			await _context.SaveChangesAsync();

		// Если это текущий пользователь, обновляем сессию
		var userJson = HttpContext.Session.GetString("User");
		if (!string.IsNullOrEmpty(userJson))
		{
			var sessionUser = JsonSerializer.Deserialize<User>(userJson);
			if (sessionUser?.Id == userId)
			{
				HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, new JsonSerializerOptions
				{
					ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
				}));
			}
		}

			return Json(new { 
				success = true, 
				enabled = user.WithdrawalEnabled,
				message = user.WithdrawalEnabled ? "Вывод средств разрешен" : "Вывод средств запрещен"
			});
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	// POST: /Admin/QuickTopUp - Быстрое пополнение баланса по заявке
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> QuickTopUp(int userId, decimal amount)
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return Json(new { success = false, message = "Пользователь не найден" });

			// Находим активную заявку на пополнение
			var pendingTopUp = await _context.PendingRequests
				.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == "TopUp" && !p.IsCompleted && p.Amount == amount);

			if (pendingTopUp == null)
				return Json(new { success = false, message = "Заявка на пополнение не найдена" });

			var oldBalance = user.Balance;
			var newBalance = oldBalance + amount;

			// Обновляем баланс
			user.Balance = newBalance;

			// Закрываем заявку
			pendingTopUp.IsCompleted = true;

			// Находим транзакцию о заявке и обновляем её описание
			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.UserId == userId 
					&& t.Amount == amount 
					&& t.Description.Contains("Заявка на пополнение баланса на сумму")
					&& t.Type == "Пополнение"
					&& t.CreatedAt >= pendingTopUp.CreatedAt.AddMinutes(-1)
					&& t.CreatedAt <= pendingTopUp.CreatedAt.AddMinutes(1));
			
			if (existingTransaction != null)
			{
				// Обновляем описание транзакции
				existingTransaction.Description = $"Пополнение счета на сумму {amount:N0} ₽";
			}
			else
			{
				// Если транзакцию не нашли, создаем новую
				var confirmationTransaction = new Transaction
				{
					UserId = userId,
					Type = "Пополнение",
					Amount = amount,
					Description = $"Пополнение счета на сумму {amount:N0} ₽",
					CreatedAt = DateTime.UtcNow,
					ProductName = ""
				};
				_context.Transactions.Add(confirmationTransaction);
			}
			
			await _context.SaveChangesAsync();

			// Если это текущий пользователь, обновляем сессию
			var userJson = HttpContext.Session.GetString("User");
			if (!string.IsNullOrEmpty(userJson))
			{
				var sessionUser = JsonSerializer.Deserialize<User>(userJson);
				if (sessionUser?.Id == userId)
				{
					HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, new JsonSerializerOptions
					{
						ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
					}));
				}
			}

			return Json(new { success = true, message = $"Баланс успешно пополнен на {amount:N0} ₽. Новый баланс: {newBalance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	// POST: /Admin/QuickWithdrawal - Быстрое подтверждение вывода средств по заявке
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> QuickWithdrawal(int userId, decimal amount)
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return Json(new { success = false, message = "Пользователь не найден" });

			// Находим активную заявку на вывод
			var pendingWithdrawal = await _context.PendingRequests
				.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == "Withdrawal" && !p.IsCompleted && p.Amount == amount);

			if (pendingWithdrawal == null)
				return Json(new { success = false, message = "Заявка на вывод не найдена" });

			// Баланс уже уменьшен при создании заявки, поэтому просто закрываем заявку
			// Закрываем заявку
			pendingWithdrawal.IsCompleted = true;

			// Находим транзакцию о заявке и обновляем её описание
			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.UserId == userId 
					&& t.Amount == -amount 
					&& t.Description.Contains("Заявка на вывод средств на сумму")
					&& t.Type == "Вывод средств"
					&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
					&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));
			
			if (existingTransaction != null)
			{
				// Обновляем описание транзакции
				existingTransaction.Description = $"Вывод средств на сумму {amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
			}
			else
			{
				// Если транзакцию не нашли, создаем новую
				var confirmationTransaction = new Transaction
				{
					UserId = userId,
					Type = "Вывод средств",
					Amount = -amount,
					Description = $"Вывод средств на сумму {amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
					CreatedAt = DateTime.UtcNow,
					ProductName = ""
				};
				_context.Transactions.Add(confirmationTransaction);
			}
			
			await _context.SaveChangesAsync();

			// Если это текущий пользователь, обновляем сессию
			var userJson = HttpContext.Session.GetString("User");
			if (!string.IsNullOrEmpty(userJson))
			{
				var sessionUser = JsonSerializer.Deserialize<User>(userJson);
				if (sessionUser?.Id == userId)
				{
					HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, new JsonSerializerOptions
					{
						ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
					}));
				}
			}

			return Json(new { success = true, message = $"Вывод средств на сумму {amount:N0} ₽ подтвержден. Баланс: {user.Balance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	// POST: /Admin/DeleteAllTransactions - Удаление всех транзакций
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteAllTransactions()
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var count = await _context.Transactions.CountAsync();
			_context.Transactions.RemoveRange(_context.Transactions);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = $"Удалено транзакций: {count}" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	// POST: /Admin/DeleteAllPendingRequests - Удаление всех заявок
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteAllPendingRequests()
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var count = await _context.PendingRequests.CountAsync();
			_context.PendingRequests.RemoveRange(_context.PendingRequests);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = $"Удалено заявок: {count}" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}
}
