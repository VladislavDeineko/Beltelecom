using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Data;
using Beltelecom.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Beltelecom.Controllers
{

    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string category, string brand, string sortOrder)
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
            products = sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "name_asc" => products.OrderBy(p => p.Name),
                "name_desc" => products.OrderByDescending(p => p.Name),
                _ => products
            };

            var categories = await _context.Products.Select(p => p.Category.Name).Distinct().ToListAsync();
            var brands = await _context.Products
                    .Select(p => p.Brand.Name)  // Преобразуем в список строк
                    .Distinct()
                    .ToListAsync();
            var viewModel = new CatalogViewModel
            {
                Products = await products.ToListAsync(),
                Categories = categories,
                Brands = brands,
                SelectedCategory = category,
                SelectedBrand = brand,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }
    }
}