using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Areas.Superadmin.Controllers
{
    //[Area("Admin")]
    //[Authorize(Roles = "SuperAdmin")]
    [Area("Superadmin")]
    public class UserController : Controller
    {
     

        public IActionResult Index()
        {
            return View();
        }
    }
}
