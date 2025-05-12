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
        var popularProductIds = _context.PopularProducts.Select(pp => pp.ProductId).ToList();
        var products = _context.Products
                                      .Where(p => popularProductIds.Contains(p.Id))
                                      .ToList();

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

    [HttpPost]
    public async Task<IActionResult> SendFeedback(FeedbackModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var emailSender = new EmailSender();
                await emailSender.SendFeedbackToAdminAsync(model.Name, model.Email, model.Message);

                TempData["Message"] = "Ваше сообщение успешно отправлено!";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Произошла ошибка при отправке сообщения.";
            }
        }
        return View("Contact", model); // Вернуть на страницу контактов, если есть ошибки
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
    public IActionResult About()
    {
        return View();
    }
}
