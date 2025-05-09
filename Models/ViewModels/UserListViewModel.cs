namespace Beltelecom.Models.ViewModels
{
    public class UserListViewModel
    {
        public string? UserId { get; set; }
        public string FullName { get; set; } // ФИО
        public string Email { get; set; } // Электронная почта
        public string? PhoneNumber { get; set; } // Телефон (может быть null)
        public string? Address { get; set; } // Адрес (может быть null)
        public int OrdersCount { get; set; } // Общее количество заказов
        public string Role { get; set; }
    }
}
