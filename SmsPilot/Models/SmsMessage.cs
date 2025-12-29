using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmsPilot.Models
{
    public class SmsMessage
    {
        public int Id { get; set; }

        [Required]
        public string Destinataire { get; set; } // Le numéro du destinataire

        [Required]
        public string Contenu { get; set; } // Le texte du SMS que je veux envoyer

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // La date prévue pour l'envoi (si null ou <= maintenant, j'envoie immédiatement)
        public DateTime? DateEnvoiPrevue { get; set; }

        public SmsStatus Statut { get; set; } = SmsStatus.EnAttente; // Le statut du message

        // Si l'API Orange me renvoie un ID de suivi, je le stocke ici
        public string? ApiMessageId { get; set; }

        // Clé étrangère : pour savoir qui a envoyé ce message
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}