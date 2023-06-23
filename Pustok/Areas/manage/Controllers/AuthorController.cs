using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Areas.manage.Controllers
{
    [Authorize]
    [Area("manage")]
    public class AuthorController : Controller
    {
        private readonly PustokDbContext _context;

        public AuthorController(PustokDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(int page=1)
        {
            var query = _context.Authors
                .Include(x => x.Books);
               
            return View(PaginatedList<Author>.Create(query,page,3));
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Author author,IFormFile file)
        {
            if (!ModelState.IsValid)
                return View();

            if (_context.Authors.Any(x=>x.FullName == author.FullName))
            {
                ModelState.AddModelError("FullName", "The FullName already taken");
                return View();
            }

            _context.Authors.Add(author);
            _context.SaveChanges();

            return RedirectToAction("index");
        }


        public IActionResult Edit(int id)
        {
            Author author = _context.Authors.Find(id);

            if (author == null)
                return View("Error");

            return View(author);
        }

        [HttpPost]
        public IActionResult Edit(Author author)
        {
            if (!ModelState.IsValid)
                return View();

            Author existAuthor = _context.Authors.Find(author.Id);

            if (existAuthor == null)
                    return View("Error");

            if (author.FullName!=existAuthor.FullName && _context.Authors.Any(x => x.Id != author.Id && x.FullName == author.FullName))
            {
                ModelState.AddModelError("FullName", "The FullName already taken");
                return View();
            }

            existAuthor.FullName = author.FullName;
            existAuthor.BirthYear = author.BirthYear;

            _context.SaveChanges();

            return RedirectToAction("index");
        }

        public IActionResult Delete(int id)
        {
            Author author = _context.Authors.Find(id);

            if (author == null)
                return View("Error");

            return View(author);
        }

        [HttpPost]
        public IActionResult Delete(Author author)
        {
            Author existAuthor = _context.Authors.Find(author.Id);

            if (existAuthor == null)
                return View("Error");

            _context.Authors.Remove(existAuthor);
            _context.SaveChanges();

            return RedirectToAction("index");
        }
    }
}
