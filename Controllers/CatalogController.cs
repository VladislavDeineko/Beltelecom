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

        public async Task<IActionResult> Index(string category, string brand, string sortOrder, int page = 1, int pageSize = 8)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // Фильтрация по категории
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category.Name == category);
            }

            // Фильтрация по бренду
            if (!string.IsNullOrEmpty(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand.Name == brand);
            }

            // Сортировка
            productsQuery = sortOrder switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "name_asc" => productsQuery.OrderBy(p => p.Name),
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                _ => productsQuery
            };

            // Общее количество товаров после фильтрации
            var totalCount = await productsQuery.CountAsync();

            // Выбор нужной страницы
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Products
                .Select(p => p.Category.Name)
                .Distinct()
                .ToListAsync();

            var brands = await _context.Products
                .Select(p => p.Brand.Name)
                .Distinct()
                .ToListAsync();

            var viewModel = new CatalogViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                SelectedCategory = category,
                SelectedBrand = brand,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return View(viewModel);
        }
    }
}