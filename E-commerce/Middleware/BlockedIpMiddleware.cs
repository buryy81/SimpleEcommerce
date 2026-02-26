using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Controllers;
using SimpleEcommerce.Data;
using SimpleEcommerce.Extensions;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Middleware;

/// <summary>
/// Блокирует доступ к сайту с IP из списка заблокированных. Администратор (по сессии) всегда допускается.
/// </summary>
public class BlockedIpMiddleware
{
	private readonly RequestDelegate _next;

	public BlockedIpMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
	{
		var clientIp = context.GetClientIpAddress();
		if (string.IsNullOrEmpty(clientIp))
		{
			await _next(context);
			return;
		}

		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var isBlocked = await db.BlockedIps.AnyAsync(b => b.Ip == clientIp);
		if (!isBlocked)
		{
			await _next(context);
			return;
		}

		// Проверяем, не администратор ли (админ всегда допускается)
		var userJson = context.Session.GetString("User");
		if (!string.IsNullOrEmpty(userJson))
		{
			try
			{
				var user = JsonSerializer.Deserialize<User>(userJson);
				if (user?.Email == BaseController.AdminEmail)
				{
					await _next(context);
					return;
				}
			}
			catch
			{
				// игнорируем ошибку десериализации
			}
		}

		context.Response.StatusCode = 403;
		context.Response.ContentType = "text/html; charset=utf-8";
		await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><title>Доступ запрещён</title></head>
<body style=""font-family:sans-serif;text-align:center;padding:4rem;"">
<h1>403 — Доступ запрещён</h1>
<p>Доступ с вашего IP-адреса заблокирован администратором.</p>
</body>
</html>");
	}
}
