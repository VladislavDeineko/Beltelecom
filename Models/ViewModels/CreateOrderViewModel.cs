namespace Beltelecom.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public int InstallmentMonths { get; set; }
        public string PhoneNumber { get; set; }
        public bool RequiresSetup { get; set; }
    }
}
