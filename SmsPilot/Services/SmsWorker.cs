using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Services
{
    // Ce service tourne en permanence en fond (BackgroundService)
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

                // Pause de 1 minute entre chaque vérification
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessScheduledMessages()
        {
            // On crée un scope car le Worker est un Singleton, mais le DbContext est Scoped
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var smsService = scope.ServiceProvider.GetRequiredService<OrangeSmsService>();
                // 1. Trouver les messages "En Attente" dont l'heure est passée
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
                        // 2. Envoi via l'API InfoBip
                        bool success = await smsService.SendSmsAsync(message.Destinataire, message.Contenu);

                        // 3. Mise à jour du statut
                        message.Statut = success ? SmsStatus.Envoye : SmsStatus.Echec;

                        // Petit délai de courtoisie pour l'API
                        await Task.Delay(200);
                    }

                    // 4. Sauvegarde en BDD
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}