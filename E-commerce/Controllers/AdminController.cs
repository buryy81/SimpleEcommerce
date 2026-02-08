using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;
using System.Text.Json;
using System;

namespace SimpleEcommerce.Controllers;

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

		ViewBag.Search = search;
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

		ViewBag.Transactions = transactions;
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

			// Обновляем баланс
			user.Balance = newBalance;
			await _context.SaveChangesAsync();

			// Проверяем и закрываем активные заявки
			if (balanceChange > 0)
			{
				// Если баланс увеличился - закрываем заявки на пополнение
				var pendingTopUps = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == "TopUp" && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingTopUp in pendingTopUps)
				{
					// Если баланс увеличился на сумму заявки или больше - закрываем заявку
					if (balanceChange >= pendingTopUp.Amount)
					{
						pendingTopUp.IsCompleted = true;
					}
				}
			}
			else if (balanceChange < 0)
			{
				// Если баланс уменьшился - закрываем заявки на вывод
				var pendingWithdrawals = await _context.PendingRequests
					.Where(p => p.UserId == userId && p.Type == "Withdrawal" && !p.IsCompleted)
					.ToListAsync();

				foreach (var pendingWithdrawal in pendingWithdrawals)
				{
					// Если баланс уменьшился на сумму заявки или больше - закрываем заявку
					if (Math.Abs(balanceChange) >= pendingWithdrawal.Amount)
					{
						pendingWithdrawal.IsCompleted = true;
					}
				}
			}

			await _context.SaveChangesAsync();

			// Создаем транзакцию для истории
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
}
