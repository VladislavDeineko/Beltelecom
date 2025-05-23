using Beltelecom.Data;
using Beltelecom.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Beltelecom.Controllers
{
    public class ServicePackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ServicePackagesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
    }
}
