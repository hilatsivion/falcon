﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class MailReceived : Mail
    {
        [Required]
        public override string Body { get; set; } // Ensures received emails have content

        [Required]
        public override string Subject { get; set; }

        [Required]
        [MaxLength(255)]
        public string Sender { get; set; } // Ensures sender is always recorded

        public DateTime TimeReceived { get; set; }
        public bool IsRead { get; set; }

        // Many-to-many relationship with tags
        public ICollection<MailTag> MailTags { get; set; } = new List<MailTag>();
    }
}
