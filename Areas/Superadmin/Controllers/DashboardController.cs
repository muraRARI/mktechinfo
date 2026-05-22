using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Areas.Superadmin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]

    [Area("Superadmin")]

    [Route("Superadmin/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
