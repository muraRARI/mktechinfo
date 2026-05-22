using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Controllers
{
    public class ContactusController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
