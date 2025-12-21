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
            // 1. Récupérer l'ID utilisateur
            int userId = GetCurrentUserId();

            // 2. Compter les contacts
            int contactCount = 0;
            if (userId != 0)
            {
                contactCount = await _context.Contacts.CountAsync(c => c.UserId == userId);
            }

            // 3. Récupérer le solde SMS
            int smsBalance = await _smsService.GetSmsBalanceAsync();

            // 4. Récupérer les 5 derniers messages
            var recentMessages = new List<SmsMessage>();
            if (userId != 0)
            {
                recentMessages = await _context.SmsMessages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.DateCreation)
                    .Take(5)
                    .ToListAsync();
            }

            // 5. Passer à la vue
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
