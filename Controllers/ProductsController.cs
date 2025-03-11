using Beltelecom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;

namespace Beltelecom.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        public async Task<IActionResult> Catalog(string category, string brand, string sortOrder)
        {
            var products = _context.Products.AsQueryable();

            // Фильтрация по категории
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category.Name == category);
            }

            // Фильтрация по бренду
            if (!string.IsNullOrEmpty(brand))
            {
                products = products.Where(p => p.Brand.Name == brand);
            }

            // Сортировка
            if (sortOrder == "price_asc")
            {
                products = products.OrderBy(p => p.Price);
            }
            else if (sortOrder == "price_desc")
            {
                products = products.OrderByDescending(p => p.Price);
            }
            else if (sortOrder == "name_asc")
            {
                products = products.OrderBy(p => p.Name);
            }
            else if (sortOrder == "name_desc")
            {
                products = products.OrderByDescending(p => p.Name);
            }

            var catalogViewModel = new CatalogViewModel
            {
                Products = await products.ToListAsync(),
                Categories = await _context.Categories.Select(c => c.Name).ToListAsync(),
                SelectedCategory = category,
                SelectedBrand = brand,
                SortOrder = sortOrder
            };

            return View(catalogViewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.IsFavorite = await _context.Favorites
                    .AnyAsync(f => f.UserId == user.Id && f.ProductId == id);
            }
            else
            {
                ViewBag.IsFavorite = false; // Если пользователь не авторизован, считаем, что товара нет в избранном
            }

            return View(product);
        }
    }
}
