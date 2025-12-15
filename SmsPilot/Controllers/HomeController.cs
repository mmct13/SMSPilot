using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmsPilot.Controllers
{
    [Authorize] // <--- INDISPENSABLE : Protège tout le contrôleur
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Page d'erreur par défaut
        public IActionResult Error()
        {
            return View();
        }
    }
}