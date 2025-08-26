using Microsoft.AspNetCore.Mvc;

namespace OmnitakSupportHub.Controllers
{
    public class SupportTeamController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
