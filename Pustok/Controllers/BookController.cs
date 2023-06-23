using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System.Net;
using System.Security.Claims;

namespace Pustok.Controllers
{
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookController(PustokDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Detail(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Book book = _context.Books
                .Include(x => x.BookReviews).ThenInclude(x => x.AppUser)
                .Include(x => x.BookImages)
                .Include(x => x.BookTags).ThenInclude(bt => bt.Tag)
                .Include(x => x.Author)
                .Include(x => x.Genre)
                .FirstOrDefault(x => x.Id == id);


            BookDetailViewModel detailVM = new BookDetailViewModel
            {
                Book = book,
                RelatedBooks = _context.Books
                .Include(x => x.Author)
                .Include(x => x.BookImages)
                .Where(x => x.Id != book.Id && x.GenreId == book.GenreId).Take(6).ToList(),
                BookReview = new BookReview { BookId = book.Id },
                AvgRate = book.BookReviews.Any() ? (int)Math.Ceiling(book.BookReviews.Average(x => x.Rate)) : 0,
                ReviewAlowed = book.BookReviews.Any(x => x.AppUserId == userId) ? false : true
            };

            return View(detailVM);
        }

        public IActionResult GetBookModal(int id)
        {
            var book = _context.Books
                .Include(x => x.Genre)
                .Include(x => x.Author)
                .Include(x => x.BookImages)
                .FirstOrDefault(x => x.Id == id);

            return PartialView("_BookModalPartial", book);
        }

        public async Task<IActionResult> AddToBasket(int id)
        {
            AppUser user = null;

            if (User.Identity.IsAuthenticated)
                user = await _userManager.Users.Include(x => x.BasketItems).FirstOrDefaultAsync(x => x.UserName == User.Identity.Name && !x.IsAdmin);


            if (_context.Books.Find(id) == null)
                return NotFound();

            if (user == null)
            {
                List<BasketCookieItemViewModel> basketItems;
                var basket = HttpContext.Request.Cookies["basket"];

                if (basket == null)
                    basketItems = new List<BasketCookieItemViewModel>();
                else
                    basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

                var wantedBook = basketItems.FirstOrDefault(x => x.BookId == id);

                if (wantedBook == null)
                    basketItems.Add(new BasketCookieItemViewModel { Count = 1, BookId = id });
                else
                    wantedBook.Count++;
                HttpContext.Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketItems));

                BasketViewModel basketVM = new BasketViewModel();
                foreach (var item in basketItems)
                {
                    var book = _context.Books.Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == item.BookId);

                    basketVM.BasketItems.Add(new BasketItemViewModel
                    {
                        Book = book,
                        Count = item.Count
                    });

                    var price = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice;
                    basketVM.TotalPrice += (price * item.Count);
                }

                return PartialView("_BasketCartPartial", basketVM);
            }
            else
            {

                var basketItem = user.BasketItems.FirstOrDefault(x => x.BookId == id);

                if (basketItem != null)
                    basketItem.Count++;
                else
                    user.BasketItems.Add(new BasketItem { BookId = id, AppUserId = user.Id, Count = 1 });

                _context.SaveChanges();

                BasketViewModel basketVM = new BasketViewModel();
                foreach (var item in user.BasketItems)
                {
                    var book = _context.Books.Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == item.BookId);

                    basketVM.BasketItems.Add(new BasketItemViewModel
                    {
                        Book = book,
                        Count = item.Count
                    });

                    var price = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice;
                    basketVM.TotalPrice += (price * item.Count);
                }
                return PartialView("_BasketCartPartial", basketVM);
            }

        }

        public IActionResult RemoveBasket(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(userId!=null && User.IsInRole("Member"))
            {
                BasketItem item = _context.BasketItems.FirstOrDefault(x => x.BookId == id && x.AppUserId == userId);

                if (item == null) return NotFound();

                _context.BasketItems.Remove(item);
                _context.SaveChanges();

                var basketItems = _context.BasketItems.Include(x=>x.Book).Where(x => x.AppUserId == userId).ToList();

                decimal totalPrice = 0;
                foreach (var bi in basketItems)
                {
                    var price = bi.Book.DiscountPercent > 0 ? (bi.Book.SalePrice * (100 - bi.Book.DiscountPercent) / 100) : bi.Book.SalePrice;
                    totalPrice += (price * bi.Count);
                }
                return Ok(new { count = basketItems.Count, totalPrice = totalPrice.ToString("0.00") });
            }
            else
            {
                var basket = Request.Cookies["basket"];

                if (basket == null)
                    return NotFound();

                List<BasketCookieItemViewModel> basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

                BasketCookieItemViewModel item = basketItems.Find(x => x.BookId == id);

                if (item == null)
                    return NotFound();

                basketItems.Remove(item);

                Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketItems));

                decimal totalPrice = 0;
                foreach (var bi in basketItems)
                {
                    var book = _context.Books.Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == bi.BookId);
                    var price = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice;
                    totalPrice += (price * bi.Count);
                }
                return Ok(new { count = basketItems.Count, totalPrice = totalPrice.ToString("0.00") });
            }



        }
        public IActionResult ShowBasket()
        {
            var basket = HttpContext.Request.Cookies["basket"];
            var basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

            return Json(basketItems);
        }

        [HttpPost]
        public async Task<IActionResult> Review(BookReview review)
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var url = Url.Action("detail", "book", new { id = review.BookId });
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("login", "account", new { returnUrl = url });

            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null || user.IsAdmin)
                return RedirectToAction("login", "account", new { returnUrl = url });

            Book book = _context.Books
             .Include(x => x.BookReviews).ThenInclude(x => x.AppUser)
             .Include(x => x.BookImages)
             .Include(x => x.BookTags).ThenInclude(bt => bt.Tag)
             .Include(x => x.Author)
             .Include(x => x.Genre)
             .FirstOrDefault(x => x.Id == review.BookId);

            if (book == null)
                return View("Error");

            BookDetailViewModel detailVM = new BookDetailViewModel
            {
                Book = book,
                RelatedBooks = _context.Books
                .Include(x => x.Author)
                .Include(x => x.BookImages)
                .Where(x => x.Id != book.Id && x.GenreId == book.GenreId).Take(6).ToList(),
                BookReview = new BookReview()
            };

            if (!ModelState.IsValid)
                detailVM.BookReview = review;


            review.CreatedAt = DateTime.UtcNow;
            review.AppUserId = user.Id;
            _context.BookReviews.Add(review);
            _context.SaveChanges();

            return View("Detail", detailVM);
        }

    }
}
