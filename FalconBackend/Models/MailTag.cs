using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalconBackend.Models
{
    public class MailTag
    {
        // Removed Id property - using composite key (MailReceivedId, TagId) defined in AppDbContext
        
        [Required]
        [ForeignKey("MailReceived")]
        public int MailReceivedId { get; set; }
        public MailReceived MailReceived { get; set; }

        [Required]
        [ForeignKey("Tag")]
        public int TagId { get; set; } 
        public Tag Tag { get; set; }
    }
}
