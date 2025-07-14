using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;

namespace FalconBackend.Services
{
    public class MailService
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorageService;
        private readonly AnalyticsService _analyticsService;
        private readonly OutlookService _outlookService;

        public MailService(AppDbContext context, FileStorageService fileStorageService, AnalyticsService analyticsService, OutlookService outlookService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _analyticsService = analyticsService;
            _outlookService = outlookService;
        }

        // DTO Conversion
        private static List<AttachmentDto> ToAttachmentDtos(ICollection<Attachments> attachments) =>
            attachments?.Select(a => new AttachmentDto
            {
                AttachmentId = a.Id,
                Name = a.Name,
                FileType = a.FileType,
                FileSize = a.FileSize,
                FilePath = a.FilePath
            }).ToList() ?? new();

        private static List<RecipientDto> ToRecipientDtos(ICollection<Recipient> recipients) =>
            recipients?.Select(r => new RecipientDto
            {
                RecipientId = r.Id, // Fixing the property name to match the Recipient class
                Email = r.Email
            }).ToList() ?? new();

        private static List<TagDto> ToTagDtos(ICollection<MailTag> mailTags) =>
            mailTags?.Select(mt => new TagDto
            {
                TagId = mt.TagId,
                Name = mt.Tag?.TagName,
                TagType = mt.Tag?.GetType().Name
            }).ToList() ?? new();

        public async Task<List<MailReceivedPreviewDto>> GetAllReceivedEmailPreviewsAsync(string userEmail, int page, int pageSize)
        {
            return await _context.MailReceived
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId)
                    && !m.IsDeleted)
                .OrderByDescending(m => m.TimeReceived)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailReceivedPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    Sender = m.Sender,
                    TimeReceived = m.TimeReceived,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body),
                    IsRead = m.IsRead,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam
                })
                .ToListAsync();
        }

        public async Task<List<MailSentPreviewDto>> GetAllSentEmailPreviewsAsync(string userEmail, int page, int pageSize)
        {
            return await _context.MailSent
                .Include(m => m.Recipients)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId)
                    && !m.IsDeleted)
                .OrderByDescending(m => m.TimeSent)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailSentPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    TimeSent = m.TimeSent,
                    Recipients = m.Recipients.Select(r => r.Email).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body), 
                    IsFavorite = m.IsFavorite
                })
                .ToListAsync();
        }

        public async Task<List<DraftPreviewDto>> GetAllDraftEmailPreviewsAsync(string userEmail, int page, int pageSize)
        {
            return await _context.Drafts
                .Include(m => m.Recipients)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId))
                .OrderByDescending(m => m.TimeCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new DraftPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    TimeCreated = m.TimeCreated,
                    Recipients = m.Recipients.Select(r => r.Email).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<MailReceivedPreviewDto>> GetReceivedEmailPreviewsByMailAccountAsync(string mailAccountId, int page, int pageSize)
        {
            return await _context.MailReceived
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .Where(m => m.MailAccount.MailAccountId == mailAccountId && !m.IsDeleted)
                .OrderByDescending(m => m.TimeReceived)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailReceivedPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    Sender = m.Sender,
                    TimeReceived = m.TimeReceived,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body), 
                    IsRead = m.IsRead,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam
                })
                .ToListAsync();
        }

        public async Task<List<MailSentPreviewDto>> GetSentEmailPreviewsByMailAccountAsync(string mailAccountId, int page, int pageSize)
        {
            return await _context.MailSent
                .Include(m => m.Recipients)
                .Where(m => m.MailAccount.MailAccountId == mailAccountId && !m.IsDeleted)
                .OrderByDescending(m => m.TimeSent)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailSentPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    TimeSent = m.TimeSent,
                    Recipients = m.Recipients.Select(r => r.Email).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body), 
                    IsFavorite = m.IsFavorite
                })
                .ToListAsync();
        }

        public async Task<List<DraftPreviewDto>> GetDraftEmailPreviewsByMailAccountAsync(string mailAccountId, int page, int pageSize)
        {
            return await _context.Drafts
                .Include(m => m.Recipients)
                .Where(m => m.MailAccount.MailAccountId == mailAccountId)
                .OrderByDescending(m => m.TimeCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new DraftPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    TimeCreated = m.TimeCreated,
                    Recipients = m.Recipients.Select(r => r.Email).ToList()
                })
                .ToListAsync();
        }

        public async Task<AllEmailsForAccountDto> GetAllEmailsForMailAccountAsync(string mailAccountId, int page = 1, int pageSize = 100)
        {
            // Get all three types of emails sequentially to avoid DbContext threading issues
            var receivedEmails = await GetReceivedEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);
            var sentEmails = await GetSentEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);
            var drafts = await GetDraftEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);

            return new AllEmailsForAccountDto
            {
                ReceivedEmails = receivedEmails,
                SentEmails = sentEmails,
                Drafts = drafts
            };
        }


        public async Task<bool> ToggleFavoriteAsync(int mailId, bool isFavorite)
        {
            var mail = await _context.Mails.FindAsync(mailId);
            if (mail == null) return false;

            mail.IsFavorite = isFavorite;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleSpamAsync(int mailId, bool isSpam)
        {
            var mail = await _context.Mails.FindAsync(mailId);
            if (mail == null) return false;

            // Check if state is actually changing
            bool stateChanged = mail.IsSpam != isSpam;

            mail.IsSpam = isSpam;
            
            // Business logic: When marking as spam, remove favorite status
            if (isSpam)
            {
                mail.IsFavorite = false;
            }

            await _context.SaveChangesAsync();

            // Add analytics tracking when marking as spam
            if (_analyticsService != null && isSpam && stateChanged)
            {
                // Get user email from mail account
                var mailAccount = await _context.MailAccounts
                    .Where(ma => ma.MailAccountId == mail.MailAccountId)
                    .FirstOrDefaultAsync();
                
                if (mailAccount != null)
                {
                    await _analyticsService.UpdateSpamEmailsWeeklyAsync(mailAccount.AppUserEmail);
                    Console.WriteLine($"Incremented spam count for {mailAccount.AppUserEmail}");
                }
            }

            return true;
        }

        public async Task<bool> ToggleSpamBatchAsync(List<ToggleSpamDto> spamUpdates, string userEmail)
        {
            if (spamUpdates == null || spamUpdates.Count == 0)
                return false;

            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            var mailIds = spamUpdates.Select(x => x.MailId).ToList();
            var mailUpdateMap = spamUpdates.ToDictionary(x => x.MailId, x => x.IsSpam);

            var mails = await _context.Mails
                .Where(m => mailIds.Contains(m.MailId))
                .ToListAsync();

            // Ensure all belong to the user's accounts
            if (mails.Any(m => !userMailAccountIds.Contains(m.MailAccountId)))
                return false;

            int spamMarkCount = 0; // Count how many emails are marked as spam

            foreach (var mail in mails)
            {
                bool isSpam = mailUpdateMap[mail.MailId];
                if (mail.IsSpam != isSpam)
                {
                    mail.IsSpam = isSpam;
                    
                    // Business logic: When marking as spam, remove favorite status
                    if (isSpam)
                    {
                        mail.IsFavorite = false;
                        spamMarkCount++; // Count when marking as spam
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Add analytics tracking for marked spam emails
            if (_analyticsService != null && spamMarkCount > 0)
            {
                // Call increment method 'spamMarkCount' times
                for (int i = 0; i < spamMarkCount; i++)
                {
                    await _analyticsService.UpdateSpamEmailsWeeklyAsync(userEmail);
                }
                Console.WriteLine($"Incremented spam count by {spamMarkCount} for {userEmail}");
            }

            return true;
        }

        public async Task<bool> DeleteMailsAsync(List<MailDeleteDto> mailsToDelete, string userEmail)
        {
            bool anyDeleted = false;
            int deleteCount = 0; // Count how many were actually deleted

            // --- Get user's accounts once ---
            var userMailAccountIds = await _context.MailAccounts
                                           .Where(ma => ma.AppUserEmail == userEmail)
                                           .Select(ma => ma.MailAccountId)
                                           .ToListAsync();

            var mailIdsToDelete = mailsToDelete.Select(dto => dto.MailId).ToList();

            // Fetch all potentially relevant mails at once
            var mails = await _context.Mails
                .Include(m => m.Attachments)
                .Include(m => m.Recipients) 
                .Include(m => (m as MailReceived).MailTags) 
                .Where(m => mailIdsToDelete.Contains(m.MailId))
                .ToListAsync();

            var mailsOwnedByUser = mails.Where(m => userMailAccountIds.Contains(m.MailAccountId)).ToList();

            if (mailsOwnedByUser.Count != mailsToDelete.Count)
            {
                Console.WriteLine("Attempted to delete emails not owned by user or not found matching DTOs.");
            }

            foreach (var mail in mailsOwnedByUser) // Iterate only through owned mails found
            {
                // Soft delete - mark as deleted and remove favorite status
                mail.IsDeleted = true;
                mail.IsFavorite = false; // Remove favorite status when deleting
                deleteCount++;
                anyDeleted = true;
            }

            if (anyDeleted)
            {
                await _context.SaveChangesAsync();

                // --- ADD Analytics Call - Call ONCE for the total count deleted ---
                if (_analyticsService != null && deleteCount > 0)
                {
                    // Call increment method 'deleteCount' times
                    for (int i = 0; i < deleteCount; i++)
                    {
                        await _analyticsService.IncrementDeletedEmailsWeeklyAsync(userEmail);
                    }
                    Console.WriteLine($"Incremented deleted count by {deleteCount} for {userEmail}");
                }
            }

            return anyDeleted;
        }

        public async Task<bool> RestoreMailsAsync(List<MailDeleteDto> mailsToRestore, string userEmail)
        {
            bool anyRestored = false;

            // --- Get user's accounts once ---
            var userMailAccountIds = await _context.MailAccounts
                                           .Where(ma => ma.AppUserEmail == userEmail)
                                           .Select(ma => ma.MailAccountId)
                                           .ToListAsync();

            var mailIdsToRestore = mailsToRestore.Select(dto => dto.MailId).ToList();

            // Fetch all potentially relevant mails at once
            var mails = await _context.Mails
                .Where(m => mailIdsToRestore.Contains(m.MailId))
                .ToListAsync();

            var mailsOwnedByUser = mails.Where(m => userMailAccountIds.Contains(m.MailAccountId)).ToList();

            if (mailsOwnedByUser.Count != mailsToRestore.Count)
            {
                Console.WriteLine("Attempted to restore emails not owned by user or not found matching DTOs.");
            }

            foreach (var mail in mailsOwnedByUser) // Iterate only through owned mails found
            {
                // Restore - mark as not deleted
                mail.IsDeleted = false;
                anyRestored = true;
            }

            if (anyRestored)
            {
                await _context.SaveChangesAsync();
            }

            return anyRestored;
        }

        private async Task SaveAttachments(List<IFormFile> attachments, int mailId, string mailAccountId, string emailType, ICollection<Attachments> emailAttachments)
        {
            if (attachments == null || attachments.Count == 0)
                return;

            var existing = await _context.Attachments
                .Where(a => a.MailId == mailId).Select(a => a.Name).ToListAsync();

            foreach (var file in attachments)
            {
                if (existing.Contains(file.FileName)) continue;

                var path = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, emailType);

                var attachment = new Attachments
                {
                    MailId = mailId,
                    Name = file.FileName,
                    FileType = Path.GetExtension(file.FileName),
                    FileSize = file.Length,
                    FilePath = path
                };

                _context.Attachments.Add(attachment);
                emailAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ToggleReadBatchAsync(List<ToggleReadDto> readUpdates, string userEmail)
        {
            if (readUpdates == null || readUpdates.Count == 0)
                return false;

            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            var mailIds = readUpdates.Select(x => x.MailId).ToList();

            var mailUpdateMap = readUpdates.ToDictionary(x => x.MailId, x => x.IsRead);

            var mails = await _context.MailReceived
                .Where(m => mailIds.Contains(m.MailId))
                .ToListAsync();

            // Ensure all belong to the user's accounts
            if (mails.Any(m => !userMailAccountIds.Contains(m.MailAccountId)))
                return false;

            foreach (var mail in mails)
            {
                bool isRead = mailUpdateMap[mail.MailId];
                if (mail.IsRead != isRead)
                {
                    mail.IsRead = isRead;

                    if (isRead)
                    {
                        var email = userEmail;
                        await _analyticsService.UpdateReadEmailsWeeklyAsync(email);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserOwnerOfMailAsync(int mailId, string userEmail)
        {
            return await _context.Mails
                .AnyAsync(m => m.MailId == mailId &&
                               _context.MailAccounts
                                   .Where(ma => ma.AppUserEmail == userEmail)
                                   .Select(ma => ma.MailAccountId)
                                   .Contains(m.MailAccountId));
        }

        public async Task<bool> AreAllMailsOwnedByUserAsync(List<MailDeleteDto> mails, string userEmail)
        {
            var userMailAccountIds = await _context.MailAccounts
                .Where(ma => ma.AppUserEmail == userEmail)
                .Select(ma => ma.MailAccountId)
                .ToListAsync();

            return mails.All(mail => userMailAccountIds.Contains(mail.MailAccountId));
        }

        public async Task<bool> DoesUserOwnMailAccountAsync(string userEmail, string mailAccountId)
        {
            return await _context.MailAccounts
                .AnyAsync(ma => ma.MailAccountId == mailAccountId && ma.AppUserEmail == userEmail);
        }

        public async Task SendMailAsync(SendMailRequest request, List<IFormFile> attachments)
        {
            if (request.Recipients == null || !request.Recipients.Any())
            {
                throw new ArgumentException("Recipient list cannot be empty.");
            }

            var senderAccount = await _context.MailAccounts
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(ma => ma.MailAccountId == request.MailAccountId);

            if (senderAccount == null)
            {
                throw new InvalidOperationException("Sender mail account not found.");
            }
            string senderDisplayAddress = senderAccount.EmailAddress;

            // Actually send the email via Outlook API
            Console.WriteLine($"--- Sending email via Outlook API: {request.Subject} ---");
            bool emailSent = await _outlookService.SendEmailAsync(senderAccount.AccessToken, request);
            
            if (!emailSent)
            {
                throw new InvalidOperationException("Failed to send email via Outlook API. Email not sent.");
            }
            Console.WriteLine($"--- Email sent successfully via Outlook API ---");

            var executionStrategy = _context.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var mailSent = new MailSent
                    {
                        MailAccountId = request.MailAccountId,
                        Subject = request.Subject,
                        Body = request.Body,
                        TimeSent = DateTime.UtcNow,
                        Recipients = request.Recipients.Select(email => new Recipient { Email = email }).ToList(),
                        Attachments = new List<Attachments>()
                    };

                    _context.MailSent.Add(mailSent);
                    await _context.SaveChangesAsync();

                    await SaveAttachments(attachments, mailSent.MailId, mailSent.MailAccountId, "Sent", mailSent.Attachments);
                    await _context.SaveChangesAsync(); // Save attachments added in SaveAttachments

                    
                    List<MailReceived> receivedCopies = new List<MailReceived>();
                    foreach (var recipientEmail in request.Recipients)
                    {
                        var recipientMailAccount = await _context.MailAccounts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ma => ma.EmailAddress == recipientEmail);

                        if (recipientMailAccount != null)
                        {
                            var receivedAttachments = new List<Attachments>();
                            // Copy attachment records after saving mailSent and its attachments
                            foreach (var sentAttachment in mailSent.Attachments)
                            {
                                receivedAttachments.Add(new Attachments
                                {
                                    // MailId will be set by EF Core relationship
                                    Name = sentAttachment.Name,
                                    FileType = sentAttachment.FileType,
                                    FileSize = sentAttachment.FileSize,
                                    FilePath = sentAttachment.FilePath // Link to the same physical file
                                });
                            }

                            var mailReceived = new MailReceived
                            {
                                MailAccountId = recipientMailAccount.MailAccountId,
                                Sender = senderDisplayAddress,
                                Subject = request.Subject,
                                Body = request.Body,
                                TimeReceived = mailSent.TimeSent,
                                IsRead = false,
                                IsFavorite = false,
                                Attachments = receivedAttachments, // Assign copied attachment records
                                Recipients = new List<Recipient> { new Recipient { Email = recipientEmail } },
                                MailTags = new List<MailTag>()
                            };
                            receivedCopies.Add(mailReceived);

                            if (_analyticsService != null)
                            {
                                await _analyticsService.UpdateEmailsReceivedWeeklyAsync(recipientMailAccount.AppUserEmail);
                            }
                        }
                    }

                    if (receivedCopies.Any())
                    {
                        _context.MailReceived.AddRange(receivedCopies);
                    }

                    await _context.SaveChangesAsync(); // Save received copies and their attachments

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync();
                    }
                    Console.WriteLine($"Error during SendMail transaction: {ex.Message}");
                    throw;
                }
            });

            var senderUserEmail = senderAccount.AppUserEmail;
            if (!string.IsNullOrEmpty(senderUserEmail) && _analyticsService != null)
            {
                await _analyticsService.UpdateEmailsSentWeeklyAsync(senderUserEmail);
            }
        }

        public async Task<MailReceivedDto?> GetReceivedMailByIdAsync(string userEmail, int mailId)
        {
            var mail = await _context.MailReceived
                .Include(m => m.Attachments)
                .Include(m => m.Recipients)
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .FirstOrDefaultAsync(m => m.MailId == mailId &&
                                          _context.MailAccounts
                                            .Where(ma => ma.AppUserEmail == userEmail)
                                            .Select(ma => ma.MailAccountId)
                                            .Contains(m.MailAccountId));

            if (mail == null) return null;

            return new MailReceivedDto
            {
                MailId = mail.MailId,
                MailAccountId = mail.MailAccountId,
                Sender = mail.Sender,
                Subject = mail.Subject,
                Body = mail.Body,
                TimeReceived = mail.TimeReceived,
                IsRead = mail.IsRead,
                IsFavorite = mail.IsFavorite,
                Attachments = ToAttachmentDtos(mail.Attachments),
                Recipients = ToRecipientDtos(mail.Recipients),
                Tags = ToTagDtos(mail.MailTags)
            };
        }
        public async Task<MailSentDto?> GetSentMailByIdAsync(string userEmail, int mailId)
        {
            var mail = await _context.MailSent
                .Include(m => m.Attachments)
                .Include(m => m.Recipients)
                .FirstOrDefaultAsync(m => m.MailId == mailId &&
                                          _context.MailAccounts
                                            .Where(ma => ma.AppUserEmail == userEmail)
                                            .Select(ma => ma.MailAccountId)
                                            .Contains(m.MailAccountId));

            if (mail == null) return null;

            return new MailSentDto
            {
                MailId = mail.MailId,
                MailAccountId = mail.MailAccountId,
                Subject = mail.Subject,
                Body = mail.Body,
                TimeSent = mail.TimeSent,
                IsFavorite = mail.IsFavorite,
                Attachments = ToAttachmentDtos(mail.Attachments),
                Recipients = ToRecipientDtos(mail.Recipients)
            };
        }

        public async Task<DraftDto?> GetDraftByIdAsync(string userEmail, int mailId)
        {
            var mail = await _context.Drafts
                .Include(m => m.Attachments)
                .Include(m => m.Recipients)
                .FirstOrDefaultAsync(m => m.MailId == mailId &&
                                          _context.MailAccounts
                                            .Where(ma => ma.AppUserEmail == userEmail)
                                            .Select(ma => ma.MailAccountId)
                                            .Contains(m.MailAccountId));

            if (mail == null) return null;

            return new DraftDto
            {
                MailId = mail.MailId,
                MailAccountId = mail.MailAccountId,
                Subject = mail.Subject,
                Body = mail.Body,
                TimeCreated = mail.TimeCreated,
                IsSent = mail.IsSent,
                IsFavorite = mail.IsFavorite,
                Attachments = ToAttachmentDtos(mail.Attachments),
                Recipients = ToRecipientDtos(mail.Recipients)
            };
        }


        public async Task<List<MailSearchResultDto>> SearchEmailsAsync(string userEmail, string keywords, string fromSender, string toRecipient)
        {
            var userMailAccountIds = await _context.MailAccounts
                                              .Where(ma => ma.AppUserEmail == userEmail)
                                              .Select(ma => ma.MailAccountId)
                                              .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new List<MailSearchResultDto>();
            }

            var receivedQuery = _context.MailReceived
                                        .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                                        .Where(m => userMailAccountIds.Contains(m.MailAccountId));

            var sentQuery = _context.MailSent
                                    .Include(m => m.Recipients)
                                    .Where(m => userMailAccountIds.Contains(m.MailAccountId));

            // --- Apply Filters (No changes needed here) ---
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                string k = keywords.ToLower();
                receivedQuery = receivedQuery.Where(m => (m.Subject != null && m.Subject.ToLower().Contains(k)) || (m.Body != null && m.Body.ToLower().Contains(k)) || (m.Sender != null && m.Sender.ToLower().Contains(k)));
                sentQuery = sentQuery.Where(m => (m.Subject != null && m.Subject.ToLower().Contains(k)) || (m.Body != null && m.Body.ToLower().Contains(k)) || m.Recipients.Any(r => r.Email.ToLower().Contains(k)));
            }
            if (!string.IsNullOrWhiteSpace(fromSender))
            {
                string s = fromSender.ToLower();
                receivedQuery = receivedQuery.Where(m => m.Sender != null && m.Sender.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(toRecipient))
            {
                string r = toRecipient.ToLower();
                sentQuery = sentQuery.Where(m => m.Recipients.Any(rec => rec.Email.ToLower().Contains(r)));
            }

            // --- Execute Queries Sequentially ---
            var receivedResults = await receivedQuery // <<< Await the first query
                .OrderByDescending(m => m.TimeReceived)
                .Take(100)
                .Select(m => new MailSearchResultDto
                { /* ... mapping ... */
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    BodySnippet = GenerateBodySnippet(m.Body),
                    Date = m.TimeReceived,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam,
                    Type = "received",
                    IsRead = m.IsRead,
                    Sender = m.Sender,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList() ?? new List<string>()
                })
                .ToListAsync();

            var sentResults = await sentQuery // <<< Await the second query separately
               .OrderByDescending(m => m.TimeSent)
               .Take(100)
               .Select(m => new MailSearchResultDto
               { /* ... mapping ... */
                   MailId = m.MailId,
                   MailAccountId = m.MailAccountId,
                   Subject = m.Subject,
                   BodySnippet = GenerateBodySnippet(m.Body),
                   Date = m.TimeSent,
                   IsFavorite = m.IsFavorite,
                   IsSpam = m.IsSpam,
                   Type = "sent",
                   IsRead = true,
                   Recipients = m.Recipients.Select(rec => rec.Email).ToList() ?? new List<string>()
               })
               .ToListAsync();

            // *** Removed: await Task.WhenAll(receivedResultsTask, sentResultsTask); ***

            // --- Combine & Sort Results (No changes needed here) ---
            var combinedResults = receivedResults.Concat(sentResults) // Use the results directly
                                                .OrderByDescending(r => r.Date)
                                                .Take(150)
                                                .ToList();

            return combinedResults;
        }

        // Ensure GenerateBodySnippet helper exists
        private static string GenerateBodySnippet(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            
            // Remove HTML tags using regex
            var plainText = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", " ");
            
            // Replace multiple whitespaces with single space and trim
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();
            
            var words = plainText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var snippet = string.Join(" ", words.Take(15));
            if (words.Length > 15) snippet += "...";
            return snippet;
        }

        public async Task<FavoriteEmailsDto> GetFavoriteEmailPreviewsAsync(string userEmail, int page = 1, int pageSize = 100) 
        {
            var userMailAccountIds = await _context.MailAccounts
                                              .Where(ma => ma.AppUserEmail == userEmail)
                                              .Select(ma => ma.MailAccountId)
                                              .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                return new FavoriteEmailsDto(); 
            }

            int itemsToTakePerType = pageSize; 

            var receivedFavoritesQuery = _context.MailReceived
                                        .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                                        .Where(m => userMailAccountIds.Contains(m.MailAccountId) && m.IsFavorite == true);

            var sentFavoritesQuery = _context.MailSent
                                    .Include(m => m.Recipients)
                                    .Where(m => userMailAccountIds.Contains(m.MailAccountId) && m.IsFavorite == true);

            // Execute sequentially
            var receivedFavorites = await receivedFavoritesQuery
                                        .OrderByDescending(m => m.TimeReceived) 
                                        .Skip((page - 1) * itemsToTakePerType) 
                                        .Take(itemsToTakePerType)
                                        .Select(m => new MailReceivedPreviewDto
                                        {
                                            MailId = m.MailId,
                                            MailAccountId = m.MailAccountId,
                                            Subject = m.Subject,
                                            Sender = m.Sender,
                                            TimeReceived = m.TimeReceived,
                                            Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList() ?? new List<string>(),
                                            BodySnippet = GenerateBodySnippet(m.Body),
                                            IsRead = m.IsRead,
                                            IsFavorite = m.IsFavorite,
                                            IsSpam = m.IsSpam
                                        })
                                        .ToListAsync();

            var sentFavorites = await sentFavoritesQuery
                                        .OrderByDescending(m => m.TimeSent) 
                                        .Skip((page - 1) * itemsToTakePerType) 
                                        .Take(itemsToTakePerType)
                                        .Select(m => new MailSentPreviewDto
                                        {
                                            MailId = m.MailId,
                                            MailAccountId = m.MailAccountId,
                                            Subject = m.Subject,
                                            TimeSent = m.TimeSent,
                                            Recipients = m.Recipients.Select(r => r.Email).ToList() ?? new List<string>(),
                                            BodySnippet = GenerateBodySnippet(m.Body),
                                            IsFavorite = m.IsFavorite
                                        })
                                        .ToListAsync();

            return new FavoriteEmailsDto
            {
                ReceivedFavorites = receivedFavorites,
                SentFavorites = sentFavorites
            };
        }


        public async Task<List<MailReceivedPreviewDto>> GetUnreadEmailPreviewsAsync(string userEmail, int page = 1, int pageSize = 100)
        {
            return await _context.MailReceived
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId)
                    && !m.IsRead && !m.IsDeleted)
                .OrderByDescending(m => m.TimeReceived)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailReceivedPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    Sender = m.Sender,
                    TimeReceived = m.TimeReceived,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body),
                    IsRead = m.IsRead,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam
                })
                .ToListAsync();
        }

        public async Task<List<MailReceivedPreviewDto>> GetTrashEmailPreviewsAsync(string userEmail, int page = 1, int pageSize = 100)
        {
            return await _context.MailReceived
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId)
                    && m.IsDeleted)
                .OrderByDescending(m => m.TimeReceived)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailReceivedPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    Sender = m.Sender,
                    TimeReceived = m.TimeReceived,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body),
                    IsRead = m.IsRead,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam
                })
                .ToListAsync();
        }

        public async Task<List<MailReceivedPreviewDto>> GetSpamEmailPreviewsAsync(string userEmail, int page = 1, int pageSize = 100)
        {
            return await _context.MailReceived
                .Include(m => m.MailTags).ThenInclude(mt => mt.Tag)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId)
                    && m.IsSpam && !m.IsDeleted)
                .OrderByDescending(m => m.TimeReceived)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MailReceivedPreviewDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    Sender = m.Sender,
                    TimeReceived = m.TimeReceived,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList(),
                    BodySnippet = GenerateBodySnippet(m.Body),
                    IsRead = m.IsRead,
                    IsFavorite = m.IsFavorite,
                    IsSpam = m.IsSpam
                })
                .ToListAsync();
        }

    }

}
