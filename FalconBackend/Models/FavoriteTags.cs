using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(TagId), nameof(AppUserEmail), IsUnique = true)] // Ensures a tag is favorited only once per account
    public class FavoriteTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Tag")]
        public int TagId { get; set; } 
        public Tag Tag { get; set; }

        [Required]
        [ForeignKey("AppUser")]
        [MaxLength(255)]
        [EmailAddress]
        public string AppUserEmail { get; set; }

        public AppUser AppUser { get; set; }
    }
}
