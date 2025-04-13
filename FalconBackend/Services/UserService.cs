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

        public UserService(AppDbContext context)
        {
            _context = context;
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



        //not real data need to change it later 
        public async Task InitializeDummyUserDataAsync(string userEmail)
        {
            Console.WriteLine($"--- Initializing dummy data for user: {userEmail} ---");

            var mailAccount = await _context.MailAccounts
                                        .FirstOrDefaultAsync(ma => ma.AppUserEmail == userEmail);

            if (mailAccount == null)
            {
                Console.WriteLine($"--- No MailAccount found for {userEmail}. Cannot add dummy emails. ---");
                return;
            }

            // Check if dummy data already exists
            bool alreadyInitialized = await _context.MailReceived
                                              .AnyAsync(mr => mr.MailAccountId == mailAccount.MailAccountId && mr.Subject.StartsWith("Dummy Subject"));
            if (alreadyInitialized)
            {
                Console.WriteLine($"--- Dummy data seems to already exist for account {mailAccount.MailAccountId}. Skipping creation. ---");
                return;
            }

            // --- Fetch available system tags (interests) ---
            var availableTags = await _context.Tags
                                            .Where(t => !(t is UserCreatedTag)) // Assuming interests are not UserCreatedTag
                                            .ToListAsync();

            if (!availableTags.Any())
            {
                Console.WriteLine($"--- Warning: No system tags found in the database to assign to dummy emails. ---");
                // Proceed without tags or handle as needed
            }

            // --- Create Dummy Emails using a Loop ---
            var dummyEmailsToAdd = new List<MailReceived>();
            var random = new Random();

            for (int i = 1; i <= 20; i++)
            {
                var senderName = $"Sender {i}";
                var senderEmail = $"sender{i}@example.com";
                bool isRead = random.Next(0, 2) == 1;
                bool isFavorite = i <= 3 ? (random.Next(0, 2) == 1) : false;

                // --- Create MailTags list for this email ---
                var tagsForThisEmail = new List<MailTag>();
                if (availableTags.Any()) 
                {
                    int numberOfTags = random.Next(1, 4);
                    var assignedTags = new HashSet<int>(); 

                    for (int j = 0; j < numberOfTags && assignedTags.Count < availableTags.Count; j++)
                    {
                        // Select a random tag from the available list
                        Tag selectedTagEntity = availableTags[random.Next(availableTags.Count)];

                        // Ensure we don't add the same tag twice to the same email
                        if (assignedTags.Add(selectedTagEntity.Id))
                        {
                            tagsForThisEmail.Add(new MailTag
                            {
                                Tag = selectedTagEntity 
                            });
                        }
                    }
                }
                // --- End Create MailTags ---


                var dummyEmail = new MailReceived
                {
                    MailAccountId = mailAccount.MailAccountId,
                    Sender = $"{senderName} <{senderEmail}>",
                    Subject = $"Dummy Subject {i}: Meeting Follow-up", // Varied subject
                    Body = $"Hello {userEmail},\n\nThis is the detailed body content for dummy email number {i}. Discussing the points from our last meeting.\n\nRegards,\n{senderName}",
                    TimeReceived = DateTime.UtcNow.AddMinutes(-(i * 5 + 5)),
                    IsRead = isRead,
                    IsFavorite = isFavorite,
                    Recipients = new List<Recipient> { new Recipient { Email = mailAccount.EmailAddress } },
                    Attachments = new List<Attachments>(),
                    MailTags = tagsForThisEmail 
                };
                dummyEmailsToAdd.Add(dummyEmail);
            }

            // Add the list of generated emails (with their MailTags) to the context
            _context.MailReceived.AddRange(dummyEmailsToAdd);

            try
            {
                await _context.SaveChangesAsync(); // EF Core handles linking MailTags to the new MailReceived IDs
                Console.WriteLine($"--- Successfully added {dummyEmailsToAdd.Count} dummy emails with tags for account {mailAccount.MailAccountId} ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- FAILED to save dummy emails with tags for account {mailAccount.MailAccountId}: {ex.Message} ---");
                // Log inner exception if possible
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"--- Inner Exception: {ex.InnerException.Message} ---");
                }
                throw;
            }
        }
    }

}
