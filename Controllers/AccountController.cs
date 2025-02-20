using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        public IActionResult Register(string username, string password, string email)
        {
            // Создаем пользователя
            _userService.CreateUser(username, password, email);
            return Ok("Пользователь создан");
        }

        public IActionResult Login(string username, string password)
        {
            // Проверяем пароль
            bool isValid = _userService.ValidateUserPassword(username, password);
            if (isValid)
            {
                return Ok("Пароль верный!");
            }
            return Unauthorized("Пароль неверный");
        }
    }
}
