using Microsoft.AspNetCore.Mvc;

[Area("Superadmin")]
public class BlogCategoryController : Controller
{
    public IActionResult Index()
    {
        // list categories
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    

    public IActionResult Edit(int id)
    {
        return View();
    }

    public IActionResult Delete(int id)
    {
        return RedirectToAction("Index");
    }
}