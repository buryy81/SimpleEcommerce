using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEcommerce.Controllers;

public class ProductController : Controller
{
	private readonly ApplicationDbContext _context;

	public ProductController(ApplicationDbContext context)
	{
		_context = context;
	}
	private List<Product> GetAllProducts()
	{
		return new List<Product>
		{
			new Product { Id = 1, Name = "Ноутбук HP Pavilion 15", Price = 45999, Description = "Производительный ноутбук для работы и учебы. Экран 15.6 дюймов, процессор Intel Core i5, 8 ГБ ОЗУ, SSD 256 ГБ.", ImageUrl = "https://picsum.photos/400/300?random=1" },
			new Product { Id = 2, Name = "Смартфон Xiaomi Redmi Note 12", Price = 24999, Description = "Современный смартфон с отличной камерой и быстрой зарядкой. Экран 6.67 дюймов, 128 ГБ памяти.", ImageUrl = "https://picsum.photos/400/300?random=2" },
			new Product { Id = 3, Name = "Наушники Sony WH-1000XM4", Price = 24990, Description = "Беспроводные наушники с активным шумоподавлением. Звук премиум-класса, до 30 часов работы.", ImageUrl = "https://picsum.photos/400/300?random=3" },
			new Product { Id = 4, Name = "Клавиатура Logitech MX Keys", Price = 8990, Description = "Беспроводная клавиатура для работы. Подсветка клавиш, эргономичный дизайн, до 5 месяцев работы от батареи.", ImageUrl = "https://picsum.photos/400/300?random=4" },
			new Product { Id = 5, Name = "Планшет Apple iPad Air", Price = 59999, Description = "Мощный планшет с дисплеем 10.9 дюймов. Чип M1, поддержка Apple Pencil, 256 ГБ памяти.", ImageUrl = "https://picsum.photos/400/300?random=5" },
			new Product { Id = 6, Name = "Умные часы Apple Watch Series 9", Price = 39999, Description = "Смарт-часы с GPS и функцией измерения здоровья. Дисплей Always-On, до 18 часов работы.", ImageUrl = "https://picsum.photos/400/300?random=6" },
			new Product { Id = 7, Name = "Игровая мышь Razer DeathAdder V3", Price = 6990, Description = "Профессиональная игровая мышь с оптическим сенсором 30K DPI. Эргономичный дизайн для правшей.", ImageUrl = "https://picsum.photos/400/300?random=7" },
			new Product { Id = 8, Name = "Монитор Samsung Odyssey G7", Price = 34999, Description = "Игровой монитор 27 дюймов с изогнутым экраном. Разрешение QHD, частота 240 Гц, HDR600.", ImageUrl = "https://picsum.photos/400/300?random=8" },
			new Product { Id = 9, Name = "Веб-камера Logitech C920", Price = 5990, Description = "HD веб-камера для видеозвонков. Разрешение 1080p, автофокус, стереозвук.", ImageUrl = "https://picsum.photos/400/300?random=9" },
			new Product { Id = 10, Name = "Внешний жесткий диск Seagate 2TB", Price = 4990, Description = "Портативный жесткий диск USB 3.0. Объем 2 ТБ, компактный дизайн, высокая скорость передачи данных.", ImageUrl = "https://picsum.photos/400/300?random=10" },
			new Product { Id = 11, Name = "Беспроводная колонка JBL Charge 5", Price = 12990, Description = "Портативная колонка с мощным звуком. Водонепроницаемая, до 20 часов работы, функция Powerbank.", ImageUrl = "https://picsum.photos/400/300?random=11" },
			new Product { Id = 12, Name = "Роутер ASUS RT-AX3000", Price = 8990, Description = "Wi-Fi 6 роутер с высокой скоростью. Двухдиапазонный, поддержка Mesh, родительский контроль.", ImageUrl = "https://picsum.photos/400/300?random=12" },
			new Product { Id = 13, Name = "Игровая консоль PlayStation 5", Price = 49999, Description = "Новейшая игровая консоль от Sony. SSD 825 ГБ, поддержка 4K и 120 FPS, обратная совместимость.", ImageUrl = "https://picsum.photos/400/300?random=13" },
			new Product { Id = 14, Name = "Электронная книга PocketBook Touch HD 3", Price = 14990, Description = "Электронная книга с экраном E Ink 7.8 дюймов. Подсветка, водонепроницаемая, 16 ГБ памяти.", ImageUrl = "https://picsum.photos/400/300?random=14" },
			new Product { Id = 15, Name = "Фитнес-браслет Xiaomi Mi Band 8", Price = 2990, Description = "Умный браслет для отслеживания активности. Мониторинг сна, пульса, 150+ режимов тренировок.", ImageUrl = "https://picsum.photos/400/300?random=15" },
			new Product { Id = 16, Name = "Принтер HP LaserJet Pro", Price = 19990, Description = "Лазерный принтер для офиса. Печать до 42 стр/мин, двусторонняя печать, Wi-Fi.", ImageUrl = "https://picsum.photos/400/300?random=16" },
			new Product { Id = 17, Name = "Игровая клавиатура Corsair K70", Price = 12990, Description = "Механическая игровая клавиатура с RGB подсветкой. Переключатели Cherry MX, антипризрачный режим.", ImageUrl = "https://picsum.photos/400/300?random=17" },
			new Product { Id = 18, Name = "SSD накопитель Samsung 1TB", Price = 6990, Description = "Быстрый SSD накопитель NVMe. Скорость чтения до 3500 МБ/с, идеален для игр и работы.", ImageUrl = "https://picsum.photos/400/300?random=18" },
			new Product { Id = 19, Name = "Блок питания Cooler Master 750W", Price = 8990, Description = "Мощный блок питания 80 Plus Gold. Модульная конструкция, тихая работа, надежность.", ImageUrl = "https://picsum.photos/400/300?random=19" },
			new Product { Id = 20, Name = "Видеокарта NVIDIA RTX 4060", Price = 34999, Description = "Игровая видеокарта с поддержкой DLSS 3.0. 8 ГБ видеопамяти, отличная производительность.", ImageUrl = "https://picsum.photos/400/300?random=20" },
			new Product { Id = 21, Name = "Материнская плата ASUS B550", Price = 12990, Description = "Материнская плата для процессоров AMD. Поддержка PCIe 4.0, Wi-Fi 6, USB 3.2.", ImageUrl = "https://picsum.photos/400/300?random=21" },
			new Product { Id = 22, Name = "Процессор AMD Ryzen 7 5800X", Price = 24990, Description = "Мощный процессор для игр и работы. 8 ядер, 16 потоков, базовая частота 3.8 ГГц.", ImageUrl = "https://picsum.photos/400/300?random=22" },
			new Product { Id = 23, Name = "Оперативная память Corsair 32GB", Price = 8990, Description = "Оперативная память DDR4 3200 МГц. Объем 32 ГБ, двухканальный режим, низкая задержка.", ImageUrl = "https://picsum.photos/400/300?random=23" },
			new Product { Id = 24, Name = "Корпус NZXT H510", Price = 7990, Description = "Стильный корпус для ПК с прозрачной боковой панелью. Управление кабелями, хорошая вентиляция.", ImageUrl = "https://picsum.photos/400/300?random=24" },
			new Product { Id = 25, Name = "Кулер для процессора Noctua NH-D15", Price = 8990, Description = "Мощный башенный кулер для процессора. Тихая работа, отличное охлаждение.", ImageUrl = "https://picsum.photos/400/300?random=25" },
			new Product { Id = 26, Name = "Микрофон Blue Yeti", Price = 12990, Description = "USB микрофон для стриминга и записи. Несколько режимов записи, качественный звук.", ImageUrl = "https://picsum.photos/400/300?random=26" },
			new Product { Id = 27, Name = "Студийные наушники Audio-Technica", Price = 14990, Description = "Профессиональные наушники для мониторинга. Закрытый тип, точное звучание.", ImageUrl = "https://picsum.photos/400/300?random=27" },
			new Product { Id = 28, Name = "Графический планшет Wacom Intuos", Price = 8990, Description = "Графический планшет для дизайнеров. Чувствительность к нажатию, компактный размер.", ImageUrl = "https://picsum.photos/400/300?random=28" },
			new Product { Id = 29, Name = "Смарт-телевизор Samsung 55", Price = 59999, Description = "4K телевизор с Smart TV. Диагональ 55 дюймов, HDR10+, голосовое управление.", ImageUrl = "https://picsum.photos/400/300?random=29" },
			new Product { Id = 30, Name = "Игровой коврик Razer Goliathus", Price = 1990, Description = "Большой игровой коврик для мыши. Точное позиционирование, долговечный материал.", ImageUrl = "https://picsum.photos/400/300?random=30" },
			new Product { Id = 31, Name = "Веб-камера Razer Kiyo Pro", Price = 12990, Description = "Профессиональная веб-камера для стриминга. Разрешение 1080p 60fps, HDR, кольцевая подсветка.", ImageUrl = "https://picsum.photos/400/300?random=31" },
			new Product { Id = 32, Name = "Игровой стул Secretlab Titan", Price = 49999, Description = "Эргономичный игровой стул премиум класса. Регулировка высоты и наклона, качественные материалы.", ImageUrl = "https://picsum.photos/400/300?random=32" },
			new Product { Id = 33, Name = "Игровой стол Arozzi Arena", Price = 24990, Description = "Большой игровой стол с подставкой для мониторов. Управление кабелями, прочная конструкция.", ImageUrl = "https://picsum.photos/400/300?random=33" },
			new Product { Id = 34, Name = "Беспроводная зарядка Samsung", Price = 2990, Description = "Быстрая беспроводная зарядка для смартфонов. Мощность 15W, компактный дизайн.", ImageUrl = "https://picsum.photos/400/300?random=34" },
			new Product { Id = 35, Name = "Powerbank Xiaomi 20000mAh", Price = 2990, Description = "Мощный внешний аккумулятор. Емкость 20000 мАч, быстрая зарядка, два USB порта.", ImageUrl = "https://picsum.photos/400/300?random=35" },
			new Product { Id = 36, Name = "Кабель USB-C Apple", Price = 1990, Description = "Оригинальный кабель USB-C для зарядки. Длина 2 метра, быстрая передача данных.", ImageUrl = "https://picsum.photos/400/300?random=36" },
			new Product { Id = 37, Name = "Чехол для iPhone 15 Pro", Price = 2990, Description = "Защитный чехол с MagSafe. Прозрачный дизайн, защита от ударов, совместимость с беспроводной зарядкой.", ImageUrl = "https://picsum.photos/400/300?random=37" },
			new Product { Id = 38, Name = "Защитное стекло для экрана", Price = 990, Description = "Закаленное защитное стекло 9H. Полное покрытие экрана, защита от царапин и ударов.", ImageUrl = "https://picsum.photos/400/300?random=38" },
			new Product { Id = 39, Name = "Автомобильный держатель для телефона", Price = 1990, Description = "Магнитный держатель для автомобиля. Универсальный крепеж, надежная фиксация.", ImageUrl = "https://picsum.photos/400/300?random=39" },
			new Product { Id = 40, Name = "Умная колонка Яндекс Станция", Price = 8990, Description = "Голосовой помощник с Алисой. Управление умным домом, музыка, новости и погода.", ImageUrl = "https://picsum.photos/400/300?random=40" }
		};
	}

	//GET: /Product/
	public IActionResult Index(int page = 1)
	{
		var allProducts = GetAllProducts();
		const int pageSize = 8;
		
		var totalPages = (int)Math.Ceiling(allProducts.Count / (double)pageSize);
		if (page < 1) page = 1;
		if (page > totalPages) page = totalPages;
		
		var products = allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
		
		ViewBag.CurrentPage = page;
		ViewBag.TotalPages = totalPages;
		ViewBag.TotalProducts = allProducts.Count;
		
		return View(products);
	}

	public IActionResult Details(int id)
	{
		var products = GetAllProducts();
		var product = products.FirstOrDefault(p => p.Id == id);
		
		if (product == null)
		{
			product = new Product
			{
				Id = id,
				Name = $"Товар {id}",
				Price = 1000 * id,
				Description = "Отличный товар для покупки",
				ImageUrl = $"https://picsum.photos/600/600?random={id}"
			};
		}
		else
		{
			// Улучшаем описание для страницы деталей
			product.Description = product.Description + " Идеально подходит для повседневного использования. Гарантия качества и надежности.";
			product.ImageUrl = product.ImageUrl.Replace("/400/300", "/600/600");
		}

		// Проверяем баланс пользователя
		var userJson = HttpContext.Session.GetString("User");
		if (!string.IsNullOrEmpty(userJson))
		{
			var user = JsonSerializer.Deserialize<User>(userJson);
			ViewBag.UserBalance = user.Balance;
			ViewBag.HasEnoughBalance = user.Balance >= product.Price;
		}
		else
		{
			ViewBag.UserBalance = 0;
			ViewBag.HasEnoughBalance = false;
		}

		return View(product);
	}

	// POST: /Product/Purchase
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Purchase(int productId)
	{
		var userJson = HttpContext.Session.GetString("User");
		if (string.IsNullOrEmpty(userJson))
			return Json(new { success = false, message = "Необходимо войти в систему" });

		var sessionUser = JsonSerializer.Deserialize<User>(userJson);
		var user = await _context.Users.FindAsync(sessionUser.Id);

		if (user == null)
			return Json(new { success = false, message = "Пользователь не найден" });

		var products = GetAllProducts();
		var product = products.FirstOrDefault(p => p.Id == productId);

		if (product == null)
			return Json(new { success = false, message = "Товар не найден" });

		if (user.Balance < product.Price)
			return Json(new { success = false, message = "Недостаточно средств на балансе" });

		// Списываем деньги
		user.Balance -= product.Price;

		// Создаем транзакцию
		var transaction = new Transaction
		{
			UserId = user.Id,
			Type = "Покупка",
			Amount = -product.Price,
			Description = $"Покупка товара: {product.Name}",
			ProductId = product.Id,
			ProductName = product.Name,
			CreatedAt = DateTime.UtcNow
		};

		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		// Обновляем сессию
		HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));

		return Json(new { success = true, message = $"Товар '{product.Name}' успешно куплен!", balance = user.Balance });
	}
}
