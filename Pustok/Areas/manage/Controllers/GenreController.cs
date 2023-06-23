using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;

namespace Pustok.Areas.manage.Controllers
{
    [Authorize]
    [Area("manage")]
    public class GenreController : Controller
    {
        private readonly PustokDbContext _context;

        public GenreController(PustokDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            List<Genre> genres = _context.Genres.Include(x=>x.Books).ToList(); 
            return View(genres);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Genre genre)
        {
            if (!ModelState.IsValid) return View();

            if(_context.Genres.Any(x=>x.Name == genre.Name))
            {
                ModelState.AddModelError("Name", "Name already taken");
                return View();
            }

            _context.Genres.Add(genre);
            _context.SaveChanges();

            return RedirectToAction("index");
        }

        public IActionResult Edit(int id)
        {
            Genre genre = _context.Genres.Find(id);

            if (genre == null)
                return View("Error");


            return View(genre);
        }
        [HttpPost]
        public IActionResult Edit(Genre genre)
        {
            if (!ModelState.IsValid) return View();

            Genre existGenre = _context.Genres.Find(genre.Id);

            if (existGenre == null)
                return View("Error");

            if(existGenre.Name!=genre.Name && _context.Genres.Any(x=> x.Name == genre.Name))
            {
                ModelState.AddModelError("Name", "Name already taken");
                return View();
            }

            existGenre.Name = genre.Name;

            _context.SaveChanges();
            return RedirectToAction("index");

        }
        public IActionResult Delete()
        {
            return View();
        }

    }
}
