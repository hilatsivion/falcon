using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public abstract class Mail
    {
        public int MailId { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public bool IsFavorite { get; set; } = false;

        // Relationships
        public int MailAccountId { get; set; }
        public MailAccount MailAccount { get; set; }

        public ICollection<Recipient> Recipients { get; set; }
        public ICollection<Attachments> Attachments { get; set; }
    }
}
