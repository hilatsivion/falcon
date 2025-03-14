using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FalconBackend.Services
{
    public class MailService
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorageService;

        public MailService(AppDbContext context, FileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        public Task<List<MailReceived>> GetReceivedEmailsAsync(int userId) =>
            _context.MailReceived
                .Include(mr => mr.Attachments)
                .Where(mr => mr.MailAccount.AppUserId == userId)
                .ToListAsync();

        public Task<List<MailSent>> GetSentEmailsAsync(int userId) =>
            _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => ms.MailAccount.AppUserId == userId)
                .ToListAsync();

        public Task<List<Draft>> GetDraftEmailsAsync(int userId) =>
            _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => d.MailAccount.AppUserId == userId)
                .ToListAsync();

        public async Task<MailReceived> AddReceivedEmailAsync(int mailAccountId, string sender, string subject, string body, List<IFormFile> attachments)
        {
            // Skip if email already exists
            var existingEmail = await _context.MailReceived
                .FirstOrDefaultAsync(m => m.MailAccountId == mailAccountId && m.Sender == sender && m.Subject == subject);

            if (existingEmail != null)
            {
                return existingEmail;
            }

            var receivedMail = new MailReceived
            {
                MailAccountId = mailAccountId,
                Sender = sender,
                Subject = subject,
                Body = body,
                TimeReceived = DateTime.UtcNow,
                IsRead = false,
                Attachments = new List<Attachments>()
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.MailReceived.Add(receivedMail);
                await _context.SaveChangesAsync();

                await SaveAttachments(attachments, receivedMail.MailId, mailAccountId, "Received", receivedMail.Attachments);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return receivedMail;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<MailSent> AddSentEmailAsync(int mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            var sentMail = new MailSent
            {
                MailAccountId = mailAccountId,
                Subject = subject,
                Body = body,
                TimeSent = DateTime.UtcNow,
                Attachments = new List<Attachments>()
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.MailSent.Add(sentMail);
                await _context.SaveChangesAsync();

                await SaveAttachments(attachments, sentMail.MailId, mailAccountId, "Sent", sentMail.Attachments);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return sentMail;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Draft> AddDraftEmailAsync(int mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            var existingDraft = await _context.Drafts
                .FirstOrDefaultAsync(d => d.MailAccountId == mailAccountId && d.Subject == subject);

            if (existingDraft != null)
            {
                existingDraft.Body = body;
                existingDraft.TimeCreated = DateTime.UtcNow;
            }
            else
            {
                existingDraft = new Draft
                {
                    MailAccountId = mailAccountId,
                    Subject = subject,
                    Body = body,
                    TimeCreated = DateTime.UtcNow,
                    IsSent = false,
                    Attachments = new List<Attachments>()
                };

                _context.Drafts.Add(existingDraft);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                await SaveAttachments(attachments, existingDraft.MailId, mailAccountId, "Drafts", existingDraft.Attachments);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return existingDraft;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task SaveAttachments(List<IFormFile> attachments, int mailId, int mailAccountId, string emailType, ICollection<Attachments> emailAttachments)
        {
            if (attachments == null || attachments.Count == 0)
                return;

            // Fetch all existing attachment names in one query for better performance
            var existingAttachments = await _context.Attachments
                .Where(a => a.MailId == mailId)
                .Select(a => a.Name)
                .ToListAsync();

            foreach (var file in attachments)
            {
                if (existingAttachments.Contains(file.FileName))
                {
                    continue;
                }

                string filePath = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, emailType);

                var attachment = new Attachments
                {
                    MailId = mailId,
                    Name = file.FileName,
                    FileType = Path.GetExtension(file.FileName),
                    FileSize = file.Length,
                    FilePath = filePath
                };

                _context.Attachments.Add(attachment);
                emailAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync();
        }
    }
}
