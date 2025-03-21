﻿using FalconBackend.Data;
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
        private readonly AnalyticsService _analyticsService;

        public MailService(AppDbContext context, FileStorageService fileStorageService, AnalyticsService analyticsService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _analyticsService = analyticsService;
        } 

        public Task<List<MailReceived>> GetReceivedEmailsByMailAccountAsync(string mailAccountId) =>
          _context.MailReceived
              .Include(mr => mr.Attachments)
              .Where(mr => mr.MailAccount.MailAccountId == mailAccountId)
              .ToListAsync();

        public Task<List<MailSent>> GetSentEmailsByMailAccountAsync(string mailAccountId) =>
            _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => ms.MailAccount.MailAccountId == mailAccountId)
                .ToListAsync();

        public Task<List<Draft>> GetDraftEmailsByMailAccountAsync(string mailAccountId) =>
            _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => d.MailAccount.MailAccountId == mailAccountId)
                .ToListAsync();

        // New methods to get emails across all mail accounts of a user
        public async Task<List<MailReceived>> GetAllReceivedEmailsByUserAsync(string userEmail) =>
            await _context.MailReceived
                .Include(mr => mr.Attachments)
                .Where(mr => _context.MailAccounts
                    .Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId)
                    .Contains(mr.MailAccountId))
                .ToListAsync();

        public async Task<List<MailSent>> GetAllSentEmailsByUserAsync(string userEmail) =>
            await _context.MailSent
                .Include(ms => ms.Attachments)
                .Where(ms => _context.MailAccounts
                    .Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId)
                    .Contains(ms.MailAccountId))
                .ToListAsync();

        public async Task<List<Draft>> GetAllDraftEmailsByUserAsync(string userEmail) =>
            await _context.Drafts
                .Include(d => d.Attachments)
                .Where(d => _context.MailAccounts
                    .Where(ma => ma.AppUserEmail == userEmail)
                    .Select(ma => ma.MailAccountId)
                    .Contains(d.MailAccountId))
                .ToListAsync();

        public async Task<MailReceived> AddReceivedEmailAsync(string mailAccountId, string sender, string subject, string body, List<IFormFile> attachments)
        {
            var mailAccount = await _context.MailAccounts
                .Include(ma => ma.AppUser)
                .FirstOrDefaultAsync(ma => ma.MailAccountId == mailAccountId);

            if (mailAccount == null)
                throw new Exception("Mail account not found.");

            string userEmail = mailAccount.AppUserEmail;

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

                // Update analytics for received email
                await _analyticsService.UpdateEmailsReceivedWeeklyAsync(userEmail);

                return receivedMail;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<MailSent> AddSentEmailAsync(string mailAccountId, string subject, string body, List<IFormFile> attachments)
        {
            var mailAccount = await _context.MailAccounts
                .Include(ma => ma.AppUser)
                .FirstOrDefaultAsync(ma => ma.MailAccountId == mailAccountId);

            if (mailAccount == null)
                throw new Exception("Mail account not found.");

            string userEmail = mailAccount.AppUserEmail;

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

                // Update analytics for sent email
                await _analyticsService.UpdateEmailsSentWeeklyAsync(userEmail);

                return sentMail;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Draft> AddDraftEmailAsync(string mailAccountId, string subject, string body, List<IFormFile> attachments)
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

        private async Task SaveAttachments(List<IFormFile> attachments, int mailId, string mailAccountId, string emailType, ICollection<Attachments> emailAttachments)
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
        public async Task<bool> ToggleFavoriteAsync(int mailId, bool isFavorite)
        {
            var mail = await _context.Mails.FindAsync(mailId);
            if (mail == null)
                return false;

            mail.IsFavorite = isFavorite;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleReadAsync(int mailId, bool isRead)
        {
            var mail = await _context.MailReceived.FindAsync(mailId);
            if (mail == null)
                return false;

            var mailAccount = await _context.MailAccounts
                .Include(ma => ma.AppUser)
                .FirstOrDefaultAsync(ma => ma.MailAccountId == mail.MailAccountId);

            mail.IsRead = isRead;
            await _context.SaveChangesAsync();

            string userEmail = mailAccount.AppUserEmail;

            if (isRead)
                await _analyticsService.UpdateReadEmailsWeeklyAsync(userEmail);
            return true;
        }

    }
}
