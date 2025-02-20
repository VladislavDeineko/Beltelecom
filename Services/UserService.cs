using Beltelecom.Data;
using Microsoft.AspNetCore.Identity;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();  // Инициализация хешера
    }

    public void CreateUser(string username, string password, string email)
    {
        // Создаем пользователя
        var user = new User
        {
            Username = username,
            Email = email,

            
        };

        // Хешируем пароль
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        // Сохраняем пользователя в базу данных
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public bool ValidateUserPassword(string username, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return false;

        // Проверка введенного пароля с хешем
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success;
    }
}
