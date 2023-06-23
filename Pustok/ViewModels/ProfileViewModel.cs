using Pustok.Models;

namespace Pustok.ViewModels
{
    public class ProfileViewModel
    {
        public UserUpdateViewModel User { get; set; }
        public List<Order> Orders { get; set; }
    }
}
