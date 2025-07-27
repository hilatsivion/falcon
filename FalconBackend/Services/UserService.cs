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
        private readonly AnalyticsService _analyticsService;

        public UserService(AppDbContext context, OutlookService outlookService, AiTaggingService aiTaggingService, AnalyticsService analyticsService)
        {
            _context = context;
            _outlookService = outlookService;
            _aiTaggingService = aiTaggingService;
            _analyticsService = analyticsService;
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
        /// Syncs both received emails (with AI tagging) and sent emails (without tagging)
        /// Properly updates analytics based on email dates
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

                // Sync received emails with AI tagging and analytics tracking
                await SyncReceivedEmailsAsync(validAccessToken, mailAccount);
                
                // Sync sent emails without tagging but with analytics tracking
                await SyncSentEmailsAsync(validAccessToken, mailAccount);

                // Update last sync time
                mailAccount.LastMailSync = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"--- Successfully completed sync for account {mailAccount.EmailAddress} ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- FAILED to sync emails for account {mailAccount.EmailAddress}: {ex.Message} ---");
                throw;
            }
        }

        /// <summary>
        /// Sync received emails and apply AI tagging with proper analytics tracking
        /// </summary>
        private async Task SyncReceivedEmailsAsync(string accessToken, MailAccount mailAccount)
        {
            Console.WriteLine($"--- Syncing received emails for account {mailAccount.EmailAddress} ---");
            
            // Fetch received emails from Outlook
            var outlookEmails = await _outlookService.GetUserEmailsAsync(accessToken, mailAccount.MailAccountId, mailAccount.AppUserEmail, 100);

            if (!outlookEmails.Any())
            {
                Console.WriteLine($"--- No new received emails found for account {mailAccount.EmailAddress} ---");
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
                Console.WriteLine($"--- No new received emails to sync for account {mailAccount.EmailAddress} ---");
                return;
            }

            Console.WriteLine($"--- Saving {newEmails.Count} new emails to database ---");
            
            // Add new received emails to the database FIRST
            _context.MailReceived.AddRange(newEmails);
            await _context.SaveChangesAsync();

            // CRITICAL: Refresh the email entities to get the generated MailId values
            Console.WriteLine($"--- Refreshing email IDs from database ---");
            var savedEmailIds = newEmails.Select(e => e.MailId).ToList();
            Console.WriteLine($"--- Generated MailIds: [{string.Join(", ", savedEmailIds)}] ---");

            // Verify the emails now have proper database IDs
            var emailsWithInvalidIds = newEmails.Where(e => e.MailId <= 0).ToList();
            if (emailsWithInvalidIds.Any())
            {
                Console.WriteLine($"--- ERROR: {emailsWithInvalidIds.Count} emails still have invalid MailIds ---");
                // Re-fetch from database to ensure we have correct IDs
                var uniqueKeys = newEmails.Select(e => new { e.Subject, e.TimeReceived, e.Sender }).ToList();
                newEmails = await _context.MailReceived
                    .Where(mr => mr.MailAccountId == mailAccount.MailAccountId)
                    .Where(mr => uniqueKeys.Any(uk => 
                        uk.Subject == mr.Subject && 
                        uk.TimeReceived == mr.TimeReceived && 
                        uk.Sender == mr.Sender))
                    .ToListAsync();
                
                Console.WriteLine($"--- Re-fetched emails with IDs: [{string.Join(", ", newEmails.Select(e => e.MailId))}] ---");
            }

            // Apply AI-powered tagging to new received emails AFTER they're saved and have proper IDs
            if (newEmails.Any())
            {
                try
                {
                    Console.WriteLine($"--- Applying AI tagging to {newEmails.Count} received emails with IDs: [{string.Join(", ", newEmails.Select(e => e.MailId))}] ---");
                    var aiTags = await _aiTaggingService.GetAiTagsAsync(newEmails);
                    
                    // Note: AI tagging service now handles its own database saves
                    Console.WriteLine($"--- AI tagging completed. Tags were processed by AiTaggingService ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--- AI tagging failed, continuing without AI tags: {ex.Message} ---");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"--- Inner exception: {ex.InnerException.Message} ---");
                    }
                    // Continue without AI tags if the service fails
                }
            }

            // Update analytics for received emails based on their actual date
            var receivedDates = newEmails.Select(email => email.TimeReceived).ToList();
            await _analyticsService.ProcessHistoricalEmailsAsync(mailAccount.AppUserEmail, receivedDates, new List<DateTime>());

            Console.WriteLine($"--- Successfully synced {newEmails.Count} new received emails for account {mailAccount.EmailAddress} ---");
        }

        /// <summary>
        /// Sync sent emails without applying any tagging but with proper analytics tracking
        /// </summary>
        private async Task SyncSentEmailsAsync(string accessToken, MailAccount mailAccount)
        {
            Console.WriteLine($"--- Syncing sent emails for account {mailAccount.EmailAddress} ---");
            
            try
            {
                // Fetch sent emails from Outlook Sent Items folder
                var outlookSentEmails = await _outlookService.GetUserSentEmailsAsync(accessToken, mailAccount.MailAccountId, mailAccount.AppUserEmail, 50);

                if (!outlookSentEmails.Any())
                {
                    Console.WriteLine($"--- No sent emails found for account {mailAccount.EmailAddress} ---");
                    return;
                }

                // Filter out emails that are already in the database (avoid duplicates)
                var existingSentEmailIds = await _context.MailSent
                    .Where(ms => ms.MailAccountId == mailAccount.MailAccountId)
                    .Select(ms => ms.Subject + "|" + ms.TimeSent.ToString("yyyy-MM-dd HH:mm:ss"))
                    .ToListAsync();

                var newSentEmails = outlookSentEmails.Where(email => 
                    !existingSentEmailIds.Contains(email.Subject + "|" + email.TimeSent.ToString("yyyy-MM-dd HH:mm:ss")))
                    .ToList();

                if (!newSentEmails.Any())
                {
                    Console.WriteLine($"--- No new sent emails to sync for account {mailAccount.EmailAddress} ---");
                    return;
                }

                // Add new sent emails to the database (no tagging for sent emails)
                _context.MailSent.AddRange(newSentEmails);
                await _context.SaveChangesAsync();

                // Update analytics for sent emails based on their actual date
                var sentDates = newSentEmails.Select(email => email.TimeSent).ToList();
                await _analyticsService.ProcessHistoricalEmailsAsync(mailAccount.AppUserEmail, new List<DateTime>(), sentDates);

                Console.WriteLine($"--- Successfully synced {newSentEmails.Count} new sent emails for account {mailAccount.EmailAddress} ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Failed to sync sent emails for account {mailAccount.EmailAddress}: {ex.Message} ---");
                // Don't throw here - continue with received email sync if sent email sync fails
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
        /// Syncs both received emails (with AI tagging) and sent emails (without tagging)
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
                bool hasExistingReceivedEmails = await _context.MailReceived
                                                  .AnyAsync(mr => mr.MailAccountId == mailAccount.MailAccountId);
                bool hasExistingSentEmails = await _context.MailSent
                                                  .AnyAsync(ms => ms.MailAccountId == mailAccount.MailAccountId);
                
                if (hasExistingReceivedEmails && hasExistingSentEmails)
                {
                    Console.WriteLine($"--- Emails already exist for account {mailAccount.MailAccountId}. Skipping to avoid duplicates. ---");
                    return;
                }

                // Sync received emails with AI tagging (if not already synced)
                if (!hasExistingReceivedEmails)
                {
                    await SyncReceivedEmailsForInitializationAsync(validAccessToken, mailAccount);
                }

                // Sync sent emails without tagging (if not already synced)
                if (!hasExistingSentEmails)
                {
                    await SyncSentEmailsForInitializationAsync(validAccessToken, mailAccount);
                }

                // Update last sync time
                mailAccount.LastMailSync = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"--- Successfully completed initialization for account {mailAccount.MailAccountId} ---");
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
        /// Sync received emails during initialization with AI tagging and analytics tracking
        /// </summary>
        private async Task SyncReceivedEmailsForInitializationAsync(string accessToken, MailAccount mailAccount)
        {
            Console.WriteLine($"--- Initializing received emails for account {mailAccount.EmailAddress} ---");
            
            // Fetch emails from Outlook using the valid token
            var outlookEmails = await _outlookService.GetUserEmailsAsync(accessToken, mailAccount.MailAccountId, mailAccount.AppUserEmail, 50);

            if (!outlookEmails.Any())
            {
                Console.WriteLine($"--- No emails found in Outlook for account {mailAccount.MailAccountId} ---");
                return;
            }

            Console.WriteLine($"--- Saving {outlookEmails.Count} emails to database ---");
            
            // Add emails to the database FIRST
            _context.MailReceived.AddRange(outlookEmails);
            await _context.SaveChangesAsync();

            // CRITICAL: Refresh the email entities to get the generated MailId values
            Console.WriteLine($"--- Refreshing email IDs from database ---");
            var savedEmailIds = outlookEmails.Select(e => e.MailId).ToList();
            Console.WriteLine($"--- Generated MailIds: [{string.Join(", ", savedEmailIds)}] ---");

            // Verify the emails now have proper database IDs
            var emailsWithInvalidIds = outlookEmails.Where(e => e.MailId <= 0).ToList();
            if (emailsWithInvalidIds.Any())
            {
                Console.WriteLine($"--- ERROR: {emailsWithInvalidIds.Count} emails still have invalid MailIds ---");
                // Re-fetch from database to ensure we have correct IDs
                var uniqueKeys = outlookEmails.Select(e => new { e.Subject, e.TimeReceived, e.Sender }).ToList();
                outlookEmails = await _context.MailReceived
                    .Where(mr => mr.MailAccountId == mailAccount.MailAccountId)
                    .Where(mr => uniqueKeys.Any(uk => 
                        uk.Subject == mr.Subject && 
                        uk.TimeReceived == mr.TimeReceived && 
                        uk.Sender == mr.Sender))
                    .ToListAsync();
                
                Console.WriteLine($"--- Re-fetched emails with IDs: [{string.Join(", ", outlookEmails.Select(e => e.MailId))}] ---");
            }

            // Apply AI-powered tagging to all emails AFTER they're saved and have proper IDs
            if (outlookEmails.Any())
            {
                try
                {
                    Console.WriteLine($"--- Applying AI tagging to {outlookEmails.Count} emails with IDs: [{string.Join(", ", outlookEmails.Select(e => e.MailId))}] ---");
                    var aiTags = await _aiTaggingService.GetAiTagsAsync(outlookEmails);
                    
                    // Note: AI tagging service now handles its own database saves
                    Console.WriteLine($"--- AI tagging completed. Tags were processed by AiTaggingService ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--- AI tagging failed, continuing without AI tags: {ex.Message} ---");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"--- Inner exception: {ex.InnerException.Message} ---");
                    }
                    // Continue without AI tags if the service fails
                }
            }

            // Update analytics for received emails based on their actual date
            var receivedDates = outlookEmails.Select(email => email.TimeReceived).ToList();
            await _analyticsService.ProcessHistoricalEmailsAsync(mailAccount.AppUserEmail, receivedDates, new List<DateTime>());

            Console.WriteLine($"--- Successfully fetched and saved {outlookEmails.Count} real Outlook received emails for account {mailAccount.MailAccountId} ---");
        }

        /// <summary>
        /// Sync sent emails during initialization without tagging but with analytics tracking
        /// </summary>
        private async Task SyncSentEmailsForInitializationAsync(string accessToken, MailAccount mailAccount)
        {
            Console.WriteLine($"--- Initializing sent emails for account {mailAccount.EmailAddress} ---");
            
            try
            {
                // Fetch sent emails from Outlook Sent Items folder
                var outlookSentEmails = await _outlookService.GetUserSentEmailsAsync(accessToken, mailAccount.MailAccountId, mailAccount.AppUserEmail, 50);

                if (!outlookSentEmails.Any())
                {
                    Console.WriteLine($"--- No sent emails found in Outlook for account {mailAccount.MailAccountId} ---");
                    return;
                }

                // Add sent emails to the database (no tagging for sent emails)
                _context.MailSent.AddRange(outlookSentEmails);
                await _context.SaveChangesAsync();

                // Update analytics for sent emails based on their actual date
                var sentDates = outlookSentEmails.Select(email => email.TimeSent).ToList();
                await _analyticsService.ProcessHistoricalEmailsAsync(mailAccount.AppUserEmail, new List<DateTime>(), sentDates);

                Console.WriteLine($"--- Successfully fetched and saved {outlookSentEmails.Count} real Outlook sent emails for account {mailAccount.MailAccountId} ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Failed to sync sent emails during initialization for account {mailAccount.EmailAddress}: {ex.Message} ---");
                // Don't throw here - continue with the process
            }
        }
    }

}
