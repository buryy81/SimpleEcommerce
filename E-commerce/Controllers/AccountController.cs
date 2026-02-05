using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

public class AccountController : Controller
{
	private readonly ApplicationDbContext _context;

	public AccountController(ApplicationDbContext context)
	{
		_context = context;
	}

	// GET: /Account/Login
	public IActionResult Login()
	{
		// Если пользователь не авторизован, перенаправляем на регистрацию
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
		{
			return RedirectToAction("Register");
		}
		return View();
	}

	// POST: /Account/Login
	[HttpPost]
	public async Task<IActionResult> Login(string email, string password)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

		if (user != null)
		{
			// Сохраняем пользователя в сессии
			HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));
			return RedirectToAction("Profile");
		}

		ViewBag.Error = "Неверный email или пароль";
		return View();
	}

	// GET: /Account/Register
	public IActionResult Register()
	{
		return View();
	}

	// POST: /Account/Register
	[HttpPost]
	public async Task<IActionResult> Register(User user)
	{
		if (ModelState.IsValid)
		{
			// Проверка на существование пользователя с таким email
			var existingUser = await _context.Users
				.FirstOrDefaultAsync(u => u.Email == user.Email);

			if (existingUser != null)
			{
				ModelState.AddModelError("Email", "Пользователь с таким email уже существует");
				return View(user);
			}

			// Конвертируем BirthDate в UTC если необходимо
			// Дата из формы приходит как Unspecified, нужно конвертировать в UTC
			if (user.BirthDate.Kind == DateTimeKind.Unspecified)
			{
				// Предполагаем, что дата рождения введена в локальном времени
				// Конвертируем в UTC, сохраняя только дату (без времени)
				user.BirthDate = new DateTime(user.BirthDate.Year, user.BirthDate.Month, user.BirthDate.Day, 0, 0, 0, DateTimeKind.Utc);
			}
			else if (user.BirthDate.Kind == DateTimeKind.Local)
			{
				user.BirthDate = user.BirthDate.ToUniversalTime();
			}
			else if (user.BirthDate.Kind == DateTimeKind.Utc)
			{
				// Уже UTC, но убедимся что время установлено на начало дня
				user.BirthDate = new DateTime(user.BirthDate.Year, user.BirthDate.Month, user.BirthDate.Day, 0, 0, 0, DateTimeKind.Utc);
			}

			user.Balance = 10000; // Стартовый баланс

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Создаем транзакцию для регистрационного бонуса
			var transaction = new Transaction
			{
				UserId = user.Id,
				Type = "Пополнение",
				Amount = 10000,
				Description = "Регистрационный бонус",
				CreatedAt = DateTime.UtcNow
			};
			_context.Transactions.Add(transaction);
			await _context.SaveChangesAsync();

			// Сохраняем в сессии
			HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));
			return RedirectToAction("Profile");
		}

		return View(user);
	}

	// GET: /Account/Profile (Личный кабинет)
	public async Task<IActionResult> Profile()
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return RedirectToAction("Login");

		var sessionUser = JsonSerializer.Deserialize<User>(userJson);
		
		// Получаем актуальные данные из БД
		var user = await _context.Users
			.FirstOrDefaultAsync(u => u.Id == sessionUser.Id);

		if (user == null)
			return RedirectToAction("Login");

		// Обновляем сессию с актуальными данными
		HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));

		// Получаем транзакции отсортированные по дате
		var transactions = await _context.Transactions
			.Where(t => t.UserId == user.Id)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync();

		ViewBag.Transactions = transactions;
		return View(user);
	}

	// POST: /Account/AddFunds
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddFunds(decimal amount)
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return RedirectToAction("Login");

		var sessionUser = JsonSerializer.Deserialize<User>(userJson);
		var user = await _context.Users.FindAsync(sessionUser.Id);

		if (user == null)
			return RedirectToAction("Login");

		user.Balance += amount;

		// Создаем транзакцию
		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = "Пополнение",
			Amount = amount,
			Description = "Пополнение баланса",
			CreatedAt = DateTime.UtcNow
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		// Обновляем сессию
		HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));

		TempData["SuccessMessage"] = $"Баланс успешно пополнен на {amount:N0} ₽";
		return RedirectToAction("Profile");
	}

	// GET: /Account/Logout
	public IActionResult Logout()
	{
		HttpContext.Session.Remove("User");
		return RedirectToAction("Login");
	}
}
