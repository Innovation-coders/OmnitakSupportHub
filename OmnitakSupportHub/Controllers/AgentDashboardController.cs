using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Agent")]
    public class AgentDashboardController : Controller
    {
        public IActionResult Index()
        {
            // Support Agent dashboard - focused on ticket management
            return View();
        }
    }
}