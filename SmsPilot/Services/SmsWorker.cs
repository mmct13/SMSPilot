using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Services
{
    // Ce service tourne en permanence en arrière-plan pour envoyer les SMS programmés
    public class SmsWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SmsWorker> _logger;

        public SmsWorker(IServiceProvider serviceProvider, ILogger<SmsWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Le service de planification SMS démarre.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledMessages();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur dans le Worker SMS.");
                }

                // Je fais une pause d'1 minute entre chaque vérification
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessScheduledMessages()
        {
            // Je crée un scope car le Worker est un Singleton, mais le DbContext est Scoped
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var smsService = scope.ServiceProvider.GetRequiredService<OrangeSmsService>();
                // Je cherche tous les messages "En Attente" dont l'heure d'envoi est passée
                var messagesToSend = await context.SmsMessages
                    .Where(m => m.Statut == SmsStatus.EnAttente
                             && m.DateEnvoiPrevue != null
                             && m.DateEnvoiPrevue <= DateTime.Now)
                    .ToListAsync();

                if (messagesToSend.Any())
                {
                    _logger.LogInformation($"{messagesToSend.Count} message(s) planifiés trouvés. Envoi en cours...");

                    foreach (var message in messagesToSend)
                    {
                        // J'envoie le SMS via l'API Orange
                        var (success, apiMsgId) = await smsService.SendSmsAsync(message.Destinataire, message.Contenu);

                        // Je mets à jour le statut du message
                        message.Statut = success ? SmsStatus.Envoye : SmsStatus.Echec;
                        message.ApiMessageId = apiMsgId;

                        // Petit délai de courtoisie pour ne pas surcharger l'API (limite 5 TPS = 200ms, je mets 250ms pour être large)
                        await Task.Delay(250);
                    }

                    // Je sauvegarde tout en base de données
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}