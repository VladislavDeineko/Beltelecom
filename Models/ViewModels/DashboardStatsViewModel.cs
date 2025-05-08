namespace Beltelecom.Models.ViewModels
{
    public class DashboardStatsViewModel
    {
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int NewOrdersThisMonth { get; set; }
        public decimal AverageOrderAmount { get; set; } // Средняя сумма заказа
        // Для графика
        public List<string> Months { get; set; } 
        public List<int> OrdersByMonth { get; set; }  // Количество заказов по месяцам
    }
}
