namespace SmsPilot.Models
{
    // Les rôles que j'ai définis pour gérer les utilisateurs
    public enum UserRole
    {
        Admin,
        User
    }

    // Les différents statuts possibles pour mes messages SMS 
    public enum SmsStatus
    {
        EnAttente, // Pour les messages que j'ai programmés ou qui sont en cours de traitement
        Envoye,    // Succès : le message a été transmis à l'API Orange
        Echec      // Erreur : numéro invalide, solde insuffisant, etc.
    }
}