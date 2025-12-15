using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmsPilot.Models
{
    public class SmsMessage
    {
        public int Id { get; set; }

        [Required]
        public string Destinataire { get; set; } // Numéro du destinataire

        [Required]
        public string Contenu { get; set; } // Le texte du SMS [cite: 67]

        public DateTime DateCreation { get; set; } = DateTime.Now;

       // Date prévue pour l'envoi (si null ou <= maintenant, envoi immédiat) [cite: 70]
        public DateTime? DateEnvoiPrevue { get; set; }

        public SmsStatus Statut { get; set; } = SmsStatus.EnAttente; // [cite: 71, 78]

        // Si l'API Orange renvoie une réponse ou un ID de suivi, on peut le stocker ici
        public string? ApiMessageId { get; set; }

        // Clé étrangère vers l'Utilisateur (traçabilité) [cite: 45]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}