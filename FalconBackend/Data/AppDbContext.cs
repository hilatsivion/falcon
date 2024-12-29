using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace FalconBackend.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor for Dependency Injection
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext() { }

        // Define DbSets for your entities
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Analytics> Analytics { get; set; }
        public DbSet<MailAccount> MailAccounts { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Mail> Mails { get; set; }
        public DbSet<Draft> Drafts { get; set; }
        public DbSet<MailSent> MailSents { get; set; }
        public DbSet<MailReceived> MailReceiveds { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Attachments> Attachments { get; set; }
        public DbSet<FavoriteTag> FavoriteTags { get; set; }
        public DbSet<Tag> Tags { get; set; }

        // Fluent API configurations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Example configuration for relationships (you can customize this):
            // Uncomment and customize as needed.

            // Configure a one-to-many relationship
            // modelBuilder.Entity<Contact>()
            //     .HasMany(c => c.Mails)
            //     .WithOne(m => m.Contact)
            //     .HasForeignKey(m => m.ContactId);

            // Additional configurations
            base.OnModelCreating(modelBuilder);
        }
    }
}
