namespace SimpleEcommerce.Extensions;

public static class HttpContextExtensions
{
	/// <summary>
	/// Возвращает IP-адрес клиента (с учётом X-Forwarded-For и X-Real-IP при работе за прокси).
	/// </summary>
	public static string? GetClientIpAddress(this HttpContext httpContext)
	{
		var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		if (!string.IsNullOrEmpty(forwardedFor))
		{
			var clientIp = forwardedFor.Split(',', StringSplitOptions.TrimEntries).FirstOrDefault();
			if (!string.IsNullOrEmpty(clientIp))
				return clientIp;
		}
		var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
		if (!string.IsNullOrEmpty(realIp))
			return realIp;
		return httpContext.Connection.RemoteIpAddress?.ToString();
	}
}
