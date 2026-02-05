using SimpleEcommerce.Models;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace SimpleEcommerce.Controllers;

public class ProductController : Controller
{
	//GET: /Product/
	public IActionResult Index()
	{
		//data for test
		var products = new List<Product>
		{
			new Product { Id = 1, Name = "Ноутбук HP", Price = 45000, ImageUrl = "https://picsum.photos/300/200?random=1" },
			new Product { Id = 2, Name = "Смартфон Xiaomi", Price = 25000, ImageUrl = "https://picsum.photos/300/200?random=2" },
			new Product { Id = 3, Name = "Наушники Sony", Price = 5000, ImageUrl = "https://picsum.photos/300/200?random=3" },
			new Product { Id = 4, Name = "Клавиатура Logitech", Price = 3000, ImageUrl = "https://picsum.photos/300/200?random=4" }
		};

		return View(products);
	}

	public IActionResult Details(int id)
	{
		var product = new Product
		{
			Id = id,
			Name = $"Товар {id}",
			Price = 1000 * id,
			Description = "Отличный товар для покупки",
			ImageUrl = $"https://picsum.photos/300/200?random={id}"
		};

		return View(product);
	}
}
