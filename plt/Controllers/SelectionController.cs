using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using movieRecom.Models.ViewModel;

namespace movieRecom.Controllers
{
    public class SelectionController : Controller
    {
        // GET: SelectionController/Selection
        public ActionResult Selection()
        {
            var viewModel = new SelectionViewModel();
            return View(viewModel);
        }

        // POST: SelectionController/Selection
        [HttpPost]
        public ActionResult Selection(
            string[]? genres,
            string? mood,
            int? runtime,
            int? year,
            string? rating,
            string? seedMovie)
        {
            // Map Russian genre names to English for database lookup
            var genreMapping = new Dictionary<string, string>
            {
                { "Боевик", "Action" },
                { "Комедия", "Comedy" },
                { "Фантастика", "Sci-Fi" },
                { "Ужасы", "Horror" },
                { "Драма", "Drama" },
                { "Триллер", "Thriller" },
                { "Романтика", "Romance" },
                { "Документальный", "Documentary" },
                { "Анимация", "Animation" }
            };

            // Convert selected genres to English
            var englishGenres = genres?
                .Where(g => genreMapping.ContainsKey(g))
                .Select(g => genreMapping[g])
                .ToArray();

            // Build query parameters for Movies/Index
            var queryParams = new Dictionary<string, string?>();
            
            if (englishGenres != null && englishGenres.Length > 0)
            {
                queryParams["genre"] = englishGenres.First(); // Use first genre for now
            }
            
            if (year.HasValue && year > 1950)
            {
                queryParams["yearFrom"] = year.ToString();
            }

            // Redirect to Movies catalog with filters
            return RedirectToAction("Index", "Movies", queryParams);
        }

        // GET: SelectionController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SelectionController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SelectionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SelectionController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SelectionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SelectionController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SelectionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
