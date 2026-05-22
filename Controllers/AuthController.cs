using Microsoft.AspNetCore.Mvc;

namespace mkinfotech.Controllers
{
    public class AuthController : Controller
    {

        // LOGIN PAGE
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // REGISTER PAGE
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // FORGOT PASSWORD PAGE
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // RESET PASSWORD PAGE
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }
       


    }
}
