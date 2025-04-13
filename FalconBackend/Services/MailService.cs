using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FalconBackend.Services
{
    public class MailService
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorageService;
        private readonly AnalyticsService _analyticsService;

        public MailService(AppDbContext context, FileStorageService fileStorageService, AnalyticsService analyticsService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _analyticsService = analyticsService;
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
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId))
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
                    IsFavorite = m.IsFavorite
                })
                .ToListAsync();
        }

        public async Task<List<MailSentPreviewDto>> GetAllSentEmailPreviewsAsync(string userEmail, int page, int pageSize)
        {
            return await _context.MailSent
                .Include(m => m.Recipients)
                .Where(m => _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId).Contains(m.MailAccountId))
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
                .Where(m => m.MailAccount.MailAccountId == mailAccountId)
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
                    IsFavorite = m.IsFavorite
                })
                .ToListAsync();
        }

        public async Task<List<MailSentPreviewDto>> GetSentEmailPreviewsByMailAccountAsync(string mailAccountId, int page, int pageSize)
        {
            return await _context.MailSent
                .Include(m => m.Recipients)
                .Where(m => m.MailAccount.MailAccountId == mailAccountId)
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


        public async Task<bool> ToggleFavoriteAsync(int mailId, bool isFavorite)
        {
            var mail = await _context.Mails.FindAsync(mailId);
            if (mail == null) return false;

            mail.IsFavorite = isFavorite;
            await _context.SaveChangesAsync();
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
                // Remove associated MailTags if it's MailReceived
                if (mail is MailReceived received && received.MailTags != null)
                {
                    _context.MailTags.RemoveRange(received.MailTags);
                }

                if (mail.Attachments != null)
                {
                    foreach (var attachment in mail.Attachments.ToList()) // Use ToList() for safe removal during iteration
                    {
                        try
                        {
                            // Attempt file deletion (optional - depends on if you want physical delete)
                            if (!string.IsNullOrEmpty(attachment.FilePath) && File.Exists(attachment.FilePath))
                            {
                                File.Delete(attachment.FilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting attachment file {attachment.FilePath}: {ex.Message}");
                            // Decide if this error should stop the process
                        }
                        _context.Attachments.Remove(attachment); // Remove from DB regardless of file deletion success
                    }
                }


                _context.Mails.Remove(mail); // Remove the mail itself
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
            // --- Recipient Validation (Keep from previous implementation) ---
            if (request.Recipients == null || !request.Recipients.Any())
            {
                throw new ArgumentException("Recipient list cannot be empty.");
            }
            foreach (var recipientEmail in request.Recipients)
            {
                // Check if the recipient email exists in any MailAccount record
                var recipientAccountExists = await _context.MailAccounts
                                                   .AnyAsync(ma => ma.EmailAddress == recipientEmail);
                if (!recipientAccountExists)
                {
                    throw new KeyNotFoundException($"Recipient email not registered in any mail account: {recipientEmail}");
                }
            }

            var senderAccount = await _context.MailAccounts
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(ma => ma.MailAccountId == request.MailAccountId);

            if (senderAccount == null)
            {
                throw new InvalidOperationException("Sender mail account not found.");
            }
            string senderDisplayAddress = senderAccount.EmailAddress;

            var mailSent = new MailSent
            {
                MailAccountId = request.MailAccountId,
                Subject = request.Subject,
                Body = request.Body,
                TimeSent = DateTime.UtcNow,
                Recipients = request.Recipients.Select(email => new Recipient { Email = email }).ToList(),
                Attachments = new List<Attachments>()
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.MailSent.Add(mailSent);
                await _context.SaveChangesAsync();

                await SaveAttachments(attachments, mailSent.MailId, mailSent.MailAccountId, "Sent", mailSent.Attachments);

                List<MailReceived> receivedCopies = new List<MailReceived>();
                foreach (var recipientEmail in request.Recipients)
                {
                    var recipientMailAccount = await _context.MailAccounts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ma => ma.EmailAddress == recipientEmail);

                    if (recipientMailAccount != null)
                    {
                        var mailReceived = new MailReceived
                        {
                            MailAccountId = recipientMailAccount.MailAccountId,
                            Sender = senderDisplayAddress,
                            Subject = request.Subject,
                            Body = request.Body,
                            TimeReceived = mailSent.TimeSent,
                            IsRead = false,
                            IsFavorite = false,
                            Attachments = new List<Attachments>(),
                            Recipients = new List<Recipient> { new Recipient { Email = recipientEmail } },
                            MailTags = new List<MailTag>()
                        };
                        receivedCopies.Add(mailReceived);

                        // --- Update Recipient's Analytics --
                        if (_analyticsService != null)
                        {
                            await _analyticsService.UpdateEmailsReceivedWeeklyAsync(recipientMailAccount.AppUserEmail);
                        }
                    }
                }

                // 3. Add all generated MailReceived records 
                if (receivedCopies.Any())
                {
                    _context.MailReceived.AddRange(receivedCopies);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var senderUserEmail = senderAccount.AppUserEmail;
                if (!string.IsNullOrEmpty(senderUserEmail) && _analyticsService != null)
                {
                    await _analyticsService.UpdateEmailsSentWeeklyAsync(senderUserEmail);
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; 
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
        private static string GenerateBodySnippet(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }
            var words = body.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var snippet = string.Join(" ", words.Take(10));
            if (words.Length > 10)
            {
                snippet += ".....";
            }
            return snippet;
        }

        public async Task<List<MailSearchResultDto>> SearchEmailsAsync(string userEmail, string keywords, string fromSender, string toRecipient)
        {
            Console.WriteLine($"--- Searching emails for user: {userEmail}, keywords: '{keywords}', from: '{fromSender}', to: '{toRecipient}' ---");

            // Get the mail account IDs for the current user
            var userMailAccountIds = await _context.MailAccounts
                                              .Where(ma => ma.AppUserEmail == userEmail)
                                              .Select(ma => ma.MailAccountId)
                                              .ToListAsync();

            if (!userMailAccountIds.Any())
            {
                Console.WriteLine($"--- No mail accounts found for user {userEmail}, returning empty search results. ---");
                return new List<MailSearchResultDto>(); // No accounts, no emails
            }

            // Base queries for received and sent mails for this user
            var receivedQuery = _context.MailReceived
                                        .Include(m => m.MailTags).ThenInclude(mt => mt.Tag) // Include tags for received
                                        .Where(m => userMailAccountIds.Contains(m.MailAccountId));

            var sentQuery = _context.MailSent
                                    .Include(m => m.Recipients) // Include recipients for sent
                                    .Where(m => userMailAccountIds.Contains(m.MailAccountId));

            // Apply filters conditionally
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                receivedQuery = receivedQuery.Where(m => (m.Subject != null && m.Subject.Contains(keywords)) || (m.Body != null && m.Body.Contains(keywords)) || (m.Sender != null && m.Sender.Contains(keywords)));
                sentQuery = sentQuery.Where(m => (m.Subject != null && m.Subject.Contains(keywords)) || (m.Body != null && m.Body.Contains(keywords)) || m.Recipients.Any(r => r.Email.Contains(keywords))); // Search recipients too for keywords in sent mail
            }

            if (!string.IsNullOrWhiteSpace(fromSender))
            {
                // Primarily filter received emails by sender
                receivedQuery = receivedQuery.Where(m => m.Sender != null && m.Sender.Contains(fromSender));

                sentQuery = sentQuery.Where(m => 1 == 0);
            }

            if (!string.IsNullOrWhiteSpace(toRecipient))
            {
                sentQuery = sentQuery.Where(m => m.Recipients.Any(r => r.Email.Contains(toRecipient)));
                
                receivedQuery = receivedQuery.Where(m => 1 == 0);
            }

            var receivedResults = await receivedQuery
                .OrderByDescending(m => m.TimeReceived)
                .Take(100) 
                .Select(m => new MailSearchResultDto
                {
                    MailId = m.MailId,
                    MailAccountId = m.MailAccountId,
                    Subject = m.Subject,
                    BodySnippet = GenerateBodySnippet(m.Body), 
                    Date = m.TimeReceived,
                    IsFavorite = m.IsFavorite,
                    Type = "received",
                    IsRead = m.IsRead,
                    Sender = m.Sender,
                    Tags = m.MailTags.Select(mt => mt.Tag.TagName).ToList()
                })
                .ToListAsync();

            var sentResults = await sentQuery
               .OrderByDescending(m => m.TimeSent)
               .Take(100) 
               .Select(m => new MailSearchResultDto
               {
                   MailId = m.MailId,
                   MailAccountId = m.MailAccountId,
                   Subject = m.Subject,
                   BodySnippet = GenerateBodySnippet(m.Body),
                   Date = m.TimeSent,
                   IsFavorite = m.IsFavorite,
                   Type = "sent",
                   IsRead = true, 
                   Recipients = m.Recipients.Select(r => r.Email).ToList()
               })
               .ToListAsync();

            var combinedResults = receivedResults.Concat(sentResults)
                                                .OrderByDescending(r => r.Date)
                                                .Take(150) 
                                                .ToList();

            Console.WriteLine($"--- Search found {combinedResults.Count} combined results. ---");
            return combinedResults;
        }

    }

}
