using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System.Security.Claims;

namespace Pustok.Controllers
{
    public class OrderController : Controller
    {
		private readonly PustokDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public OrderController(PustokDbContext context, UserManager<AppUser> userManager)
        {
			_context = context;
            _userManager = userManager;
        }
        public IActionResult Checkout()
        {
            return View(GetCheckoutVM());
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            if (!ModelState.IsValid)
            {
                var vm = GetCheckoutVM();
                vm.Order = order;
                return View(vm);
            }

            if (User.Identity.IsAuthenticated && User.IsInRole("Member"))
			{
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                AppUser user = _userManager.FindByNameAsync(User.Identity.Name).Result;

                var basketDbItems = _context.BasketItems.Include(x => x.Book).Where(x => x.AppUserId == userId).ToList();

                var orderItems = basketDbItems.Select(bi => new OrderItem { BookId = bi.BookId, Count = bi.Count, DiscountPercent = bi.Book.DiscountPercent, SalePrice = bi.Book.SalePrice, CostPrice = bi.Book.CostPrice });
                order.OrderItems.AddRange(orderItems);

                order.FullName = user.FullName;
                order.Email = user.Email;
                order.AppUserId = userId;

                _context.BasketItems.RemoveRange(basketDbItems);
            }
            else
			{
                var basket = HttpContext.Request.Cookies["basket"];

                if (basket == null) return View("Error");

                List<BasketCookieItemViewModel> basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

                foreach (var bi in basketItems)
                {
                    Book book = _context.Books.Find(bi.BookId);
                    if (book == null) return View("Error");

                    order.OrderItems.Add(new OrderItem { BookId = bi.BookId, Count = bi.Count, DiscountPercent = book.DiscountPercent, SalePrice = book.SalePrice, CostPrice = book.CostPrice });
                }

                Response.Cookies.Delete("basket");
            }


            //create order 

            order.CreatedAt = DateTime.UtcNow;
            order.Status = Enums.OrderStatus.Pending;

            _context.Orders.Add(order);
            _context.SaveChanges();

            //if (User.Identity.IsAuthenticated && User.IsInRole("Member"))
            //{
            //    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //    _context.BasketItems.RemoveRange(_context.BasketItems.Where(x => x.AppUserId == userId).ToList());
            //    _context.SaveChanges();
            //}
            //else
            //    Response.Cookies.Delete("basket");


            return RedirectToAction("index", "home");
        }

        private List<CheckoutBookItemViewModel> GetBasketItems()
        {
			List<CheckoutBookItemViewModel> basketItems = new List<CheckoutBookItemViewModel>();

			if (User.Identity.IsAuthenticated && User.IsInRole("Member"))
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var basketDbItems = _context.BasketItems.Include(x=>x.Book).Where(x => x.AppUserId == userId).ToList();

				foreach (var item in basketDbItems)
				{
					CheckoutBookItemViewModel bi = new CheckoutBookItemViewModel
					{
						Name = item.Book.Name,
						Count = item.Count,
						Price = item.Book.DiscountPercent == 0 ? item.Book.SalePrice : (item.Book.SalePrice * (100 - item.Book.DiscountPercent) / 100)
					};

					basketItems.Add(bi);
				}
			}
            else
            {
				List<BasketCookieItemViewModel> basketCookieItems;

				var basket = HttpContext.Request.Cookies["basket"];

				if (basket == null)
					basketCookieItems = new List<BasketCookieItemViewModel>();
				else
					basketCookieItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

				foreach (var item in basketCookieItems)
				{
					Book book = _context.Books.Find(item.BookId);
					CheckoutBookItemViewModel bi = new CheckoutBookItemViewModel
					{
						Name = book.Name,
						Count = item.Count,
						Price = book.DiscountPercent == 0 ? book.SalePrice : (book.SalePrice * (100 - book.DiscountPercent) / 100)
					};

					basketItems.Add(bi);
				}
			}
			

            return basketItems;
		}

        private CheckoutViewModel GetCheckoutVM()
        {

            AppUser user = null;
            if (User.Identity.IsAuthenticated && User.IsInRole("Member"))
                user = _userManager.FindByNameAsync(User.Identity.Name).Result;

            CheckoutViewModel vm = new CheckoutViewModel
            {
                BasketItems = GetBasketItems(),
                Order = new Order { FullName = user?.FullName,Email = user?.Email },
            };

            vm.TotalPrice = vm.BasketItems.Sum(x => x.Count * x.Price);

            return vm;
        }

        [Authorize(Roles ="Member")]
        public IActionResult Detail(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Order order = _context.Orders
                .Include(x=>x.OrderItems)
                .ThenInclude(x=>x.Book)
                .FirstOrDefault(x => x.Id == id && x.AppUserId==userId);

            if(order==null) return View("Error");

            return View(order);
        }

	}
}
