using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;


namespace FalconBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext() { }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Fluent API configurations for relationships can be added here if necessary.
        }
    }
}
