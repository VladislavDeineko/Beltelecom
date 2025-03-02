namespace Beltelecom.Models
{
    public class CatalogViewModel
    {
        public List<Product> Products { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Brands { get; set; }
        public string SelectedCategory { get; set; }
        public string SelectedBrand { get; set; }
        public string SortOrder { get; set; }
    }
}
