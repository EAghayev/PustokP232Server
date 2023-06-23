using Pustok.Models;

namespace Pustok.ViewModels
{
	public class CheckoutViewModel
	{
		public List<CheckoutBookItemViewModel> BasketItems { get; set; }
		public Order Order { get; set; }
		public decimal TotalPrice { get; set; }
	}
}
