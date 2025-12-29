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
    [Authorize] // Bon, ici je m'assure que seuls les utilisateurs connectés peuvent accéder
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly OrangeSmsService _smsService;
        private readonly ILogger<MessagesController> _logger;

        // J'injecte mes dépendances : la base de données, le service Orange et le logger pour tracer ce qui se passe
        public MessagesController(AppDbContext context, OrangeSmsService smsService, ILogger<MessagesController> logger)
        {
            _context = context;
            _smsService = smsService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // Ma page d'historique où je liste tous les messages envoyés
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var messages = await _context.SmsMessages
                .Where(m => m.UserId == userId) // Chaque utilisateur ne voit que ses propres messages, c'est important
                .OrderByDescending(m => m.DateCreation) // Je mets les plus récents en premier, c'est plus logique
                .ToListAsync();

            return View(messages);
        }

        // Page de détails pour voir un message spécifique
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int userId = GetCurrentUserId();
            var message = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // Ici j'affiche le formulaire pour créer un nouveau message
        public IActionResult Create()
        {
            int userId = GetCurrentUserId();
            // Je charge tous les contacts de l'utilisateur pour la liste déroulante
            var contacts = _context.Contacts
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    NumeroTelephone = c.NumeroTelephone,
                    NomComplet = c.Nom + " " + (c.Prenom ?? "")
                })
                .ToList();
            ViewBag.Contacts = new SelectList(contacts, "NumeroTelephone", "NomComplet");

            return View();
        }

        // Maintenant je traite l'envoi du SMS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SmsMessage message, string? SelectedContactPhone)
        {
            int userId = GetCurrentUserId();
            message.UserId = userId;
            message.DateCreation = DateTime.Now;

            // IMPORTANT : Je retire "User" de la validation ModelState
            // Sinon ça plante car je gère la relation via UserId directement
            ModelState.Remove("User");

            // Si l'utilisateur a choisi un contact dans la liste, je récupère son numéro
            if (!string.IsNullOrEmpty(SelectedContactPhone))
            {
                message.Destinataire = SelectedContactPhone;
            }

            if (ModelState.IsValid)
            {
                // Je vérifie si c'est un envoi immédiat ou programmé
                if (message.DateEnvoiPrevue == null || message.DateEnvoiPrevue <= DateTime.Now)
                {
                    try
                    {
                        var (success, apiMsgId) = await _smsService.SendSmsAsync(message.Destinataire, message.Contenu);
                        message.Statut = success ? SmsStatus.Envoye : SmsStatus.Echec;
                        message.ApiMessageId = apiMsgId;

                        if (success)
                        {
                            _logger.LogInformation($"SMS envoyé avec succès au {message.Destinataire}. API Message ID: {apiMsgId}");
                            TempData["SuccessMessage"] = "SMS envoyé avec succès !";
                        }
                        else
                        {
                            _logger.LogWarning($"Échec de l'envoi du SMS au {message.Destinataire}");
                            TempData["ErrorMessage"] = "Échec de l'envoi du SMS. Veuillez réessayer.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erreur lors de l'envoi du SMS au {message.Destinataire}");
                        message.Statut = SmsStatus.Echec;
                        TempData["ErrorMessage"] = "Erreur lors de l'envoi du SMS. Veuillez réessayer plus tard.";
                    }
                }
                else
                {
                    message.Statut = SmsStatus.EnAttente;
                    _logger.LogInformation($"SMS programmé pour {message.DateEnvoiPrevue} au {message.Destinataire}");
                    TempData["SuccessMessage"] = $"SMS programmé pour le {message.DateEnvoiPrevue:dd/MM/yyyy HH:mm}";
                }

                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si la validation échoue, je recharge la liste des contacts pour réafficher le formulaire
            var contacts = _context.Contacts
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    NumeroTelephone = c.NumeroTelephone,
                    NomComplet = c.Nom + " " + (c.Prenom ?? "")
                })
                .ToList();
            ViewBag.Contacts = new SelectList(contacts, "NumeroTelephone", "NomComplet");
            return View(message);
        }
    }
}