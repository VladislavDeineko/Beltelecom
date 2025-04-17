namespace Beltelecom.Models.ViewModels
{
    public class OrderFilterViewModel
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public List<Order> Orders { get; set; } = new();
    }

}
