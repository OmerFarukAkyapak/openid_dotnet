using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenIDServer.Models;
using OpenIDServer.ViewModels;

namespace OpenIDServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewData["UserName"] = User.FindFirst(ClaimTypes.Name)?.Value;
                ViewData["UserEmail"] = User.FindFirst(ClaimTypes.Email)?.Value;
                ViewData["ClientId"] = TempData["ClientId"]?.ToString();
                ViewData["RedirectUri"] = TempData["RedirectUri"]?.ToString();
                ViewData["Scope"] = TempData["Scope"]?.ToString();
                ViewData["State"] = TempData["State"]?.ToString();
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
