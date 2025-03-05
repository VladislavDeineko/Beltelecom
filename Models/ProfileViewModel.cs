namespace Beltelecom.Models
{
    public class ProfileViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNum { get; set; }
        public UserAddress UserAddress { get; set; }
        public UserDealInfo UserDealInfo { get; set; }
    }
}
