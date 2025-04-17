using Beltelecom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

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
                .Include(p => p.Specifications)
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

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Получаем данные для выпадающих списков
            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,CategoryId,BrandId,ImageUrl,Description,WarrantyMonths")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index)); // Перенаправляем на список товаров
            }

            // Получаем данные для выпадающих списков
            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);

            return View(product);
        }

        // Метод для подтверждения удаления товара
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);  // Отправляем товар на страницу подтверждения
        }

        // Метод для выполнения удаления товара
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ProductList));  // Перенаправляем на список товаров
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // GET: Product/ProductList
        public async Task<IActionResult> ProductList()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            return View("ProductList", products);  // Указываем явное имя представления
        }
    }
}
