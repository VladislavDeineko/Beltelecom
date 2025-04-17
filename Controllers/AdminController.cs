using Microsoft.AspNetCore.Mvc;
using Beltelecom.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Beltelecom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Beltelecom.Models.ViewModels;
using static NuGet.Packaging.PackagingConstants;

namespace Beltelecom.Controllers
{
    [Authorize(Roles = "Admin")] // Доступ только для администраторов
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Страница для просмотра всех товаров
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.Include(p => p.Category).Include(p => p.Brand).ToListAsync();
            return View(products);
        }

        // Страница для добавления нового товара
        public IActionResult Create()
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name");
            return View();
        }

        // Обработка добавления нового товара
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Products));
            }
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        // Страница для редактирования товара
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        // Обработка редактирования товара
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,CategoryId,BrandId,ImageUrl")] Product product)
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
                    if (!_context.Products.Any(e => e.Id == product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Products));
            }

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        // Страница для удаления товара
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // Обработка удаления товара
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        // Страница для просмотра входящих заказов с поиском и сортировкой
        public async Task<IActionResult> IncomingOrders(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

            var ordersQuery = _context.Orders
                .Where(o => !o.IsActive && !o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = _context.UserDealInfos.ToList();

            // Сопоставление UserId → ФИО
            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            // Поиск по ФИО
            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            // Сортировка по дате
            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }



        public async Task<IActionResult> ActiveOrders(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

            var ordersQuery = _context.Orders
                .Where(o => o.IsActive && !o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = _context.UserDealInfos.ToList();

            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.IsActive = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(IncomingOrders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.IsCompleted = true;
            order.CompletedDate = DateTime.UtcNow;
            order.IsActive = false;

            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ActiveOrders));
        }

        public async Task<IActionResult> CompletedOrders(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

            var ordersQuery = _context.Orders
                .Where(o => o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = _context.UserDealInfos.ToList();

            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }




    }
}
