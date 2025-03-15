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

    }
}
