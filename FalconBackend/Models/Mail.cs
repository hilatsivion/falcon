using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public abstract class Mail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MailId { get; set; }

        public string Body { get; set; }
        public string Subject { get; set; }
        public bool IsFavorite { get; set; } = false;

        // Relationships
        [Required]
        [ForeignKey("MailAccount")]
        [MaxLength(100)] 
        public string MailAccountId { get; set; } // Foreign Key to MailAccount.Id (hashed GUID)

        public MailAccount MailAccount { get; set; }

        public ICollection<Recipient> Recipients { get; set; } = new List<Recipient>();
        public ICollection<Attachments> Attachments { get; set; } = new List<Attachments>();
    }
}
