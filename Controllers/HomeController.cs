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
        // Извлекаем список продуктов из базы данных
        var products = await _context.Products.ToListAsync();

        // Передаем данные о продуктах в представление
        return View(products);
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

}
