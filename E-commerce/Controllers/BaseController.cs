using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SimpleEcommerce.Data;
using SimpleEcommerce.Extensions;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Controllers;

/// <summary>
/// Базовый контроллер с общими методами работы с сессией и сериализацией пользователя.
/// </summary>
public abstract class BaseController : Controller
{
	protected static readonly JsonSerializerOptions JsonOptions = new()
	{
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public const string AdminEmail = "admin@admin.com";

	protected const string SessionKeyUser = "User";
	protected const string SessionKeyPendingTopUp = "PendingTopUp";
	protected const string SessionKeyPendingWithdrawal = "PendingWithdrawal";
	protected const string SessionKeyPendingWithdrawalBank = "PendingWithdrawalBank";
	protected const string SessionKeyPendingWithdrawalCardOrPhone = "PendingWithdrawalCardOrPhone";

	protected const string RequestTypeTopUp = "TopUp";
	protected const string RequestTypeWithdrawal = "Withdrawal";

	protected const string TransactionTypeTopUp = "Пополнение";
	protected const string TransactionTypeWithdrawal = "Вывод средств";
	protected const string TransactionTypePurchase = "Покупка";
	protected const string TransactionTypeDeduction = "Списание";

	/// <summary>
	/// Возвращает пользователя из сессии или null.
	/// </summary>
	protected User? GetSessionUser()
	{
		var userJson = HttpContext.Session.GetString(SessionKeyUser);
		if (string.IsNullOrEmpty(userJson))
			return null;
		return JsonSerializer.Deserialize<User>(userJson);
	}

	/// <summary>
	/// Сохраняет пользователя в сессию (без навигационных свойств).
	/// </summary>
	protected void SaveUserToSession(User user)
	{
		HttpContext.Session.SetString(SessionKeyUser, JsonSerializer.Serialize(user, JsonOptions));
	}

	/// <summary>
	/// Проверяет, является ли текущий пользователь из сессии администратором.
	/// </summary>
	protected bool IsAdmin()
	{
		var user = GetSessionUser();
		return user?.Email == AdminEmail;
	}

	/// <summary>
	/// Возвращает IP-адрес клиента (с учётом X-Forwarded-For и X-Real-IP при работе за прокси).
	/// </summary>
	protected string? GetClientIpAddress() => HttpContext.GetClientIpAddress();

}
