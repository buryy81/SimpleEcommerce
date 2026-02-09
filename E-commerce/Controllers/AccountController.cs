using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

public class AccountController : Controller
{
	private readonly ApplicationDbContext _context;
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public AccountController(ApplicationDbContext context)
	{
		_context = context;
	}

	// GET: /Account/Login
	public IActionResult Login()
	{
		// Если пользователь уже авторизован, перенаправляем в профиль
		var userJson = HttpContext.Session.GetString("User");
		if (!string.IsNullOrEmpty(userJson))
		{
			return RedirectToAction("Profile");
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
			// Сохраняем пользователя в сессии (без навигационных свойств)
			HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, JsonOptions));
			return RedirectToAction("Profile");
		}

		ViewBag.Error = "Неверный email или пароль";
		return View();
	}

	// GET: /Account/Register
	public IActionResult Register()
	{
		// Очищаем ModelState при GET запросе, чтобы не показывать ошибки при первой загрузке
		ModelState.Clear();
		return View();
	}

	// GET: /Account/Terms
	public IActionResult Terms()
	{
		return View();
	}

	// POST: /Account/Register
	[HttpPost]
	public async Task<IActionResult> Register(User user, bool agreeTerms = false)
	{
		// Проверка согласия с офертой
		// В ASP.NET Core, если чекбокс не отмечен, параметр может быть не передан вообще
		var agreeTermsValue = Request.Form["agreeTerms"].ToString();
		bool hasAgreed = agreeTerms || agreeTermsValue == "true" || agreeTermsValue == "on";
		
		if (!hasAgreed)
		{
			ModelState.AddModelError("agreeTerms", "Для продолжения регистрации необходимо принять условия пользовательского соглашения");
			// Делаем ModelState невалидным, чтобы форма не прошла валидацию
			ModelState.AddModelError("", "Пожалуйста, исправьте ошибки в форме");
			return View(user);
		}

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

			user.Balance = 0; // Начальный баланс

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Сохраняем в сессии (без навигационных свойств)
			HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, JsonOptions));
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

		// Проверяем активные заявки в БД и синхронизируем с сессией
		var activeTopUp = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == "TopUp" && !p.IsCompleted);
		
		if (activeTopUp == null && !string.IsNullOrEmpty(HttpContext.Session.GetString("PendingTopUp")))
		{
			// Заявка закрыта в БД, но еще есть в сессии - очищаем сессию
			HttpContext.Session.Remove("PendingTopUp");
		}
		else if (activeTopUp != null && string.IsNullOrEmpty(HttpContext.Session.GetString("PendingTopUp")))
		{
			// Заявка есть в БД, но нет в сессии - восстанавливаем в сессии
			HttpContext.Session.SetString("PendingTopUp", activeTopUp.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
		}

		// Получаем активную заявку на вывод
		var activeWithdrawal = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == "Withdrawal" && !p.IsCompleted);
		
		if (activeWithdrawal == null && !string.IsNullOrEmpty(HttpContext.Session.GetString("PendingWithdrawal")))
		{
			// Заявка закрыта в БД, но еще есть в сессии - очищаем сессию
			HttpContext.Session.Remove("PendingWithdrawal");
			HttpContext.Session.Remove("PendingWithdrawalBank");
			HttpContext.Session.Remove("PendingWithdrawalCardOrPhone");
		}
		else if (activeWithdrawal != null && string.IsNullOrEmpty(HttpContext.Session.GetString("PendingWithdrawal")))
		{
			// Заявка есть в БД, но нет в сессии - восстанавливаем в сессии
			HttpContext.Session.SetString("PendingWithdrawal", activeWithdrawal.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
			if (!string.IsNullOrEmpty(activeWithdrawal.Bank))
				HttpContext.Session.SetString("PendingWithdrawalBank", activeWithdrawal.Bank);
			if (!string.IsNullOrEmpty(activeWithdrawal.CardOrPhone))
				HttpContext.Session.SetString("PendingWithdrawalCardOrPhone", activeWithdrawal.CardOrPhone);
		}

		// Обновляем сессию с актуальными данными (без навигационных свойств)
		HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, JsonOptions));

		// Получаем активные заявки
		activeTopUp = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == "TopUp" && !p.IsCompleted);

		// Получаем транзакции отсортированные по дате
		var transactions = await _context.Transactions
			.Where(t => t.UserId == user.Id)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync();

		ViewBag.Transactions = transactions;
		ViewBag.ActiveWithdrawal = activeWithdrawal;
		ViewBag.ActiveTopUp = activeTopUp;
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

		// Сохраняем информацию о ожидающем пополнении в БД
		var pendingRequest = new PendingRequest
		{
			UserId = user.Id,
			Type = "TopUp",
			Amount = amount,
			CreatedAt = DateTime.UtcNow,
			IsCompleted = false
		};

		_context.PendingRequests.Add(pendingRequest);

		// Создаем транзакцию для истории операций
		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = "Пополнение",
			Amount = amount,
			Description = $"Заявка на пополнение баланса на сумму {amount:N0} ₽",
			CreatedAt = DateTime.UtcNow,
			ProductName = ""
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		// Также сохраняем в сессии для отображения плашки
		HttpContext.Session.SetString("PendingTopUp", amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

		// Возвращаем успешный ответ (для JavaScript)
		return Ok();
	}

	// GET: /Account/ClearPendingTopUp
	[HttpGet]
	public IActionResult ClearPendingTopUp()
	{
		HttpContext.Session.Remove("PendingTopUp");
		return Ok();
	}

	// POST: /Account/RequestWithdrawal
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RequestWithdrawal(decimal amount, string bank, string cardOrPhone)
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return Json(new { success = false, message = "Необходима авторизация" });

		var sessionUser = JsonSerializer.Deserialize<User>(userJson);
		var user = await _context.Users.FindAsync(sessionUser.Id);

		if (user == null)
			return Json(new { success = false, message = "Пользователь не найден" });

		if (!user.WithdrawalEnabled)
			return Json(new { success = false, message = "Вывод средств временно недоступен" });

		if (amount <= 0 || amount > user.Balance)
			return Json(new { success = false, message = "Неверная сумма для вывода" });

		// Отнимаем сумму с баланса пользователя
		user.Balance -= amount;

		// Сохраняем информацию о заявке на вывод в БД
		var pendingRequest = new PendingRequest
		{
			UserId = user.Id,
			Type = "Withdrawal",
			Amount = amount,
			Bank = bank,
			CardOrPhone = cardOrPhone,
			CreatedAt = DateTime.UtcNow,
			IsCompleted = false
		};

		_context.PendingRequests.Add(pendingRequest);

		// Создаем транзакцию для истории операций
		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = "Вывод средств",
			Amount = -amount,
			Description = $"Заявка на вывод средств на сумму {amount:N0} ₽: {bank}, {cardOrPhone}",
			CreatedAt = DateTime.UtcNow,
			ProductName = ""
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		// Обновляем сессию с актуальными данными
		HttpContext.Session.SetString("User", JsonSerializer.Serialize(user, JsonOptions));

		// Также сохраняем в сессии для отображения плашки
		HttpContext.Session.SetString("PendingWithdrawal", amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
		HttpContext.Session.SetString("PendingWithdrawalBank", bank);
		HttpContext.Session.SetString("PendingWithdrawalCardOrPhone", cardOrPhone);

		return Json(new { success = true, message = "Заявка на вывод средств создана" });
	}

	// GET: /Account/Logout
	public IActionResult Logout()
	{
		HttpContext.Session.Remove("User");
		HttpContext.Session.Remove("PendingTopUp");
		HttpContext.Session.Remove("PendingWithdrawal");
		HttpContext.Session.Remove("PendingWithdrawalBank");
		HttpContext.Session.Remove("PendingWithdrawalCardOrPhone");
		return RedirectToAction("Login");
	}
}
