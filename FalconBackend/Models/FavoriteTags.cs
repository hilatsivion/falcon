using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(TagName), nameof(MailAccountId), IsUnique = true)] // Ensures a tag is favorited only once per account
    public class FavoriteTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Tag")]
        [MaxLength(100)]
        public string TagName { get; set; }

        public Tag Tag { get; set; }

        [Required]
        [ForeignKey("MailAccount")]
        [MaxLength(100)] // Ensure consistency with `MailAccountId` in `MailAccount.cs`
        public string MailAccountId { get; set; }

        public MailAccount MailAccount { get; set; }
    }
}
