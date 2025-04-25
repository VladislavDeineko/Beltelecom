namespace Beltelecom.Models
{
    public class PopularProduct
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
