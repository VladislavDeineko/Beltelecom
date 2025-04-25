using Beltelecom.Data;
using Beltelecom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Beltelecom.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PopularProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PopularProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PopularProducts/Index
        public IActionResult Index()
        {
            // Получаем список популярных товаров из базы данных
            var popularProducts = _context.PopularProducts
                                          .Include(p => p.Product)
                                          .Take(4) // или любое другое ограничение, если нужно
                                          .ToList();

            var model = popularProducts.Select(p => p.Product).ToList(); // предполагаем, что у вас есть связь с таблицей продуктов
            return View(model);
        }

        // Метод для очистки текущего списка популярных товаров
        [HttpPost]
        public IActionResult Clear()
        {
            // Удаляем все текущие популярные товары
            var currentPopularProducts = _context.PopularProducts.ToList();
            _context.PopularProducts.RemoveRange(currentPopularProducts);
            _context.SaveChanges();

            return Ok();
        }

        // POST: /PopularProducts/Add
        [HttpPost]
        public IActionResult Add(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product != null)
            {
                var existingProduct = _context.PopularProducts
                                              .FirstOrDefault(p => p.ProductId == productId);

                if (existingProduct == null)
                {
                    _context.PopularProducts.Add(new PopularProduct { ProductId = productId });
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        // POST: /PopularProducts/Remove
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var popularProduct = _context.PopularProducts
                                         .FirstOrDefault(p => p.ProductId == productId);

            if (popularProduct != null)
            {
                _context.PopularProducts.Remove(popularProduct);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: /PopularProducts/Search
        [HttpGet]
        public IActionResult Search(string query)
        {
            var products = _context.Products
                                   .Where(p => p.Name.Contains(query))
                                   .Take(10)
                                   .ToList();

            return Json(products);
        }
    }
}
