using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;
using SimpleEcommerce.Services;
using System.Text.Json;

namespace SimpleEcommerce.Controllers;

public class CartController : BaseController
{
    private const string SessionKeyCart = "Cart";

    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получить корзину из сессии
    /// </summary>
    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString(SessionKeyCart);
        if (string.IsNullOrEmpty(cartJson))
            return new List<CartItem>();
        return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
    }

    /// <summary>
    /// Сохранить корзину в сессию
    /// </summary>
    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(SessionKeyCart, JsonSerializer.Serialize(cart, JsonOptions));
    }


    /// <summary>
    /// Страница корзины
    /// </summary>
    public IActionResult Index()
    {
        ViewData["Title"] = "Корзина - AiMarket";
        var user = GetSessionUser();
        if (user == null)
            return RedirectToAction("Login", "Account");

        var cart = GetCart();
        ViewBag.UserBalance = user.Balance;
        ViewBag.TotalAmount = cart.Sum(item => item.Total);
        ViewBag.HasEnoughBalance = user.Balance >= cart.Sum(item => item.Total);

        return View(cart);
    }

    /// <summary>
    /// Добавить товар в корзину
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult AddToCart(int productId, int quantity = 1)
    {
        var user = GetSessionUser();
        if (user == null)
            return Json(new { success = false, redirect = true, url = Url.Action("Login", "Account") });

        var products = ProductService.GetAllProducts();
        var product = products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
            return Json(new { success = false, message = "Товар не найден" });

        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(item => item.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                StoreName = product.StoreName,
                Quantity = quantity
            });
        }

        SaveCart(cart);

        var cartCount = cart.Sum(item => item.Quantity);
        var cartTotal = cart.Sum(item => item.Total);

        return Json(new { 
            success = true, 
            message = "Товар добавлен в корзину",
            cartCount,
            cartTotal
        });
    }

    /// <summary>
    /// Обновить количество товара в корзине
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult UpdateQuantity(int productId, int quantity)
    {
        var user = GetSessionUser();
        if (user == null)
            return Json(new { success = false, message = "Необходима авторизация" });

        if (quantity <= 0)
            return Json(new { success = false, message = "Количество должно быть больше нуля" });

        var cart = GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Json(new { success = false, message = "Товар не найден в корзине" });

        item.Quantity = quantity;
        SaveCart(cart);

        var cartCount = cart.Sum(item => item.Quantity);
        var cartTotal = cart.Sum(item => item.Total);
        var itemTotal = item.Total;

        return Json(new { 
            success = true, 
            cartCount,
            cartTotal,
            itemTotal
        });
    }

    /// <summary>
    /// Удалить товар из корзины
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Remove(int productId)
    {
        var user = GetSessionUser();
        if (user == null)
            return Json(new { success = false, message = "Необходима авторизация" });

        var cart = GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Json(new { success = false, message = "Товар не найден в корзине" });

        cart.Remove(item);
        SaveCart(cart);

        var cartCount = cart.Sum(item => item.Quantity);
        var cartTotal = cart.Sum(item => item.Total);

        return Json(new { 
            success = true, 
            message = "Товар удален из корзины",
            cartCount,
            cartTotal
        });
    }

    /// <summary>
    /// Очистить корзину
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Clear()
    {
        var user = GetSessionUser();
        if (user == null)
            return Json(new { success = false, message = "Необходима авторизация" });

        HttpContext.Session.Remove(SessionKeyCart);

        return Json(new { success = true, message = "Корзина очищена" });
    }

    /// <summary>
    /// Получить количество товаров в корзине (для навигации)
    /// </summary>
    [HttpGet]
    public IActionResult GetCartCount()
    {
        var cart = GetCart();
        var count = cart.Sum(item => item.Quantity);
        return Json(new { count });
    }

    /// <summary>
    /// Оформить заказ (купить все товары из корзины)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var sessionUser = GetSessionUser();
        if (sessionUser == null)
            return Json(new { success = false, redirect = true, url = Url.Action("Login", "Account") });

        var user = await _context.Users.FindAsync(sessionUser.Id);
        if (user == null)
            return Json(new { success = false, message = "Пользователь не найден" });

        var cart = GetCart();
        if (cart.Count == 0)
            return Json(new { success = false, message = "Корзина пуста" });

        var totalAmount = cart.Sum(item => item.Total);
        if (user.Balance < totalAmount)
            return Json(new { success = false, message = "Недостаточно средств на балансе" });

        var products = ProductService.GetAllProducts();

        // Создаем транзакции для каждого товара
        foreach (var cartItem in cart)
        {
            var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
            if (product == null) continue;

            var transaction = new Transaction
            {
                UserId = user.Id,
                Type = TransactionTypePurchase,
                Amount = -cartItem.Total,
                Description = $"Покупка товара: {cartItem.Name} (x{cartItem.Quantity})",
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
        }

        user.Balance -= totalAmount;
        await _context.SaveChangesAsync();

        // Обновляем пользователя в сессии
        SaveUserToSession(user);

        // Очищаем корзину
        HttpContext.Session.Remove(SessionKeyCart);

        return Json(new { 
            success = true, 
            message = "Заказ успешно оформлен!",
            newBalance = user.Balance
        });
    }
}
