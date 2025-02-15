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
    /// This service contains all queries related to mails, including received emails, sent emails, drafts, and saving attachments.
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

        // Get all received emails for the user
        public Task<List<MailReceived>> GetReceivedEmailsAsync(int userId) =>
            _context.MailReceived
                .Include(mr => mr.Attachments) // Include attachments in response
                .Where(mr => mr.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Get all sent emails for the user
        public Task<List<MailSent>> GetSentEmailsAsync(int userId) =>
            _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => ms.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Get all draft emails for the user
        public Task<List<Draft>> GetDraftEmailsAsync(int userId) =>
            _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => d.MailAccount.AppUserId == userId)
                .ToListAsync();

        // Add a new received email with attachments
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

            // Save attachments
            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    string filePath = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, "Received");
                    var attachment = new Attachments
                    {
                        MailId = receivedMail.MailId,
                        Name = file.FileName,
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        FilePath = filePath
                    };

                    _context.Attachments.Add(attachment);
                    receivedMail.Attachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            return receivedMail;
        }

        // Add a new sent email with attachments
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

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    string filePath = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, "Sent");
                    var attachment = new Attachments
                    {
                        MailId = sentMail.MailId,
                        Name = file.FileName,
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        FilePath = filePath
                    };

                    _context.Attachments.Add(attachment);
                    sentMail.Attachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            return sentMail;
        }

        // Add a new draft email with attachments
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

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    string filePath = await _fileStorageService.SaveAttachmentAsync(file, mailAccountId, mailAccountId, "Drafts");
                    var attachment = new Attachments
                    {
                        MailId = draftMail.MailId,
                        Name = file.FileName,
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        FilePath = filePath
                    };

                    _context.Attachments.Add(attachment);
                    draftMail.Attachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            return draftMail;
        }
    }
}
