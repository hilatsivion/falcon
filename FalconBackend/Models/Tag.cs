using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(TagName), IsUnique = true)] // Ensure unique tag names for consistency
    public class Tag
    {
        [Key]
        [MaxLength(100)]
        public string TagName { get; set; } // Now the primary key

        // Relationships
        public ICollection<FavoriteTag> FavoriteTags { get; set; } = new List<FavoriteTag>();
        public ICollection<MailTag> MailTags { get; set; } = new List<MailTag>();
    }
}