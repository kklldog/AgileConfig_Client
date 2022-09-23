using AgileConfig.Client.RegisterCenter;
using AgileConfigDiscoveryServiceSample.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AgileConfigDiscoveryServiceSample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDiscoveryService _ds;

        public HomeController(ILogger<HomeController> logger, IDiscoveryService ds )
        {
            _logger = logger;
            _ds = ds;
        }

        public IActionResult Index()
        {
            ViewBag.services = _ds.Services;
            ViewBag.userCenterBaseUrl = _ds.RandomOne("user_center").AsHttpHost();

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