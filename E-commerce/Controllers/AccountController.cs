using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

public class AccountController : Controller
{
	// GET: /Account/Login
	public IActionResult Login()
	{
		return View();
	}

	// POST: /Account/Login
	[HttpPost]
	public IActionResult Login(string email, string password)
	{
		// Временная проверка (позже добавим базу данных)
		var user = new User
		{
			Id = 1,
			Email = "test@mail.ru",
			Password = "123",
			FirstName = "Иван",
			LastName = "Иванов",
			BirthDate = new DateTime(1990, 5, 15),
			Balance = 15000
		};

		if (email == user.Email && password == user.Password)
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
	public IActionResult Register(User user)
	{
		if (ModelState.IsValid)
		{
			// Присваиваем ID (временное решение)
			user.Id = new Random().Next(100, 1000);
			user.Balance = 10000; // Стартовый баланс

			// Сохраняем в сессии
			HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));
			return RedirectToAction("Profile");
		}

		return View(user);
	}

	// GET: /Account/Profile (Личный кабинет)
	public IActionResult Profile()
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return RedirectToAction("Login");

		var user = JsonSerializer.Deserialize<User>(userJson);
		return View(user);
	}
	// GET: /Account/Logout
	public IActionResult Logout()
	{
		HttpContext.Session.Remove("User");
		return RedirectToAction("Login");
	}
}
