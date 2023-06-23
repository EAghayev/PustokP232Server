using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Controllers
{
    public class ShopController : Controller
    {
		private readonly PustokDbContext _context;

		public ShopController(PustokDbContext context)
        {
			_context = context;
		}
        public IActionResult Index(int page=1,int size=8,decimal? minPrice=null,decimal? maxPrice=null, int?genreId=null,List<int> tagIds=null)
        {
            ViewBag.Genres = _context.Genres.Include(x=>x.Books).ToList();
			ViewBag.Authors = _context.Authors.ToList();
			ViewBag.Tags = _context.Tags.ToList();

            ViewBag.GenreId = genreId;
            ViewBag.TagIds = tagIds;
          


			var query = _context.Books
                .Include(x => x.Author)
                .Include(x=>x.BookTags)
                .Include(x => x.BookImages.Where(x => x.PosterStatus != null)).Where(x=>!x.IsDeleted);

			ViewBag.MinRange = query.Min(x => x.SalePrice);
			ViewBag.MaxRange = query.Max(x => x.SalePrice);

			if (genreId != null)
                query = query.Where(x => x.GenreId == genreId);

            if (tagIds != null && tagIds.Count>0)
                query = query.Where(x => x.BookTags.Any(x => tagIds.Contains(x.TagId)));

            if (minPrice != null && maxPrice != null)
                query = query.Where(x => x.SalePrice >= minPrice && x.SalePrice <= maxPrice);

           
			ViewBag.MinPrice = minPrice?? ViewBag.MinRange;
			ViewBag.MaxPrice = maxPrice?? ViewBag.MaxRange;


			PaginatedList<Book> data = PaginatedList<Book>.Create(query, page, size);
			return View(data);
        }
    }
}
