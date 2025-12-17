# SMSPilot

SMSPilot est une application web centralisée dédiée à l'expédition de SMS unitaires ou en masse via l'API Orange.  
Conçue pour structurer la communication des organisations, elle intègre la gestion des utilisateurs, des carnets de contacts et la planification de campagnes.

## Fonctionnalités Clés

### Authentification & Sécurité

- Système de connexion sécurisé (Email / Mot de passe)
- Gestion des rôles (RBAC) : Administrateur et Utilisateur Standard
- Cloisonnement des données : chaque utilisateur ne voit que ses propres contacts et messages

### Gestion des Contacts

- Ajout, modification et suppression de contacts
- Support du format international (+225...)
- Organisation par groupes (ex : VIP, Prospections)

### Envoi de SMS (API Orange)

- Envoi immédiat : expédition instantanée via l'API Orange
- Envoi planifié : programmation d'un message pour une date future

### Historique & Suivi

- Tableau de bord synthétique
- Journal détaillé des envois avec statuts en temps réel :
  - 🟢 Succès (Envoyé)
  - 🔴 Échec (Erreur API ou numéro)
  - 🟡 En attente (Planifié)

### Administration

- Interface réservée aux Administrateurs
- Création et suppression de comptes utilisateurs

## Stack Technique

- Framework : ASP.NET Core 8.0 (MVC)
- Langage : C#
- Base de données : SQL Server (LocalDB) via Entity Framework Core (Code First)
- Frontend : Razor Views, Bootstrap 5, Bootstrap Icons
- Services externes : Orange SMS API

## Installation et Démarrage

### Prérequis

- Visual Studio 2022
- .NET 8.0 SDK

### 1. Cloner le projet

```bash
git clone https://github.com/mmct13/SmsPilot.git
```

### 2. Configuration (appsettings.json)

Créez un fichier appsettings.json à la racine du projet SmsPilot avec ce contenu :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SmsPilotDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "OrangeApi": {
    "ClientId": "VOTRE_CLIENT_ID_ORANGE",
    "ClientSecret": "VOTRE_CLIENT_SECRET_ORANGE",
    "BaseUrl": "https://api.orange.com"
  }
}
```

### 3. Base de données

```bash
Update Database
```

### 4. Premier démarrage

La base étant vide au départ, injectez manuellement le premier administrateur via SQL Server Object Explorer :

- Table : Users
  - Données :
    - Nom : Admin
    - Email : admin@smspilot.ci
    - Role : 0 (Admin)
