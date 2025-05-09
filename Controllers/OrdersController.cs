using Beltelecom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Beltelecom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Beltelecom.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUserPhone()
        {
            var userId = _userManager.GetUserId(User);

            var dealInfo = await _context.UserDealInfos
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (dealInfo != null && !string.IsNullOrEmpty(dealInfo.PhoneNumber))
            {
                return Json(new { phoneNumber = dealInfo.PhoneNumber });
            }

            return Json(new { phoneNumber = (string?)null });
        }

        public class PlaceOrderRequest
        {
            public int ProductId { get; set; }
            public bool RequiresSetup { get; set; }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound("Товар не найден.");

            var userDealInfo = await _context.UserDealInfos.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (userDealInfo == null || string.IsNullOrEmpty(userDealInfo.PhoneNumber))
                return BadRequest("Пожалуйста, укажите номер телефона в личном кабинете.");

            var order = new Order
            {
                UserId = user.Id,
                ProductId = product.Id,
                TotalAmount = product.Price,
                PaidAmount = 0m,
                InstallmentMonths = product.InstallmentMonths,
                OrderDate = DateTime.UtcNow,
                IsActive = false,
                IsCompleted = false,
                RequiresSetup = request.RequiresSetup
            };

            product.Stock -= 1;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
