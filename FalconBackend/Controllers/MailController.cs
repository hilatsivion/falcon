using FalconBackend.Data;
using FalconBackend.Models;
using FalconBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/mail")]
    public class MailService : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorageService;

        public MailService(AppDbContext context, FileStorageService fileStorageService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        }

        [HttpGet("received/{userId}")]
        public async Task<IActionResult> GetReceivedEmailsAsync(int userId)
        {
            var emails = await _context.MailReceived
                .Include(mr => mr.Attachments)
                .Where(mr => mr.MailAccount.AppUserId == userId)
                .ToListAsync();

            if (!emails.Any())
                return NotFound("No received emails found for this user.");

            return Ok(emails);
        }

        [HttpGet("sent/{userId}")]
        public async Task<IActionResult> GetSentEmailsAsync(int userId)
        {
            var emails = await _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => ms.MailAccount.AppUserId == userId)
                .ToListAsync();

            if (!emails.Any())
                return NotFound("No sent emails found for this user.");

            return Ok(emails);
        }

        [HttpGet("drafts/{userId}")]
        public async Task<IActionResult> GetDraftEmailsAsync(int userId)
        {
            var drafts = await _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => d.MailAccount.AppUserId == userId)
                .ToListAsync();

            if (!drafts.Any())
                return NotFound("No drafts found for this user.");

            return Ok(drafts);
        }

        [HttpPost("received")]
        public async Task<IActionResult> AddReceivedEmailAsync(int mailAccountId, string sender, string subject, string body, List<IFormFile> attachments)
        {
            if (string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                return BadRequest("Sender, subject, and body are required.");

            var existingEmail = await _context.MailReceived
                .FirstOrDefaultAsync(m => m.MailAccountId == mailAccountId && m.Sender == sender && m.Subject == subject);

            if (existingEmail != null)
                return Conflict("Duplicate email detected.");

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

                return CreatedAtAction(nameof(GetReceivedEmailsAsync), new { userId = mailAccountId }, receivedMail);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to save received email.");
            }
        }

        [HttpPost("sent")]
        public async Task<IActionResult> AddSentEmailAsync(int mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                return BadRequest("Subject and body are required.");

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

                return CreatedAtAction(nameof(GetSentEmailsAsync), new { userId = mailAccountId }, sentMail);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to save sent email.");
            }
        }

        [HttpPost("draft")]
        public async Task<IActionResult> AddDraftEmailAsync(int mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                return BadRequest("Subject and body are required.");

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

                return CreatedAtAction(nameof(GetDraftEmailsAsync), new { userId = mailAccountId }, existingDraft);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to save draft email.");
            }
        }

        private async Task SaveAttachments(List<IFormFile> attachments, int mailId, int mailAccountId, string emailType, ICollection<Attachments> emailAttachments)
        {
            if (attachments == null || attachments.Count == 0)
                return;

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
