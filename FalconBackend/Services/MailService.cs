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
    /// <summary>
    /// Handles all queries related to emails, including received emails, sent emails, drafts, 
    /// managing attachments, retrieving, marking, and deleting emails.
    /// </summary>
    public class MailService
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorageService;

        public MailService(AppDbContext context, FileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        // Get all received emails for a user, including attachments
        public Task<List<MailReceived>> GetReceivedEmailsAsync(int userId) =>
            _context.MailReceived
                .Include(mr => mr.Attachments)
                .Where(mr => mr.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Get all sent emails for a user, including attachments
        public Task<List<MailSent>> GetSentEmailsAsync(int userId) =>
            _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => ms.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Get all draft emails for a user, including attachments
        public Task<List<Draft>> GetDraftEmailsAsync(int userId) =>
            _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => d.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Get a single email (received, sent, or draft) by ID, including attachments
        public async Task<Mail> GetMailByIdAsync(int mailId)
        {
            var receivedMail = await _context.MailReceived
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MailId == mailId);

            if (receivedMail != null) return receivedMail;

            var sentMail = await _context.MailSent
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MailId == mailId);

            if (sentMail != null) return sentMail;

            return await _context.Drafts
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MailId == mailId);
        }

        // Mark a received email as read
        public async Task<bool> MarkAsReadAsync(int mailId)
        {
            var email = await _context.MailReceived.FirstOrDefaultAsync(m => m.MailId == mailId);
            if (email == null)
                return false;

            email.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Add a new received email and save attachments
        public async Task<MailReceived> AddReceivedEmailAsync(int mailAccountId, string sender, string subject, string body, List<IFormFile> attachments)
        {
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

            _context.MailReceived.Add(receivedMail);
            await _context.SaveChangesAsync(); // Save email first to get ID

            await SaveAttachmentsAsync(receivedMail.MailId, mailAccountId, "Received", attachments, receivedMail.Attachments.ToList());
            await _context.SaveChangesAsync();

            return receivedMail;
        }

        // Add a new sent email and save attachments
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

            _context.MailSent.Add(sentMail);
            await _context.SaveChangesAsync();

            await SaveAttachmentsAsync(sentMail.MailId, mailAccountId, "Sent", attachments, sentMail.Attachments.ToList());
            await _context.SaveChangesAsync();

            return sentMail;
        }

        // Add a new draft email and save attachments
        public async Task<Draft> AddDraftEmailAsync(int mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            var draftMail = new Draft
            {
                MailAccountId = mailAccountId,
                Subject = subject,
                Body = body,
                TimeCreated = DateTime.UtcNow,
                IsSent = false,
                Attachments = new List<Attachments>()
            };

            _context.Drafts.Add(draftMail);
            await _context.SaveChangesAsync();

            await SaveAttachmentsAsync(draftMail.MailId, mailAccountId, "Drafts", attachments, draftMail.Attachments.ToList());
            await _context.SaveChangesAsync();

            return draftMail;
        }

        // Helper function to save attachments
        private async Task SaveAttachmentsAsync(int mailId, int mailAccountId, string emailType, List<IFormFile> attachments, List<Attachments> mailAttachments)
        {
            if (attachments == null || attachments.Count == 0) return;

            foreach (var file in attachments)
            {
                string filePath = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, mailId, emailType);
                var attachment = new Attachments
                {
                    MailId = mailId,
                    Name = file.FileName,
                    FileType = Path.GetExtension(file.FileName),
                    FileSize = file.Length,
                    FilePath = filePath
                };

                _context.Attachments.Add(attachment);
                mailAttachments.Add(attachment);
            }
        }

        // Delete an email (received, sent, or draft) along with its attachments
        public async Task<bool> DeleteMailAsync(int mailId)
        {
            var email = await GetMailByIdAsync(mailId);
            if (email == null)
                return false;

            // Remove and delete all associated attachments
            var attachments = _context.Attachments.Where(a => a.MailId == mailId).ToList();
            foreach (var attachment in attachments)
            {
                if (File.Exists(attachment.FilePath))
                    File.Delete(attachment.FilePath);

                _context.Attachments.Remove(attachment);
            }

            _context.Remove(email);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
