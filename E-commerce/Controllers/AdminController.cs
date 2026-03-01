using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

public class AdminController : BaseController
{
	private readonly ApplicationDbContext _context;

	public AdminController(ApplicationDbContext context)
	{
		_context = context;
	}

	private void UpdateSessionIfCurrentUser(int userId, User user)
	{
		var sessionUser = GetSessionUser();
		if (sessionUser?.Id == userId)
			SaveUserToSession(user);
	}

	public async Task<IActionResult> Index(string search = "")
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		// Получаем список заблокированных IP
		var blockedIps = await _context.BlockedIps.Select(b => b.Ip).ToListAsync();

		IQueryable<User> usersQuery = _context.Users;

		// Исключаем пользователей с заблокированными IP
		if (blockedIps.Any())
		{
			usersQuery = usersQuery.Where(u => u.Ip == null || !blockedIps.Contains(u.Ip));
		}

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

		var pendingWithdrawalsCount = await _context.PendingRequests
			.Where(p => p.Type == RequestTypeWithdrawal && !p.IsCompleted)
			.CountAsync();

		var pendingWithdrawals = await _context.PendingRequests
			.Where(p => p.Type == RequestTypeWithdrawal && !p.IsCompleted)
			.Include(p => p.User)
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();

		var pendingTopUpsCount = await _context.PendingRequests
			.Where(p => p.Type == RequestTypeTopUp && !p.IsCompleted)
			.CountAsync();

		var pendingTopUps = await _context.PendingRequests
			.Where(p => p.Type == RequestTypeTopUp && !p.IsCompleted)
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

		var pendingTopUps = pendingRequests.Where(p => p.Type == RequestTypeTopUp).ToList();
		var pendingWithdrawals = pendingRequests.Where(p => p.Type == RequestTypeWithdrawal).ToList();
		
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

	public async Task<IActionResult> BlockedIps(string search = "")
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		var blockedIpsList = await _context.BlockedIps.OrderByDescending(b => b.CreatedAt).ToListAsync();
		var blockedIps = blockedIpsList.Select(b => b.Ip).ToList();

		// Получаем пользователей с заблокированными IP
		List<User> blockedUsers = new List<User>();
		if (blockedIps.Any())
		{
			IQueryable<User> usersQuery = _context.Users
				.Where(u => u.Ip != null && blockedIps.Contains(u.Ip));

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

			blockedUsers = await usersQuery
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

			ViewBag.UserRequests = userRequests;
		}

		ViewBag.Search = search;
		ViewBag.BlockedUsers = blockedUsers;
		return View(blockedIpsList);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddBlockedIp(string ip, string? comment = null, string? returnUrl = null)
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		ip = (ip ?? "").Trim();
		if (string.IsNullOrEmpty(ip))
		{
			TempData["ErrorMessage"] = "Укажите IP-адрес";
			return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("BlockedIps")!);
		}

		if (await _context.BlockedIps.AnyAsync(b => b.Ip == ip))
		{
			TempData["ErrorMessage"] = "Этот IP уже заблокирован";
			return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("BlockedIps")!);
		}

		_context.BlockedIps.Add(new BlockedIp { Ip = ip, Comment = comment?.Trim() });
		await _context.SaveChangesAsync();
		TempData["SuccessMessage"] = $"IP {ip} заблокирован";
		return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("BlockedIps")!);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RemoveBlockedIp(int id)
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		var blocked = await _context.BlockedIps.FindAsync(id);
		if (blocked != null)
		{
			_context.BlockedIps.Remove(blocked);
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = $"IP {blocked.Ip} разблокирован";
		}
		return RedirectToAction("BlockedIps");
	}

	public async Task<IActionResult> EditLevel(int id)
	{
		if (!IsAdmin())
			return RedirectToAction("Login", "Account");

		var user = await _context.Users.FindAsync(id);
		if (user == null)
		{
			TempData["ErrorMessage"] = "Пользователь не найден";
			return RedirectToAction("Index");
		}
		return View(user);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateLevel(int userId, int level)
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
				return Json(new { success = false, message = "Пользователь не найден" });

			if (level < 0)
				return Json(new { success = false, message = "Уровень не может быть отрицательным" });

			var oldLevel = user.Level;
			user.Level = level;
			await _context.SaveChangesAsync();

			UpdateSessionIfCurrentUser(userId, user);

			return Json(new { success = true, message = $"Уровень изменен с {oldLevel} на {level}" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

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

			var activeWithdrawals = await _context.PendingRequests
				.Where(p => p.UserId == userId && p.Type == RequestTypeWithdrawal && !p.IsCompleted)
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
							&& t.Description != null && t.Description.Contains("Заявка на вывод средств на сумму")
							&& t.Type == TransactionTypeWithdrawal
							&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
							&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));
					
					if (existingTransaction != null)
						existingTransaction.Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
					else
						_context.Transactions.Add(new Transaction
						{
							UserId = userId,
							Type = TransactionTypeWithdrawal,
							Amount = -pendingWithdrawal.Amount,
							Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
							CreatedAt = DateTime.UtcNow,
							ProductName = ""
						});
				}
			}

			if (balanceChange > 0)
			{
				var pendingTopUps = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == RequestTypeTopUp && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingTopUp in pendingTopUps)
				{
					pendingTopUp.IsCompleted = true;
					topUpConfirmed = true;

					var existingTransaction = await _context.Transactions
						.FirstOrDefaultAsync(t => t.UserId == userId 
							&& t.Amount == pendingTopUp.Amount 
							&& t.Description != null && t.Description.Contains("Заявка на пополнение баланса на сумму")
							&& t.Type == TransactionTypeTopUp
							&& t.CreatedAt >= pendingTopUp.CreatedAt.AddMinutes(-1)
							&& t.CreatedAt <= pendingTopUp.CreatedAt.AddMinutes(1));

					if (existingTransaction != null)
						existingTransaction.Description = $"Пополнение счета на сумму {pendingTopUp.Amount:N0} ₽";
					else
						_context.Transactions.Add(new Transaction
						{
							UserId = userId,
							Type = TransactionTypeTopUp,
							Amount = pendingTopUp.Amount,
							Description = $"Пополнение счета на сумму {pendingTopUp.Amount:N0} ₽",
							CreatedAt = DateTime.UtcNow,
							ProductName = ""
						});
				}
			}

			if (balanceChange < 0 && !withdrawalConfirmed)
			{
				var pendingTopUps = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == RequestTypeTopUp && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingTopUp in pendingTopUps)
					pendingTopUp.IsCompleted = true;

				var pendingWithdrawals = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == RequestTypeWithdrawal && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingWithdrawal in pendingWithdrawals)
				{
					pendingWithdrawal.IsCompleted = true;
					withdrawalConfirmed = true;

					var existingTransaction = await _context.Transactions
						.FirstOrDefaultAsync(t => t.UserId == userId 
							&& t.Amount == -pendingWithdrawal.Amount 
							&& t.Description != null && t.Description.Contains("Заявка на вывод средств на сумму")
							&& t.Type == TransactionTypeWithdrawal
							&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
							&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));

					if (existingTransaction != null)
						existingTransaction.Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
					else
						_context.Transactions.Add(new Transaction
						{
							UserId = userId,
							Type = TransactionTypeWithdrawal,
							Amount = -pendingWithdrawal.Amount,
							Description = $"Вывод средств на сумму {pendingWithdrawal.Amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
							CreatedAt = DateTime.UtcNow,
							ProductName = ""
						});
				}
			}

			await _context.SaveChangesAsync();

			if (!withdrawalConfirmed && !topUpConfirmed)
			{
				_context.Transactions.Add(new Transaction
				{
					UserId = userId,
					Type = balanceChange > 0 ? TransactionTypeTopUp : TransactionTypeDeduction,
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
				});
				await _context.SaveChangesAsync();
			}

			UpdateSessionIfCurrentUser(userId, user);

			return Json(new { success = true, message = $"Баланс успешно изменен с {oldBalance:N0} ₽ на {newBalance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

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

			UpdateSessionIfCurrentUser(userId, user);

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

			var pendingTopUp = await _context.PendingRequests
				.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == RequestTypeTopUp && !p.IsCompleted && p.Amount == amount);

			if (pendingTopUp == null)
				return Json(new { success = false, message = "Заявка на пополнение не найдена" });

			var oldBalance = user.Balance;
			var newBalance = oldBalance + amount;

			// Обновляем баланс
			user.Balance = newBalance;

			pendingTopUp.IsCompleted = true;

			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.UserId == userId 
					&& t.Amount == amount 
					&& t.Description != null && t.Description.Contains("Заявка на пополнение баланса на сумму")
					&& t.Type == TransactionTypeTopUp
					&& t.CreatedAt >= pendingTopUp.CreatedAt.AddMinutes(-1)
					&& t.CreatedAt <= pendingTopUp.CreatedAt.AddMinutes(1));

			if (existingTransaction != null)
				existingTransaction.Description = $"Пополнение счета на сумму {amount:N0} ₽";
			else
				_context.Transactions.Add(new Transaction
				{
					UserId = userId,
					Type = TransactionTypeTopUp,
					Amount = amount,
					Description = $"Пополнение счета на сумму {amount:N0} ₽",
					CreatedAt = DateTime.UtcNow,
					ProductName = ""
				});

			await _context.SaveChangesAsync();

			UpdateSessionIfCurrentUser(userId, user);

			return Json(new { success = true, message = $"Баланс успешно пополнен на {amount:N0} ₽. Новый баланс: {newBalance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RejectTopUp(int userId, decimal amount)
	{
		try
		{
			if (!IsAdmin())
				return Json(new { success = false, message = "Доступ запрещен" });

			var pendingTopUp = await _context.PendingRequests
				.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == RequestTypeTopUp && !p.IsCompleted && p.Amount == amount);

			if (pendingTopUp == null)
				return Json(new { success = false, message = "Заявка на пополнение не найдена" });

			pendingTopUp.IsCompleted = true;

			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.UserId == userId
					&& t.Amount == amount
					&& t.Description != null && t.Description.Contains("Заявка на пополнение баланса на сумму")
					&& t.Type == TransactionTypeTopUp
					&& t.CreatedAt >= pendingTopUp.CreatedAt.AddMinutes(-1)
					&& t.CreatedAt <= pendingTopUp.CreatedAt.AddMinutes(1));

			if (existingTransaction != null)
				existingTransaction.Description = $"Заявка на пополнение отклонена (сумма {amount:N0} ₽)";

			await _context.SaveChangesAsync();

			return Json(new { success = true, message = $"Заявка на пополнение на {amount:N0} ₽ отклонена" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

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

			var pendingWithdrawal = await _context.PendingRequests
				.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == RequestTypeWithdrawal && !p.IsCompleted && p.Amount == amount);

			if (pendingWithdrawal == null)
				return Json(new { success = false, message = "Заявка на вывод не найдена" });

			pendingWithdrawal.IsCompleted = true;

			var existingTransaction = await _context.Transactions
				.FirstOrDefaultAsync(t => t.UserId == userId 
					&& t.Amount == -amount 
					&& t.Description != null && t.Description.Contains("Заявка на вывод средств на сумму")
					&& t.Type == TransactionTypeWithdrawal
					&& t.CreatedAt >= pendingWithdrawal.CreatedAt.AddMinutes(-1)
					&& t.CreatedAt <= pendingWithdrawal.CreatedAt.AddMinutes(1));

			if (existingTransaction != null)
				existingTransaction.Description = $"Вывод средств на сумму {amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}";
			else
				_context.Transactions.Add(new Transaction
				{
					UserId = userId,
					Type = TransactionTypeWithdrawal,
					Amount = -amount,
					Description = $"Вывод средств на сумму {amount:N0} ₽: {pendingWithdrawal.Bank}, {pendingWithdrawal.CardOrPhone}",
					CreatedAt = DateTime.UtcNow,
					ProductName = ""
				});

			await _context.SaveChangesAsync();

			UpdateSessionIfCurrentUser(userId, user);

			return Json(new { success = true, message = $"Вывод средств на сумму {amount:N0} ₽ подтвержден. Баланс: {user.Balance:N0} ₽" });
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
		}
	}

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
