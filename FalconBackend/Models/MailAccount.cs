﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(EmailAddress), IsUnique = true)]
    public class MailAccount
    {
        [Key]
        [MaxLength(100)]
        public string MailAccountId { get; set; } = Guid.NewGuid().ToString("N"); 

        [Required]
        [MaxLength(500)]
        public string Token { get; set; }  

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string EmailAddress { get; set; }

        public DateTime LastMailSync { get; set; } = DateTime.UtcNow;
        public bool IsDefault { get; set; }

        [Required]
        public MailProvider Provider { get; set; }

        public enum MailProvider
        {
            Gmail,
            Outlook
        }

        // Relationships
        [Required]
        [ForeignKey("AppUser")]
        [MaxLength(255)]
        [EmailAddress]
        public string AppUserEmail { get; set; }  // Foreign key to AppUser.Email

        public AppUser AppUser { get; set; }

        public ICollection<Mail> Mails { get; set; } = new List<Mail>();
    }
}
