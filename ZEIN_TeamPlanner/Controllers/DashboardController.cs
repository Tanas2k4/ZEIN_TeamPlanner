using Microsoft.AspNetCore.Mvc;

namespace ZEIN_TeamPlanner.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
