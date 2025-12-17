using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;
using SmsPilot.Services;
using System.Security.Claims;

namespace SmsPilot.Controllers
{
    [Authorize] // Sécurité : Il faut être connecté
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly OrangeSmsService _smsService;

        // On injecte le contexte BDD et le Service Infobip qu'on vient de créer
        public MessagesController(AppDbContext context, OrangeSmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // 1. PAGE HISTORIQUE (Liste des messages)
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var messages = await _context.SmsMessages
                .Where(m => m.UserId == userId) // On ne voit que ses propres messages
                .OrderByDescending(m => m.DateCreation) // Les plus récents en haut
                .ToListAsync();

            return View(messages);
        }

        // 2. PAGE DE CRÉATION (Formulaire)
        public IActionResult Create()
        {
            int userId = GetCurrentUserId();
            // On charge les contacts pour la liste déroulante
            var contacts = _context.Contacts.Where(c => c.UserId == userId).ToList();
            ViewBag.Contacts = new SelectList(contacts, "NumeroTelephone", "Nom");

            return View();
        }

        // 3. TRAITEMENT DE L'ENVOI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SmsMessage message, string? SelectedContactPhone)
        {
            int userId = GetCurrentUserId();
            message.UserId = userId;
            message.DateCreation = DateTime.Now;

            // --- CORRECTION MAJEURE ICI ---
            // On retire "User" de la validation car on gère la relation via UserId
            ModelState.Remove("User");
            // -----------------------------

            // Gestion du numéro via la liste déroulante
            if (!string.IsNullOrEmpty(SelectedContactPhone))
            {
                message.Destinataire = SelectedContactPhone;
            }

            if (ModelState.IsValid)
            {
                // ... (Le reste de ton code d'envoi reste identique) ...

                // Exemple rappel :
                if (message.DateEnvoiPrevue == null || message.DateEnvoiPrevue <= DateTime.Now)
                {
                    bool success = await _smsService.SendSmsAsync(message.Destinataire, message.Contenu);
                    message.Statut = success ? SmsStatus.Envoye : SmsStatus.Echec;
                }
                else
                {
                    message.Statut = SmsStatus.EnAttente;
                }

                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si ça échoue, on recharge la liste
            var contacts = _context.Contacts.Where(c => c.UserId == userId).ToList();
            ViewBag.Contacts = new SelectList(contacts, "NumeroTelephone", "Nom");
            return View(message);
        }
    }
}