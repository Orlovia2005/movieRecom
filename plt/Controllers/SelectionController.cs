using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using movieRecom.Models.ViewModel;

namespace movieRecom.Controllers
{
    public class SelectionController : Controller
    {
        // GET: SelectionController
        // Добавьте этот новый метод
        public ActionResult Selection()
        {
            var viewModel = new SelectionViewModel();
            return View(viewModel);
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
