using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(AppUserEmail), IsUnique = false)] // Index for faster lookups
    public class Analytics
    {
        [Key]
        [ForeignKey("AppUser")]
        [MaxLength(255)]
        [EmailAddress]
        public string AppUserEmail { get; set; } // Now the primary key, linked to AppUser.Email

        // Time Tracking
        public float TimeSpentToday { get; set; }
        public float AvgTimeSpentDaily { get; set; }
        public float TimeSpentThisWeek { get; set; }
        public float AvgTimeSpentWeekly { get; set; }

        // Email Tracking
        public int EmailsReceivedWeekly { get; set; }
        public int EmailsSentWeekly { get; set; }
        public int SpamEmailsWeekly { get; set; }
        public int ReadEmailsWeekly { get; set; }

        // Spam Detection Rate (Calculated Field)
        [NotMapped] // Prevents EF from mapping to the database
        public float SpamDetectionRate => EmailsReceivedWeekly == 0 ? 0 :
            (float)SpamEmailsWeekly / EmailsReceivedWeekly * 100;

        // Timestamp (Last Updated)
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Relationships
        public AppUser AppUser { get; set; }
    }
}
