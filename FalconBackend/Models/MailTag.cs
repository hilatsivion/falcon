using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalconBackend.Models
{
    public class MailTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
