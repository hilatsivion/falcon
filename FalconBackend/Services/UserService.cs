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
        private readonly OutlookService _outlookService;
        private readonly AiTaggingService _aiTaggingService;

        public UserService(AppDbContext context, OutlookService outlookService, AiTaggingService aiTaggingService)
        {
            _context = context;
            _outlookService = outlookService;
            _aiTaggingService = aiTaggingService;
        }

        /// <summary>
        /// Create a new mail account with OAuth tokens and automatically sync emails
        /// This method handles the complete flow after OAuth authentication
        /// </summary>
        public async Task<MailAccount> CreateMailAccountAsync(MailAccountCreateRequest request, string userEmail)
        {
            try
            {
                Console.WriteLine($"--- Creating mail account for {request.EmailAddress} ---");

                // Check if user exists
                var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    throw new InvalidOperationException($"User {userEmail} not found");
                }

                // Check if mail account already exists
                var existingAccount = await _context.MailAccounts
                    .FirstOrDefaultAsync(ma => ma.EmailAddress == request.EmailAddress && ma.AppUserEmail == userEmail);
                
                if (existingAccount != null)
                {
                    throw new InvalidOperationException($"Mail account {request.EmailAddress} already exists for user {userEmail}");
                }

                // Calculate token expiration
                var tokenExpiresAt = request.ExpiresIn.HasValue 
                    ? DateTime.UtcNow.AddSeconds(request.ExpiresIn.Value)
                    : DateTime.UtcNow.AddHours(1); // Default 1 hour

                // Create new mail account with OAuth tokens
                var mailAccount = new MailAccount
                {
                    EmailAddress = request.EmailAddress,
                    AccessToken = request.AccessToken,
                    RefreshToken = request.RefreshToken,
                    TokenExpiresAt = tokenExpiresAt,
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(14), // Refresh tokens typically last 14+ days
                    Provider = request.Provider,
                    IsDefault = request.IsDefault,
                    AppUserEmail = userEmail,
                    LastMailSync = DateTime.UtcNow,
                    IsTokenValid = true
                };

                // Add to database
                _context.MailAccounts.Add(mailAccount);
                await _context.SaveChangesAsync();

                Console.WriteLine($"--- Mail account {mailAccount.MailAccountId} created successfully ---");

                // Automatically sync emails if requested
                if (request.SyncMailsImmediately)
                {
                    Console.WriteLine($"--- Starting automatic email sync for {mailAccount.EmailAddress} ---");
                    await SyncMailsForAccountAsync(mailAccount);
                }

                return mailAccount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- FAILED to create mail account for {request.EmailAddress}: {ex.Message} ---");
                throw;
            }
        }

        /// <summary>
        /// Sync emails for a specific mail account with automatic token refresh
        /// Includes placeholder for AI-powered tagging
        /// </summary>
        public async Task SyncMailsForAccountAsync(MailAccount mailAccount)
        {
            try
            {
                Console.WriteLine($"--- Syncing emails for account {mailAccount.EmailAddress} ---");

                // Get a valid access token (automatically refreshes if needed)
                var validAccessToken = await _outlookService.GetValidAccessTokenAsync(mailAccount, async (updatedAccount) =>
                {
                    _context.MailAccounts.Update(updatedAccount);
                    await _context.SaveChangesAsync();
                });

                if (string.IsNullOrEmpty(validAccessToken))
                {
                    Console.WriteLine($"--- Failed to get valid access token for account {mailAccount.EmailAddress}. Token refresh needed. ---");
                    mailAccount.IsTokenValid = false;
                    await _context.SaveChangesAsync();
                    return;
                }

                // Get the last sync time to only fetch new emails
                var lastSyncTime = mailAccount.LastMailSync;
                Console.WriteLine($"--- Last sync was at: {lastSyncTime} ---");

                // Fetch emails from Outlook
                var outlookEmails = await _outlookService.GetUserEmailsAsync(validAccessToken, mailAccount.MailAccountId, 100);

                if (!outlookEmails.Any())
                {
                    Console.WriteLine($"--- No new emails found for account {mailAccount.EmailAddress} ---");
                    return;
                }

                // Filter out emails that are already in the database (avoid duplicates)
                var existingEmailIds = await _context.MailReceived
                    .Where(mr => mr.MailAccountId == mailAccount.MailAccountId)
                    .Select(mr => mr.Subject + "|" + mr.TimeReceived.ToString("yyyy-MM-dd HH:mm:ss") + "|" + mr.Sender)
                    .ToListAsync();

                var newEmails = outlookEmails.Where(email => 
                    !existingEmailIds.Contains(email.Subject + "|" + email.TimeReceived.ToString("yyyy-MM-dd HH:mm:ss") + "|" + email.Sender))
                    .ToList();

                if (!newEmails.Any())
                {
                    Console.WriteLine($"--- No new emails to sync for account {mailAccount.EmailAddress} ---");
                    mailAccount.LastMailSync = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return;
                }

                // --- Fetch available system tags for potential auto-tagging ---
                var availableTags = await _context.Tags
                    .Where(t => !(t is UserCreatedTag))
                    .ToListAsync();

                // Process each new email and assign tags
                foreach (var email in newEmails)
                {
                    // Auto-assign tags based on simple keyword matching
                    if (availableTags.Any())
                    {
                        var emailTags = AutoAssignTags(email, availableTags);
                        email.MailTags = emailTags;
                    }
                }

                // Apply AI-powered tagging to all new emails in batch
                if (newEmails.Any())
                {
                    try
                    {
                        Console.WriteLine($"--- Applying AI tagging to {newEmails.Count} new emails ---");
                        var aiTags = await _aiTaggingService.GetAiTagsAsync(newEmails);
                        
                        // Apply the AI tags to the emails
                        foreach (var aiTag in aiTags)
                        {
                            var email = newEmails.FirstOrDefault(e => e.MailId == aiTag.MailReceivedId);
                            if (email != null)
                            {
                                email.MailTags.Add(aiTag);
                            }
                        }
                        
                        Console.WriteLine($"--- Successfully applied {aiTags.Count} AI-generated tags ---");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"--- AI tagging failed, continuing without AI tags: {ex.Message} ---");
                        // Continue without AI tags if the service fails
                    }
                }

                // Add new emails to the database
                _context.MailReceived.AddRange(newEmails);
                await _context.SaveChangesAsync();

                Console.WriteLine($"--- Successfully synced {newEmails.Count} new emails for account {mailAccount.EmailAddress} ---");

                // Update last sync time
                mailAccount.LastMailSync = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- FAILED to sync emails for account {mailAccount.EmailAddress}: {ex.Message} ---");
                throw;
            }
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
            if (string.IsNullOrEmpty(mailAccount.AccessToken))
            {
                Console.WriteLine($"--- No access token found for account {mailAccount.MailAccountId}. User needs to authenticate first. ---");
                return;
            }

            try
            {
                // Get a valid access token (automatically refreshes if needed)
                var validAccessToken = await _outlookService.GetValidAccessTokenAsync(mailAccount, async (updatedAccount) =>
                {
                    _context.MailAccounts.Update(updatedAccount);
                    await _context.SaveChangesAsync();
                });

                if (string.IsNullOrEmpty(validAccessToken))
                {
                    Console.WriteLine($"--- Failed to get valid access token for account {mailAccount.MailAccountId}. User needs to re-authenticate. ---");
                    mailAccount.IsTokenValid = false;
                    await _context.SaveChangesAsync();
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

                // Fetch emails from Outlook using the valid token
                var outlookEmails = await _outlookService.GetUserEmailsAsync(validAccessToken, mailAccount.MailAccountId, 50);

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

                // Apply AI-powered tagging to all emails in batch
                if (outlookEmails.Any())
                {
                    try
                    {
                        Console.WriteLine($"--- Applying AI tagging to {outlookEmails.Count} emails ---");
                        var aiTags = await _aiTaggingService.GetAiTagsAsync(outlookEmails);
                        
                        // Apply the AI tags to the emails
                        foreach (var aiTag in aiTags)
                        {
                            var email = outlookEmails.FirstOrDefault(e => e.MailId == aiTag.MailReceivedId);
                            if (email != null)
                            {
                                email.MailTags.Add(aiTag);
                            }
                        }
                        
                        Console.WriteLine($"--- Successfully applied {aiTags.Count} AI-generated tags ---");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"--- AI tagging failed, continuing without AI tags: {ex.Message} ---");
                        // Continue without AI tags if the service fails
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

            foreach (var tag in availableTags) // Check all available tags
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
            }

            return mailTags;
        }
    }

}
