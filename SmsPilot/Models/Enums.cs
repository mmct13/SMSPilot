namespace SmsPilot.Models
{
    // Rôles définis dans le module de gestion des utilisateurs [cite: 33]
    public enum UserRole
    {
        Admin,
        User
    }

   // Statuts définis dans le module Historique & Suivi 
    public enum SmsStatus
    {
        EnAttente, // Pour les messages planifiés ou en cours de traitement
        Envoye,    // Succès : transmis à l'API Orange
        Echec      // Erreur : numéro invalide, solde insuffisant, etc.
    }
}