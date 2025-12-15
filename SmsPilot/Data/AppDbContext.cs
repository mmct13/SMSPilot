using Microsoft.EntityFrameworkCore;
using SmsPilot.Models;

namespace SmsPilot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<SmsMessage> SmsMessages { get; set; }

        // Configuration spécifique (Fluent API) si besoin
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Exemple : L'email doit être unique dans la base
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}