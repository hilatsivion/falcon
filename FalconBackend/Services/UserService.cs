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
                    AppUserEmail = userEmail, // Links tag to the user
                    TagName = tag.TagName
                };

                _context.FavoriteTags.Add(favoriteTag);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
