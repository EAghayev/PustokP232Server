using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Areas.manage.Controllers
{
	[Area("manage")]
	public class OrderController : Controller
	{
        private readonly PustokDbContext _context;
        private readonly IHubContext<PustokHub> _hubContext;

        public OrderController(PustokDbContext context, IHubContext<PustokHub> hubContext)
		{
            _context = context;
            _hubContext = hubContext;
        }
		public IActionResult Index(int page=1,int size=4)
		{
			var query = _context.Orders.Include(x => x.OrderItems);

			var data = PaginatedList<Order>.Create(query, page, size);
			return View(data);
		}

		public IActionResult Edit(int id)
		{
			Order order = _context.Orders
				.Include(x => x.OrderItems).ThenInclude(x => x.Book)
				.FirstOrDefault(x => x.Id == id && x.Status == Enums.OrderStatus.Pending);

			if (order == null) return View("Error");

			return View(order);
		}

		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
		{
			Order order = _context.Orders.FirstOrDefault(x => x.Id == id && x.Status == Enums.OrderStatus.Pending);
            if (order == null) return View("Error");

			order.Status = Enums.OrderStatus.Accepted;
			_context.SaveChanges();

            AppUser user = _context.AppUsers.FirstOrDefault(x => x.Id == order.AppUserId);

            if (user.IsOnline)
            {
                await _hubContext.Clients.Client(user.ConnectionId).SendAsync("OrderAccepted");
            }



			return RedirectToAction("index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            Order order = _context.Orders.FirstOrDefault(x => x.Id == id && x.Status == Enums.OrderStatus.Pending);
            if (order == null) return View("Error");

            order.Status = Enums.OrderStatus.Rejected;
            _context.SaveChanges();

            return RedirectToAction("index");
        }

		public IActionResult Detail(int id)
		{
            Order order = _context.Orders
                .Include(x => x.OrderItems).ThenInclude(x => x.Book)
                .FirstOrDefault(x => x.Id == id);

            if (order == null) return View("Error");

            return View(order);
        }
    }
}
