using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public DbSet<MailSent> MailSent { get; set; }
        public DbSet<MailReceived> MailReceived { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Attachments> Attachments { get; set; }
        public DbSet<FavoriteTag> FavoriteTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<MailTag> MailTags { get; set; }
        public DbSet<UserCreatedTag> UserCreatedTag { get; set; }
        public DbSet<FilterFolder> FilterFolders { get; set; }
        public DbSet<FilterFolderTag> FilterFolderTags { get; set; }

        // Fluent API configurations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // AppUser <-> Analytics (One-to-One)
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Analytics)
                .WithOne(a => a.AppUser)
                .HasForeignKey<Analytics>(a => a.AppUserEmail)
                .OnDelete(DeleteBehavior.Cascade);

            // AppUser <-> MailAccount (One-to-Many)
            modelBuilder.Entity<MailAccount>()
                .HasOne(ma => ma.AppUser)
                .WithMany(u => u.MailAccounts)
                .HasForeignKey(ma => ma.AppUserEmail)
                .OnDelete(DeleteBehavior.Cascade);

            // AppUser <-> Contacts (One-to-Many)
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.AppUser)
                .WithMany(u => u.Contacts)
                .HasForeignKey(c => c.AppUserEmail)
                .OnDelete(DeleteBehavior.Cascade);

            // AppUser <-> FavoriteTags (Many-to-Many via FavoriteTags)
            modelBuilder.Entity<FavoriteTag>()
                .HasKey(ft => new { ft.AppUserEmail, ft.TagId });

            modelBuilder.Entity<FavoriteTag>()
                .HasOne(ft => ft.AppUser)
                .WithMany(u => u.FavoriteTags)
                .HasForeignKey(ft => ft.AppUserEmail)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FavoriteTag>()
                .HasOne(ft => ft.Tag)
                .WithMany(t => t.FavoriteTags)
                .HasForeignKey(ft => ft.TagId)
                .OnDelete(DeleteBehavior.NoAction);

            // MailAccount <-> Mail (One-to-Many)
            modelBuilder.Entity<Mail>()
                .HasOne(m => m.MailAccount)
                .WithMany(ma => ma.Mails)
                .HasForeignKey(m => m.MailAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mail <-> Recipients (One-to-Many)
            modelBuilder.Entity<Recipient>()
                .HasOne(r => r.Mail)
                .WithMany(m => m.Recipients)
                .HasForeignKey(r => r.MailId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mail <-> Attachments (One-to-Many)
            modelBuilder.Entity<Attachments>()
                .HasOne(a => a.Mail)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MailId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mail <-> Replies (One-to-Many)
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Mail)
                .WithMany()
                .HasForeignKey(r => r.RepliedToMailId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many: MailReceived <-> Tags (MailTags)
            modelBuilder.Entity<MailTag>()
                .HasKey(mt => new { mt.MailReceivedId, mt.TagId });

            modelBuilder.Entity<MailTag>()
                .HasOne(mt => mt.MailReceived)
                .WithMany(mr => mr.MailTags)
                .HasForeignKey(mt => mt.MailReceivedId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MailTag>()
                .HasOne(mt => mt.Tag)
                .WithMany(t => t.MailTags)
                .HasForeignKey(mt => mt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tag>()
                .HasDiscriminator<string>("TagType")
                .HasValue<Tag>("SystemTag")
                .HasValue<UserCreatedTag>("UserTag");

            // System tags
            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = -1, TagName = "Work" },
                new Tag { Id = -2, TagName = "School" },
                new Tag { Id = -3, TagName = "Social network" },
                new Tag { Id = -4, TagName = "News" },
                new Tag { Id = -5, TagName = "Discounts" },
                new Tag { Id = -6, TagName = "Finance" },
                new Tag { Id = -7, TagName = "Family & friends" },
                new Tag { Id = -8, TagName = "Personal" },
                new Tag { Id = -9, TagName = "Health" }
            );

            modelBuilder.Entity<UserCreatedTag>()
                .HasOne(uct => uct.CreatedByUser)
                .WithMany()
                .HasForeignKey(uct => uct.CreatedByUserEmail)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FilterFolder>(entity =>
            {
                entity.Property(e => e.Keywords)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                     )
                     .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Configure SenderEmails List<string> similarly
                entity.Property(e => e.SenderEmails)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                     )
                     .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Configure relationships for FilterFolder (as added in previous step)
                entity.HasOne(f => f.AppUser)
                      .WithMany(u => u.FilterFolders) // Assumes FilterFolders collection exists on AppUser
                      .HasForeignKey(f => f.AppUserEmail)
                      .IsRequired();
            });


            // --- Configure FilterFolder / Tag Many-to-Many (as added in previous step) ---
            modelBuilder.Entity<FilterFolderTag>(entity =>
            {
                entity.HasKey(fft => new { fft.FilterFolderId, fft.TagId });

                entity.HasOne(fft => fft.FilterFolder)
                      .WithMany(f => f.FilterFolderTags)
                      .HasForeignKey(fft => fft.FilterFolderId);

                entity.HasOne(fft => fft.Tag)
                      .WithMany(t => t.FilterFolderTags)
                      .HasForeignKey(fft => fft.TagId);
            });


            base.OnModelCreating(modelBuilder);
        }

    }
}
