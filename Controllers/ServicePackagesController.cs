using Beltelecom.Data;
using Beltelecom.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace Beltelecom.Controllers
{
    public class ServicePackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ServicePackagesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var allPackages = await _context.ServicePackages.ToListAsync();

            ServicePackage? currentUserPackage = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    var userDealInfo = await _context.UserDealInfos
                        .Include(u => u.ServicePackage)
                        .FirstOrDefaultAsync(u => u.UserId == user.Id);

                    currentUserPackage = userDealInfo?.ServicePackage;
                }
            }
            ViewBag.CurrentUserPackage = currentUserPackage;
            return View(allPackages);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int servicePackageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var userInfo = await _context.UserDealInfos.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (userInfo == null ||
                string.IsNullOrWhiteSpace(userInfo.FirstName) ||
                string.IsNullOrWhiteSpace(userInfo.LastName) ||
                string.IsNullOrWhiteSpace(userInfo.MiddleName) ||
                string.IsNullOrWhiteSpace(userInfo.PhoneNumber))
            {
                TempData["Error"] = "Для отправки заявки на смену пакета услуг укажите Личные данные и номер телефона в личном кабинете.";
                return RedirectToAction("Index", "ServicePackages");
            }

            var existingPackage = await _context.ServicePackages.FindAsync(servicePackageId);
            if (existingPackage == null)
                return NotFound();

            var order = new ServicePackageOrder
            {
                UserId = user.Id,
                ServicePackageId = servicePackageId,
                PricePerMonth = existingPackage.Price,
                OrderDate = DateTime.UtcNow,
                IsConfirmed = false,
                IsCompleted = false
            };

            _context.ServicePackageOrders.Add(order);
            await _context.SaveChangesAsync();

            var orderId = order.Id;
            var callbackUrl = Url.Action(
                "ConfirmServicePackage",
                "ServicePackages",
            new { orderId = orderId },
            protocol: Request.Scheme);
            Console.WriteLine("WWW"+ callbackUrl);
            await _emailSender.SendEmailAsync(user.Email, "Подтверждение заявки на смену пакета услуг",
            $@"
            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; padding: 20px; border-radius: 8px;"">
                <h2 style=""color: #2e6c80;"">Подтверждение заявки на смену пакета услуг</h2>
                <p>Вы подали заявку на смену пакета на <strong>{order.ServicePackage.Name}</strong>.</p>
                <p>Для подтверждения, пожалуйста, перейдите по ссылке ниже:</p>
                <p style=""text-align: center; margin: 30px 0;"">
                    <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'
                       style=""background-color: #2e6c80; color: white; padding: 12px 20px; text-decoration: none; border-radius: 5px;"">
                        Подтвердить заявку
                    </a>
                </p>
                <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">
                <p style=""font-size: 12px; color: #777;"">
                    Если вы не подавали заявку, просто проигнорируйте это письмо.
                </p>
            </div>");

            TempData["Success"] = $"Заявка на смену пакета услуг успешно отправлена. " +
                      $"Вам нужно подтвердить её в письме, которое пришло на вашу электронную почту {user.Email}.";
            return RedirectToAction("Index", "ServicePackages");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmServicePackage(int orderId)
        {
            var order = await _context.ServicePackageOrders.FindAsync(orderId);
            if (order == null || order.IsConfirmed)
            {
                return NotFound(); // или страница с сообщением
            }

            order.IsConfirmed = true;
            await _context.SaveChangesAsync();

            return View("ServicePackageConfirmed"); // создадим это представление позже
        }

    }
}
