using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Data;
using Beltelecom.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Beltelecom.Controllers
{
    [Authorize] // Только авторизованные пользователи могут управлять избранным
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public class ToggleFavoriteRequest
        {
            public int ProductId { get; set; }
        }

        public FavoritesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favoriteProducts = await _context.Favorites
                .Where(f => f.UserId == user.Id)
                .Include(f => f.Product)
                .Select(f => f.Product)
                .ToListAsync();

            return View(favoriteProducts);
        }

        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Пользователь не найден." });
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ProductId == productId);

            if (existingFavorite != null)
            {
                return Json(new { success = false, message = "Товар уже в избранном." });
            }

            var favorite = new Favorite
            {
                UserId = user.Id,
                ProductId = productId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Пользователь не найден." });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ProductId == productId);

            if (favorite == null)
            {
                return Json(new { success = false, message = "Товар не найден в избранном." });
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Пользователь не найден." });
            }

            var favoriteItem = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ProductId == request.ProductId);

            if (favoriteItem != null)
            {
                // Если товар уже в избранном — удаляем его
                _context.Favorites.Remove(favoriteItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                // Если товара нет в избранном — добавляем его
                var newFavorite = new Favorite
                {
                    UserId = user.Id,
                    ProductId = request.ProductId
                };

                _context.Favorites.Add(newFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
        }

    }

}
