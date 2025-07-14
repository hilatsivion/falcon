using System;
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
        [MaxLength(2000)]  // Increased for longer tokens
        public string AccessToken { get; set; }  // Renamed from Token for clarity

        [MaxLength(2000)]
        public string? RefreshToken { get; set; }  // For automatic token renewal

        public DateTime? TokenExpiresAt { get; set; }  // When access token expires
        public DateTime? RefreshTokenExpiresAt { get; set; }  // When refresh token expires

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string EmailAddress { get; set; }

        public DateTime LastMailSync { get; set; } = DateTime.UtcNow;
        public bool IsDefault { get; set; }
        public bool IsTokenValid { get; set; } = true;  // Track token validity

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

        // Helper method to check if access token needs refresh
        public bool NeedsTokenRefresh()
        {
            return TokenExpiresAt.HasValue && 
                   DateTime.UtcNow.AddMinutes(5) >= TokenExpiresAt.Value; // Refresh 5 minutes before expiry
        }

        // Helper method to check if refresh token is still valid
        public bool HasValidRefreshToken()
        {
            return !string.IsNullOrEmpty(RefreshToken) && 
                   (!RefreshTokenExpiresAt.HasValue || DateTime.UtcNow < RefreshTokenExpiresAt.Value);
        }
    }
}
