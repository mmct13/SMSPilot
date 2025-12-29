using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;
using SmsPilot.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace SmsPilot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly OrangeSmsService _smsService;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, OrangeSmsService smsService)
        {
            _logger = logger;
            _context = context;
            _smsService = smsService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        public async Task<IActionResult> Index()
        {
            // Je récupère l'ID de l'utilisateur connecté
            int userId = GetCurrentUserId();

            // Je compte combien de contacts l'utilisateur a
            int contactCount = 0;
            if (userId != 0)
            {
                contactCount = await _context.Contacts.CountAsync(c => c.UserId == userId);
            }

            // Je récupère le solde SMS depuis l'API Orange
            int smsBalance = await _smsService.GetSmsBalanceAsync();

            // Je récupère les 5 derniers messages pour l'historique récent
            var recentMessages = new List<SmsMessage>();
            if (userId != 0)
            {
                recentMessages = await _context.SmsMessages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.DateCreation)
                    .Take(5)
                    .ToListAsync();
            }

            // J'envoie toutes ces données à la vue pour l'affichage
            ViewBag.ContactCount = contactCount;
            ViewBag.SmsBalance = smsBalance;
            ViewBag.RecentMessages = recentMessages;

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
