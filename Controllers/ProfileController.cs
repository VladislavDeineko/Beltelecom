    using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Data;
using Beltelecom.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;

namespace Beltelecom.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ProfileController(UserManager<IdentityUser> userManager, ApplicationDbContext context, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        // Личный кабинет (по умолчанию – персональная информация)
        public async Task<IActionResult> Index()
        {
            ServicePackage? currentUserPackage = null;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userAddress = await _context.UserAddresses.FirstOrDefaultAsync(u => u.UserId == user.Id);
            var userDealInfo = await _context.UserDealInfos.FirstOrDefaultAsync(u => u.UserId == user.Id);

            if (userDealInfo == null)
            {
                // Если записи нет, можно создать новую или отобразить информацию пользователю
                userDealInfo = new UserDealInfo
                {
                    UserId = user.Id,
                    PhoneNumber = user.PhoneNumber // Возможно, вы хотите добавить телефон при создании
                };
            }

            var model = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNum = user.PhoneNumber,
                UserAddress = userAddress ?? new UserAddress(),
                UserDealInfo = userDealInfo ?? new UserDealInfo()
            };

            if (user != null)
            {
                var userDealInfo2 = await _context.UserDealInfos
                    .Include(u => u.ServicePackage)
                    .FirstOrDefaultAsync(u => u.UserId == user.Id);

                currentUserPackage = userDealInfo2?.ServicePackage;
            }
            ViewBag.CurrentUserPackage = currentUserPackage;

            return View(model);
        }

        // Сохранение адреса
        [HttpPost]
        public async Task<IActionResult> UpdateAddress(UserAddress userAddress)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var existingAddress = await _context.UserAddresses.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (existingAddress == null)
            {
                userAddress.UserId = user.Id;
                _context.UserAddresses.Add(userAddress);
            }
            else
            {
                existingAddress.Region = userAddress.Region;
                existingAddress.City = userAddress.City;
                existingAddress.Street = userAddress.Street;
                existingAddress.House = userAddress.House;
                existingAddress.Apartment = userAddress.Apartment;
                _context.UserAddresses.Update(existingAddress);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessageAddress"] = "Адрес успешно обновлен.";
            }
            catch
            {
                TempData["ErrorMessageAddress"] = "Ошибка при сохранении адреса.";
            }

            return RedirectToAction("Index");
        }

        // Сохранение паспортных данных
        [HttpPost]
        public async Task<IActionResult> UpdateDealInfo(UserDealInfo userDealInfo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var existingDealInfo = await _context.UserDealInfos.FirstOrDefaultAsync(u => u.UserId == user.Id);
            if (existingDealInfo == null)
            {
                // Заполнение данных по умолчанию, если их нет
                userDealInfo.UserId = user.Id;
                userDealInfo.LastName = string.IsNullOrEmpty(userDealInfo.LastName) ? "Не указано" : userDealInfo.LastName;
                userDealInfo.FirstName = string.IsNullOrEmpty(userDealInfo.FirstName) ? "Не указано" : userDealInfo.FirstName;
                userDealInfo.MiddleName = string.IsNullOrEmpty(userDealInfo.MiddleName) ? "Не указано" : userDealInfo.MiddleName;
                userDealInfo.PassportNumber = string.IsNullOrEmpty(userDealInfo.PassportNumber) ? "Не указано" : userDealInfo.PassportNumber;
                userDealInfo.PassportIssuedBy = string.IsNullOrEmpty(userDealInfo.PassportIssuedBy) ? "Не указано" : userDealInfo.PassportIssuedBy;
                userDealInfo.PersonalNumber = string.IsNullOrEmpty(userDealInfo.PersonalNumber) ? "Не указано" : userDealInfo.PersonalNumber;
                userDealInfo.PhoneNumber = user.PhoneNumber;
                _context.UserDealInfos.Add(userDealInfo);
            }
            else
            {
                // Заполнение данных по умолчанию для существующих данных
                existingDealInfo.UserId = user.Id;
                existingDealInfo.LastName = string.IsNullOrEmpty(userDealInfo.LastName) ? "Не указано" : userDealInfo.LastName;
                existingDealInfo.FirstName = string.IsNullOrEmpty(userDealInfo.FirstName) ? "Не указано" : userDealInfo.FirstName;
                existingDealInfo.MiddleName = string.IsNullOrEmpty(userDealInfo.MiddleName) ? "Не указано" : userDealInfo.MiddleName;
                existingDealInfo.PassportNumber = string.IsNullOrEmpty(userDealInfo.PassportNumber) ? "Не указано" : userDealInfo.PassportNumber;
                existingDealInfo.PassportIssuedBy = string.IsNullOrEmpty(userDealInfo.PassportIssuedBy) ? "Не указано" : userDealInfo.PassportIssuedBy;
                existingDealInfo.PersonalNumber = string.IsNullOrEmpty(userDealInfo.PersonalNumber) ? "Не указано" : userDealInfo.PersonalNumber;
                existingDealInfo.PhoneNumber = user.PhoneNumber;
                existingDealInfo.PassportIssuedDate = userDealInfo.PassportIssuedDate;
                existingDealInfo.PassportIssuedDate = userDealInfo.PassportIssuedDate;

                _context.UserDealInfos.Update(existingDealInfo);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessageDeal"] = "Личные данные успешно обновлены.";
            }
            catch
            {
                TempData["ErrorMessageDeal"] = "Ошибка при сохранении личных данных.";
            }
            return RedirectToAction("Index");
        }

        // Выход из аккаунта
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
