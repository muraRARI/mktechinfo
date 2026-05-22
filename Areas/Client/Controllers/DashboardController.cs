using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Areas.Client.Controllers
{
    //[Area("Client")]
    ////[Authorize]
    //[Route("Client/Dashboard")]
    [Authorize(Roles = "Client")]

    [Area("Client")]
    [Route("Client/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
