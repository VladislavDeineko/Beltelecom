using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Beltelecom.Models;
using Beltelecom.Data;
using Microsoft.EntityFrameworkCore;

namespace Beltelecom.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;  // Добавляем контекст для работы с базой данных

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;  // Инициализируем контекст
    }

    public async Task<IActionResult> Index()
    {
        // Извлекаем 4 товара с наименьшим количеством на складе
        var products = await _context.Products
                                     .OrderBy(p => p.Stock)
                                     .Take(4)
                                     .ToListAsync();

        // Извлекаем категории 
        var categories = await _context.Categories
            .Include(c => c.Products) // обязательно для Any()
            .Where(c => c.Products.Any())
            .OrderBy(c => c.Name)
            .Take(6)
            .ToListAsync();

        // Создаем HomeViewModel
        var model = new HomeViewModel
        {
            Products = products,
            Categories = categories
        };

        return View(model);
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Categories()
    {
        return RedirectToAction("Index", "Categories");
    }

    public async Task<IActionResult> GetLowStockProducts()
    {
        var products = await _context.Products
                                     .OrderBy(p => p.Stock) // Сортируем по количеству
                                     .Take(4) // Берем 4 товара
                                     .ToListAsync();

        return PartialView("_ProductsPartial", products);
    }

}
