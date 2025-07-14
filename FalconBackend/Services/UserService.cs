using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FalconBackend.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveUserTagsAsync(string userEmail, List<string> tags)
        {
            var user = await _context.AppUsers
                .Include(u => u.FavoriteTags)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                throw new Exception("User not found");

            // Remove existing tags for the user
            _context.FavoriteTags.RemoveRange(user.FavoriteTags);

            // Add new tags
            foreach (var tagName in tags)
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName == tagName);
                if (tag == null)
                {
                    tag = new Tag { TagName = tagName };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                var favoriteTag = new FavoriteTag
                {
                    AppUserEmail = userEmail,
                    TagId = tag.Id
                };

                _context.FavoriteTags.Add(favoriteTag);
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task CreateUserTagAsync(string userEmail, string tagName)
        {
            var existingTag = await _context.Tags
                .Where(t => t.TagName == tagName)
                .OfType<UserCreatedTag>() 
                .FirstOrDefaultAsync();

            if (existingTag != null)
                throw new Exception("Tag already exists");

            var userTag = new UserCreatedTag
            {
                TagName = tagName,
                CreatedByUserEmail = userEmail
            };

            _context.Tags.Add(userTag);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TagDto>> GetAllTagsAsync(string userEmail)
        {
            return await _context.Tags
                .Where(t => !(t is UserCreatedTag) || ((UserCreatedTag)t).CreatedByUserEmail == userEmail)
                .Select(t => new TagDto
                {
                    TagId = t.Id,
                    TagName = t.TagName
                })
                .ToListAsync();
        }


        public struct TagDto
        {
            public int TagId { get; set; }
            public string TagName { get; set; }
        }

        public async Task<List<UserMailAccountDto>> GetUserMailAccountsAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                // Or throw an ArgumentNullException
                return new List<UserMailAccountDto>();
            }

            return await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => new UserMailAccountDto
                {
                    MailAccountId = ma.MailAccountId,
                    EmailAddress = ma.EmailAddress,
                    IsDefault = ma.IsDefault
                })
                .ToListAsync();
        }



        /// <summary>
        /// Fetches real Outlook emails for a user and stores them in the database
        /// Replaces the previous dummy data implementation
        /// </summary>
        public async Task InitializeRealOutlookDataAsync(string userEmail)
        {
            Console.WriteLine($"--- Fetching real Outlook emails for user: {userEmail} ---");

            var mailAccount = await _context.MailAccounts
                                        .FirstOrDefaultAsync(ma => ma.AppUserEmail == userEmail);

            if (mailAccount == null)
            {
                Console.WriteLine($"--- No MailAccount found for {userEmail}. Cannot fetch emails. ---");
                return;
            }

            // Check if we have a valid access token
            if (string.IsNullOrEmpty(mailAccount.Token))
            {
                Console.WriteLine($"--- No access token found for account {mailAccount.MailAccountId}. User needs to authenticate first. ---");
                return;
            }

            try
            {
                // Create OutlookService (you'll need to inject this in the constructor)
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<OutlookService>();
                var outlookService = new OutlookService(configuration, logger);

                // Validate token first
                bool isTokenValid = await outlookService.ValidateAccessTokenAsync(mailAccount.Token);
                if (!isTokenValid)
                {
                    Console.WriteLine($"--- Access token for account {mailAccount.MailAccountId} is invalid or expired. User needs to re-authenticate. ---");
                    return;
                }

                // Check if emails already exist for this account (avoid duplicates)
                bool hasExistingEmails = await _context.MailReceived
                                                  .AnyAsync(mr => mr.MailAccountId == mailAccount.MailAccountId);
                if (hasExistingEmails)
                {
                    Console.WriteLine($"--- Emails already exist for account {mailAccount.MailAccountId}. Skipping to avoid duplicates. ---");
                    return;
                }

                // Fetch emails from Outlook
                var outlookEmails = await outlookService.GetUserEmailsAsync(mailAccount.Token, mailAccount.MailAccountId, 50);

                if (!outlookEmails.Any())
                {
                    Console.WriteLine($"--- No emails found in Outlook for account {mailAccount.MailAccountId} ---");
                    return;
                }

                // --- Fetch available system tags for potential auto-tagging ---
                var availableTags = await _context.Tags
                                                .Where(t => !(t is UserCreatedTag))
                                                .ToListAsync();

                // Process each email and optionally assign tags based on content analysis
                foreach (var email in outlookEmails)
                {
                    // Auto-assign tags based on simple keyword matching (optional)
                    if (availableTags.Any())
                    {
                        var emailTags = AutoAssignTags(email, availableTags);
                        email.MailTags = emailTags;
                    }
                }

                // Add emails to the database
                _context.MailReceived.AddRange(outlookEmails);
                await _context.SaveChangesAsync();

                Console.WriteLine($"--- Successfully fetched and saved {outlookEmails.Count} real Outlook emails for account {mailAccount.MailAccountId} ---");

                // Update last sync time
                mailAccount.LastMailSync = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- FAILED to fetch Outlook emails for account {mailAccount.MailAccountId}: {ex.Message} ---");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"--- Inner Exception: {ex.InnerException.Message} ---");
                }
                throw;
            }
        }

        /// <summary>
        /// Auto-assigns tags to emails based on simple keyword matching
        /// This is a basic implementation - you can enhance it with AI/ML for better accuracy
        /// </summary>
        private List<MailTag> AutoAssignTags(MailReceived email, List<Tag> availableTags)
        {
            var mailTags = new List<MailTag>();
            var emailContent = $"{email.Subject} {email.Body}".ToLowerInvariant();

            // Simple keyword matching for demo purposes
            var tagKeywords = new Dictionary<string, List<string>>
            {
                { "work", new List<string> { "meeting", "project", "deadline", "work", "business", "office" } },
                { "personal", new List<string> { "family", "friend", "personal", "vacation", "birthday" } },
                { "important", new List<string> { "urgent", "important", "asap", "priority", "critical" } },
                { "finance", new List<string> { "payment", "invoice", "money", "bank", "finance", "budget" } },
                { "travel", new List<string> { "flight", "hotel", "travel", "trip", "booking", "reservation" } }
            };

            foreach (var tag in availableTags.Take(3)) // Limit to 3 tags per email
            {
                var tagName = tag.TagName.ToLowerInvariant();
                
                // Check if tag name matches any keyword category
                foreach (var category in tagKeywords)
                {
                    if (category.Key == tagName || category.Value.Any(keyword => emailContent.Contains(keyword)))
                    {
                        mailTags.Add(new MailTag
                        {
                            Tag = tag,
                            MailReceived = email
                        });
                        break; // Avoid duplicate tags
                    }
                }

                if (mailTags.Count >= 3) break; // Limit tags per email
            }

            return mailTags;
        }
    }

}
