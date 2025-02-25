using Beltelecom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Models;

namespace Beltelecom.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Извлекаем только 4 товара с наименьшим количеством на складе
            var products = await _context.Products
                                         .OrderBy(p => p.Stock)  // Сортировка по Stock
                                         .Take(4)                // Ограничение 4 товарами
                                         .ToListAsync();

            return View(products);  // Передаем в представление только эти 4 товара
        }
    }
}
