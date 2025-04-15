using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FalconBackend.Services
{
    public class FilterService : IFilterService
    {
        private readonly AppDbContext _context;

        public FilterService(AppDbContext context)
        {
            _context = context;
        }

        private TagDto MapTagToDto(Tag tag)
        {
            if (tag == null) return null;
            return new TagDto { TagId = tag.Id, Name = tag.TagName, TagType = null };
        }

        private FilterFolderDto MapFilterFolderToDto(FilterFolder filterFolder)
        {
            if (filterFolder == null) return null;
            return new FilterFolderDto
            {
                FilterFolderId = filterFolder.FilterFolderId,
                Name = filterFolder.Name,
                FolderColor = filterFolder.FolderColor,
                Keywords = filterFolder.Keywords ?? new List<string>(),
                SenderEmails = filterFolder.SenderEmails ?? new List<string>(),
                Tags = filterFolder.FilterFolderTags?
                          .Select(fft => MapTagToDto(fft.Tag))
                          .Where(t => t != null).ToList() ?? new List<TagDto>()
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
                         .Select(mt => MapTagToDto(mt.Tag))
                         .Where(t => t != null).ToList() ?? new List<TagDto>()
            };
        }

        public async Task<FilterFolderDto?> CreateFilterAsync(string userEmail, FilterFolderCreateDto createDto)
        {
            var user = await _context.AppUsers.FindAsync(userEmail);
            if (user == null) { return null; }

            var filterFolder = new FilterFolder
            {
                Name = createDto.Name,
                FolderColor = createDto.FolderColor,
                Keywords = createDto.Keywords ?? new List<string>(),
                SenderEmails = createDto.SenderEmails ?? new List<string>(),
                AppUserEmail = userEmail
            };

            if (createDto.TagIds != null && createDto.TagIds.Any())
            {
                var tags = await _context.Tags
                                         .Where(t => createDto.TagIds.Contains(t.Id))
                                         .ToListAsync();
                foreach (var tag in tags)
                {
                    filterFolder.FilterFolderTags.Add(new FilterFolderTag { TagId = tag.Id });
                }
            }

            _context.FilterFolders.Add(filterFolder);
            await _context.SaveChangesAsync();

            var createdFilter = await _context.FilterFolders
                                      .Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag)
                                      .FirstOrDefaultAsync(f => f.FilterFolderId == filterFolder.FilterFolderId);
            return MapFilterFolderToDto(createdFilter);
        }

        public async Task<IEnumerable<FilterFolderDto>> GetFiltersAsync(string userEmail)
        {
            var filters = await _context.FilterFolders
                                      .Where(f => f.AppUserEmail == userEmail)
                                      .Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag)
                                      .ToListAsync();
            return filters.Select(MapFilterFolderToDto);
        }

        public async Task<FilterFolderDto?> GetFilterByIdAsync(string userEmail, int filterId)
        {
            var filterFolder = await _context.FilterFolders
                                     .Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag)
                                     .FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail);
            return MapFilterFolderToDto(filterFolder);
        }

        public async Task<bool> UpdateFilterAsync(string userEmail, int filterId, FilterFolderUpdateDto updateDto)
        {
            var filterFolder = await _context.FilterFolders
                                    .FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail);
            if (filterFolder == null) { return false; }

            filterFolder.Name = updateDto.Name;
            filterFolder.FolderColor = updateDto.FolderColor;
            filterFolder.Keywords = updateDto.Keywords ?? new List<string>();
            filterFolder.SenderEmails = updateDto.SenderEmails ?? new List<string>();

            var existingJoins = await _context.FilterFolderTags
                                              .Where(fft => fft.FilterFolderId == filterId).ToListAsync();
            _context.FilterFolderTags.RemoveRange(existingJoins);

            if (updateDto.TagIds != null && updateDto.TagIds.Any())
            {
                var tagsToAdd = await _context.Tags
                                        .Where(t => updateDto.TagIds.Contains(t.Id)).ToListAsync();
                foreach (var tag in tagsToAdd)
                {
                    _context.FilterFolderTags.Add(new FilterFolderTag { FilterFolderId = filterFolder.FilterFolderId, TagId = tag.Id });
                }
            }

            _context.Entry(filterFolder).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); return true; }
            catch (DbUpdateConcurrencyException) { return false; }
        }

        public async Task<bool> DeleteFilterAsync(string userEmail, int filterId)
        {
            var filterFolder = await _context.FilterFolders
                                       .FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail);
            if (filterFolder == null) { return false; }
            _context.FilterFolders.Remove(filterFolder);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<EmailSummaryDto>> GetEmailsByFilterAsync(string userEmail, int filterId)
        {
            var filter = await _context.FilterFolders
                                .Include(f => f.FilterFolderTags).ThenInclude(fft => fft.Tag)
                                .FirstOrDefaultAsync(f => f.FilterFolderId == filterId && f.AppUserEmail == userEmail);

            if (filter == null) { return new List<EmailSummaryDto>(); }

            var userMailAccounts = _context.MailAccounts
                                      .Where(ma => ma.AppUserEmail == userEmail)
                                      .Select(ma => ma.MailAccountId);

            var baseQuery = _context.Mails
                                    .Where(m => userMailAccounts.Contains(m.MailAccountId));

            IQueryable<MailReceived> receivedQuery;
            if (filter.SenderEmails != null && filter.SenderEmails.Any())
            {
                receivedQuery = baseQuery.OfType<MailReceived>()
                                         .Where(mr => mr.Sender != null && filter.SenderEmails.Contains(mr.Sender));
            }
            else
            {
                receivedQuery = baseQuery.OfType<MailReceived>();
            }

            if (filter.Keywords != null && filter.Keywords.Any())
            {
                receivedQuery = receivedQuery.Where(mr =>
                    filter.Keywords.Any(k =>
                        (mr.Subject != null && mr.Subject.Contains(k)) ||
                        (mr.Body != null && mr.Body.Contains(k))
                   )
                );
            }

            var filterTagIds = filter.FilterFolderTags.Select(fft => fft.TagId).ToList();
            if (filterTagIds.Any())
            {
                receivedQuery = receivedQuery
                                    .Include(mr => mr.MailTags)
                                        .ThenInclude(mt => mt.Tag)
                                    .Where(mr => mr.MailTags.Any(mt => filterTagIds.Contains(mt.TagId)));
            }
            else
            {
                receivedQuery = receivedQuery
                                    .Include(mr => mr.MailTags)
                                        .ThenInclude(mt => mt.Tag);
            }

            var emails = await receivedQuery
                                    .OrderByDescending(mr => mr.TimeReceived)
                                    .ToListAsync();

            return emails.Select(MapReceivedEmailToSummaryDto);
        }
    }
}