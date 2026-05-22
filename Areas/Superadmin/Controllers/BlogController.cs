using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Areas.Superadmin.Controllers
{
    [Area("Superadmin")]
    public class BlogController : Controller
    {
        // ================= LIST =================


        public IActionResult Index()
        {

            return View();
        }
        public IActionResult List()
        {
            // TODO: _blogService.GetAll()
            return View();
        }

        public IActionResult Edit()
        {
            // TODO: _blogService.GetAll()
            return View();
        }
        // ================= DETAILS =================
        public IActionResult Details(int id)
        {
            // TODO: _blogService.GetById(id)
            return View();
        }

        // ================= CREATE (GET) =================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string title, string content)
        {
            if (string.IsNullOrEmpty(title))
                return View();

            // TODO: _blogService.Create()

            return RedirectToAction("Index");
        }

        // ================= EDIT (GET) =================
        //[HttpGet]
        //public IActionResult Edit(int id)
        //{
        //    // TODO: get blog by id
        //    return View();
        //}


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            using var client = new HttpClient();

            var response = await client.GetAsync(
                $"https://localhost:44394/api/BlogApi/edit/{id}"
            );

            var json = await response.Content.ReadAsStringAsync();

            ViewBag.BlogJson = json;

            return View();
        }
        // ================= EDIT (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string title, string content)
        {
            // TODO: _blogService.Update()

            return RedirectToAction("Index");
        }

        // ================= DELETE (GET CONFIRM) =================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            // show confirmation page
            return View();
        }

        // ================= DELETE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // TODO: _blogService.Delete(id)

            return RedirectToAction("Index");
        }

       
        // ================= TOGGLE PUBLISH =================
        public IActionResult TogglePublish(int id)
        {
            // TODO: _blogService.TogglePublish(id)

            return RedirectToAction("Index");
        }
    }
}