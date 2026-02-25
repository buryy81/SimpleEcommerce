using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

public class AccountController : BaseController
{
	private readonly ApplicationDbContext _context;

	public AccountController(ApplicationDbContext context)
	{
		_context = context;
	}

	public IActionResult Login()
	{
		if (GetSessionUser() != null)
			return RedirectToAction("Profile");
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
			SaveUserToSession(user);
			return RedirectToAction("Profile");
		}

		ViewBag.Error = "Неверный email или пароль";
		return View();
	}

	public IActionResult Register()
	{
		ModelState.Clear();
		return View();
	}

	public IActionResult Terms()
	{
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> Register(User user, bool agreeTerms = false)
	{
		var agreeTermsValue = Request.Form["agreeTerms"].ToString();
		bool hasAgreed = agreeTerms || agreeTermsValue == "true" || agreeTermsValue == "on";
		
		if (!hasAgreed)
		{
			ModelState.AddModelError("agreeTerms", "Для продолжения регистрации необходимо принять условия пользовательского соглашения");
			ModelState.AddModelError("", "Пожалуйста, исправьте ошибки в форме");
			return View(user);
		}

		if (ModelState.IsValid)
		{
			var existingUser = await _context.Users
				.FirstOrDefaultAsync(u => u.Email == user.Email);

			if (existingUser != null)
			{
				ModelState.AddModelError("Email", "Пользователь с таким email уже существует");
				return View(user);
			}

			user.Balance = 0;
			user.Ip = HttpContext.Connection.RemoteIpAddress?.ToString();

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			SaveUserToSession(user);
			return RedirectToAction("Profile");
		}

		return View(user);
	}

	public async Task<IActionResult> Profile()
	{
		var sessionUser = GetSessionUser();
		if (sessionUser == null)
			return RedirectToAction("Login");

		var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sessionUser.Id);
		if (user == null)
			return RedirectToAction("Login");

		var activeTopUp = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == RequestTypeTopUp && !p.IsCompleted);

		if (activeTopUp == null && !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyPendingTopUp)))
			HttpContext.Session.Remove(SessionKeyPendingTopUp);
		else if (activeTopUp != null && string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyPendingTopUp)))
			HttpContext.Session.SetString(SessionKeyPendingTopUp, activeTopUp.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

		var activeWithdrawal = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == RequestTypeWithdrawal && !p.IsCompleted);

		if (activeWithdrawal == null && !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyPendingWithdrawal)))
		{
			HttpContext.Session.Remove(SessionKeyPendingWithdrawal);
			HttpContext.Session.Remove(SessionKeyPendingWithdrawalBank);
			HttpContext.Session.Remove(SessionKeyPendingWithdrawalCardOrPhone);
		}
		else if (activeWithdrawal != null && string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyPendingWithdrawal)))
		{
			HttpContext.Session.SetString(SessionKeyPendingWithdrawal, activeWithdrawal.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
			if (!string.IsNullOrEmpty(activeWithdrawal.Bank))
				HttpContext.Session.SetString(SessionKeyPendingWithdrawalBank, activeWithdrawal.Bank);
			if (!string.IsNullOrEmpty(activeWithdrawal.CardOrPhone))
				HttpContext.Session.SetString(SessionKeyPendingWithdrawalCardOrPhone, activeWithdrawal.CardOrPhone);
		}

		SaveUserToSession(user);

		activeTopUp = await _context.PendingRequests
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == RequestTypeTopUp && !p.IsCompleted);

		var transactions = await _context.Transactions
			.Where(t => t.UserId == user.Id)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync();

		ViewBag.Transactions = transactions;
		ViewBag.ActiveWithdrawal = activeWithdrawal;
		ViewBag.ActiveTopUp = activeTopUp;
		return View(user);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddFunds(decimal amount)
	{
		var sessionUser = GetSessionUser();
		if (sessionUser == null)
			return RedirectToAction("Login");

		var user = await _context.Users.FindAsync(sessionUser.Id);
		if (user == null)
			return RedirectToAction("Login");

		var pendingRequest = new PendingRequest
		{
			UserId = user.Id,
			Type = RequestTypeTopUp,
			Amount = amount,
			CreatedAt = DateTime.UtcNow,
			IsCompleted = false
		};

		_context.PendingRequests.Add(pendingRequest);

		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = TransactionTypeTopUp,
			Amount = amount,
			Description = $"Заявка на пополнение баланса на сумму {amount:N0} ₽",
			CreatedAt = DateTime.UtcNow,
			ProductName = ""
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		HttpContext.Session.SetString(SessionKeyPendingTopUp, amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

		return Ok();
	}

	[HttpGet]
	public IActionResult ClearPendingTopUp()
	{
		HttpContext.Session.Remove(SessionKeyPendingTopUp);
		return Ok();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RequestWithdrawal(decimal amount, string bank, string cardOrPhone)
	{
		var sessionUser = GetSessionUser();
		if (sessionUser == null)
			return Json(new { success = false, message = "Необходима авторизация" });

		var user = await _context.Users.FindAsync(sessionUser.Id);
		if (user == null)
			return Json(new { success = false, message = "Пользователь не найден" });

		if (!user.WithdrawalEnabled)
			return Json(new { success = false, message = "Вывод средств временно недоступен" });

		if (user.Balance <= 0)
			return Json(new { success = false, message = "Вывод средств недоступен при нулевом балансе" });

		if (amount <= 0 || amount > user.Balance)
			return Json(new { success = false, message = "Неверная сумма для вывода" });

		user.Balance -= amount;

		var pendingRequest = new PendingRequest
		{
			UserId = user.Id,
			Type = RequestTypeWithdrawal,
			Amount = amount,
			Bank = bank,
			CardOrPhone = cardOrPhone,
			CreatedAt = DateTime.UtcNow,
			IsCompleted = false
		};

		_context.PendingRequests.Add(pendingRequest);

		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = TransactionTypeWithdrawal,
			Amount = -amount,
			Description = $"Заявка на вывод средств на сумму {amount:N0} ₽: {bank}, {cardOrPhone}",
			CreatedAt = DateTime.UtcNow,
			ProductName = ""
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		SaveUserToSession(user);

		HttpContext.Session.SetString(SessionKeyPendingWithdrawal, amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
		HttpContext.Session.SetString(SessionKeyPendingWithdrawalBank, bank);
		HttpContext.Session.SetString(SessionKeyPendingWithdrawalCardOrPhone, cardOrPhone);

		return Json(new { success = true, message = "Заявка на вывод средств создана" });
	}

	public IActionResult Logout()
	{
		HttpContext.Session.Remove(SessionKeyUser);
		HttpContext.Session.Remove(SessionKeyPendingTopUp);
		HttpContext.Session.Remove(SessionKeyPendingWithdrawal);
		HttpContext.Session.Remove(SessionKeyPendingWithdrawalBank);
		HttpContext.Session.Remove(SessionKeyPendingWithdrawalCardOrPhone);
		return RedirectToAction("Login");
	}
}
