// Falcon/FlaconBacked/Services/FilterFolderService.cs

using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FalconBackend.Models;
using FalconBackend.Data;
using System;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Text;

namespace FalconBackend.Services
{
    public class FilterService : IFilterService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FilterService> _logger;

        public FilterService(AppDbContext context, ILogger<FilterService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private TagDto MapTagToDto(Tag tag)
        {
            if (tag == null) return null;
            return new TagDto { TagId = tag.Id, Name = tag.TagName, TagType = tag.GetType().Name };
        }

        private FilterFolderDto MapFilterFolderToDto(FilterFolder filterFolder, int totalEmails, int newEmailsCount)
        {
            if (filterFolder == null) return null;
            return new FilterFolderDto
            {
                FilterFolderId = filterFolder.FilterFolderId,
                Name = filterFolder.Name ?? string.Empty,
                FolderColor = filterFolder.FolderColor,
                Keywords = filterFolder.Keywords ?? new List<string>(),
                SenderEmails = filterFolder.SenderEmails ?? new List<string>(),
                Tags = filterFolder.FilterFolderTags?
                          .Where(fft => fft.Tag != null)
                          .Select(fft => MapTagToDto(fft.Tag))
                          .Where(t => t != null).ToList() ?? new List<TagDto>(),
                TotalEmails = totalEmails,
                NewEmailsCount = newEmailsCount
            };
        }

        private EmailSummaryDto MapReceivedEmailToSummaryDto(MailReceived email)
        {
            if (email == null) return null;
            string bodyPreview = string.IsNullOrEmpty(email.Body)
                ? string.Empty
                : email.Body.Substring(0, Math.Min(email.Body.Length, 150)) + (email.Body.Length > 150 ? "..." : "");

            return new EmailSummaryDto
            {
                MailId = email.MailId,
                Subject = email.Subject ?? string.Empty,
                SenderEmail = email.Sender ?? string.Empty,
                TimeReceived = email.TimeReceived,
                BodyPreview = bodyPreview,
                IsRead = email.IsRead,
                Tags = email.MailTags?
                         .Where(mt => mt.Tag != null)
                         .Select(mt => MapTagToDto(mt.Tag))
                         .Where(t => t != null).ToList() ?? new List<TagDto>()
            };
        }

        public async Task<IEnumerable<FilterFolderDto>> GetFiltersAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail)) { return new List<FilterFolderDto>(); }

            List<string> userMailAccountIds;
            try { userMailAccountIds = await _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail).Select(ma => ma.MailAccountId).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error fetching MailAccountIds for user {UserEmail}", userEmail); return new List<FilterFolderDto>(); }

            if (!userMailAccountIds.Any()) { return new List<FilterFolderDto>(); }

            List<FilterFolder> filters;
            try { filters = await _context.FilterFolders.Where(f => f.AppUserEmail == userEmail).Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error fetching FilterFolders for user {UserEmail}", userEmail); return new List<FilterFolderDto>(); }

            var filterDtos = new List<FilterFolderDto>();

            foreach (var filter in filters)
            {
                if (filter == null) continue;

                int totalCount = 0;
                int newCount = 0;

                try
                {
                    IQueryable<MailReceived> baseQuery = _context.MailReceived.AsNoTracking().Where(m => m.MailAccountId != null && userMailAccountIds.Contains(m.MailAccountId));
                    var filteredQuery = ApplyFilterCriteria(baseQuery, filter);

                    var matchingMailIds = await filteredQuery
                                                .Select(mr => mr.MailId)
                                                .Distinct()
                                                .ToListAsync();

                    totalCount = matchingMailIds.Count;

                    if (totalCount > 0)
                    {
                        newCount = await _context.MailReceived
                                            .Where(mr => matchingMailIds.Contains(mr.MailId) && !mr.IsRead)
                                            .CountAsync();
                    }

                    var filterForMapping = filters.FirstOrDefault(f => f.FilterFolderId == filter.FilterFolderId);
                    filterDtos.Add(MapFilterFolderToDto(filterForMapping ?? filter, totalCount, newCount));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"--- Filter Count Error ---");
                    Debug.WriteLine($"Filter ID: {filter.FilterFolderId} (Name: '{filter.Name}')");
                    Debug.WriteLine($"Error Message: {ex.Message}");
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    Debug.WriteLine($"--- End Filter Count Error ---");

                    filterDtos.Add(MapFilterFolderToDto(filter, -1, -1));
                }
            }
            return filterDtos;
        }

        // --- ApplyFilterCriteria Helper (Using PredicateBuilder for Senders AND Keywords) ---
        private IQueryable<MailReceived> ApplyFilterCriteria(IQueryable<MailReceived> query, FilterFolder filter)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(filter);

            // Apply SenderEmails filter (Using PredicateBuilder)
            var validSenders = filter.SenderEmails?
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToList();
            if (validSenders != null && validSenders.Any())
            {
                var senderPredicate = PredicateBuilder.False<MailReceived>();
                foreach (var sender in validSenders)
                {
                    string tempSender = sender; // Avoid closure issue
                    senderPredicate = senderPredicate.Or(mr => mr.Sender != null && mr.Sender.ToLower().Contains(tempSender));
                }
                query = query.Where(senderPredicate); // Apply combined OR conditions
            }

            // Apply Keywords filter (Using PredicateBuilder)
            var validKeywords = filter.Keywords?
                .Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.ToLower()).ToList();
            if (validKeywords != null && validKeywords.Any())
            {
                var keywordPredicate = PredicateBuilder.False<MailReceived>();
                foreach (var keyword in validKeywords)
                {
                    string tempKeyword = keyword;
                    keywordPredicate = keywordPredicate.Or(mr =>
                        (mr.Subject != null && mr.Subject.ToLower().Contains(tempKeyword)) ||
                        (mr.Body != null && mr.Body.ToLower().Contains(tempKeyword))
                    );
                }
                query = query.Where(keywordPredicate);
            }

            // Apply Tags filter (No changes needed here)
            var filterTagIds = filter.FilterFolderTags?.Select(fft => fft.TagId).Distinct().ToList();
            if (filterTagIds != null && filterTagIds.Any())
            {
                query = query.Where(mr => mr.MailTags != null && mr.MailTags.Any(mt => filterTagIds.Contains(mt.TagId)));
            }

            return query;
        }

        // --- Other Service Methods ---
        public async Task<FilterFolderDto?> CreateFilterAsync(string userEmail, FilterFolderCreateDto createDto)
        {
            ArgumentNullException.ThrowIfNull(createDto);
            if (string.IsNullOrEmpty(userEmail)) return null;
            var user = await _context.AppUsers.FindAsync(userEmail);
            if (user == null) return null;

            var filterFolder = new FilterFolder { Name = createDto.Name, FolderColor = createDto.FolderColor, Keywords = createDto.Keywords?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>(), SenderEmails = createDto.SenderEmails?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>(), AppUserEmail = userEmail, FilterFolderTags = new List<FilterFolderTag>() };
            if (createDto.TagIds != null && createDto.TagIds.Any()) { var distinctTagIds = createDto.TagIds.Distinct().ToList(); var tags = await _context.Tags.Where(t => distinctTagIds.Contains(t.Id)).ToListAsync(); foreach (var tag in tags) { filterFolder.FilterFolderTags.Add(new FilterFolderTag { TagId = tag.Id }); } }

            try { _context.FilterFolders.Add(filterFolder); await _context.SaveChangesAsync(); var createdFilter = await _context.FilterFolders.Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag).AsNoTracking().FirstOrDefaultAsync(f => f.FilterFolderId == filterFolder.FilterFolderId); return MapFilterFolderToDto(createdFilter, 0, 0); }
            catch (Exception ex) { _logger.LogError(ex, "Error saving new filter for user {UserEmail}", userEmail); return null; }
        }

        public async Task<FilterFolderDto?> GetFilterByIdAsync(string userEmail, int filterId)
        {
            if (string.IsNullOrEmpty(userEmail)) return null;
            try { var filterFolder = await _context.FilterFolders.Where(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail).Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag).AsNoTracking().FirstOrDefaultAsync(); if (filterFolder == null) return null; return MapFilterFolderToDto(filterFolder, 0, 0); }
            catch (Exception ex) { _logger.LogError(ex, "Error fetching filter by ID {FilterId} for user {UserEmail}", filterId, userEmail); return null; }
        }

        public async Task<bool> UpdateFilterAsync(string userEmail, int filterId, FilterFolderUpdateDto updateDto)
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            if (string.IsNullOrEmpty(userEmail)) return false;
            try
            {
                var filterFolder = await _context.FilterFolders.Include(f => f.FilterFolderTags).FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail); if (filterFolder == null) return false;
                filterFolder.Name = updateDto.Name; filterFolder.FolderColor = updateDto.FolderColor; filterFolder.Keywords = updateDto.Keywords?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>(); filterFolder.SenderEmails = updateDto.SenderEmails?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
                var tagsToKeep = updateDto.TagIds?.Distinct().ToList() ?? new List<int>(); var currentTags = filterFolder.FilterFolderTags.Select(fft => fft.TagId).ToList(); var tagsToRemove = filterFolder.FilterFolderTags.Where(fft => !tagsToKeep.Contains(fft.TagId)).ToList(); foreach (var tagToRemove in tagsToRemove) { _context.FilterFolderTags.Remove(tagToRemove); }
                var tagsToAddIds = tagsToKeep.Where(id => !currentTags.Contains(id)).ToList(); if (tagsToAddIds.Any()) { var tagsToAdd = await _context.Tags.Where(t => tagsToAddIds.Contains(t.Id)).ToListAsync(); foreach (var tag in tagsToAdd) { filterFolder.FilterFolderTags.Add(new FilterFolderTag { TagId = tag.Id }); } }
                await _context.SaveChangesAsync(); return true;
            }
            catch (DbUpdateConcurrencyException ex) { _logger.LogWarning(ex, "Concurrency error updating filter {FilterId}", filterId); return false; }
            catch (Exception ex) { _logger.LogError(ex, "Error updating filter {FilterId}", filterId); return false; }
        }

        public async Task<bool> DeleteFilterAsync(string userEmail, int filterId)
        {
            if (string.IsNullOrEmpty(userEmail)) return false;
            try { var filterFolder = await _context.FilterFolders.FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail); if (filterFolder == null) return false; _context.FilterFolders.Remove(filterFolder); await _context.SaveChangesAsync(); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting filter {FilterId}", filterId); return false; }
        }

        public async Task<IEnumerable<EmailSummaryDto>> GetEmailsByFilterAsync(string userEmail, int filterId)
        {
            if (string.IsNullOrEmpty(userEmail)) throw new ArgumentNullException(nameof(userEmail));
            var filter = await _context.FilterFolders.AsNoTracking().Include(f => f.FilterFolderTags).FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail);
            if (filter == null) { return new List<EmailSummaryDto>(); }
            var userMailAccountIds = await _context.MailAccounts.Where(ma => ma.AppUserEmail == userEmail).Select(ma => ma.MailAccountId).ToListAsync();
            if (!userMailAccountIds.Any()) { return new List<EmailSummaryDto>(); }

            IQueryable<MailReceived> baseQuery = _context.MailReceived.AsQueryable();
            var userEmailsQuery = baseQuery.Where(m => m.MailAccountId != null && userMailAccountIds.Contains(m.MailAccountId));
            var finalQuery = ApplyFilterCriteria(userEmailsQuery, filter);

            try
            {
                var emails = await finalQuery.Include(mr => mr.MailTags).ThenInclude(mt => mt.Tag).OrderByDescending(mr => mr.TimeReceived).Take(200).AsNoTracking().ToListAsync();
                return emails.Select(MapReceivedEmailToSummaryDto).Where(dto => dto != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emails for filter ID {FilterId}", filterId);
                return new List<EmailSummaryDto>();
            }
        }
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2) { var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>()); return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters); }
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2) { var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>()); return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters); }
    }
}