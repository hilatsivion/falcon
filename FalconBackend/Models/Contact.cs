using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(AppUserEmail))]
    [Index(nameof(EmailAddress), IsUnique = false)]
    public class Contact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incrementing primary key
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string EmailAddress { get; set; }

        public DateTime? LastContactDate { get; set; }

        [Required]
        public bool IsFavorite { get; set; }

        // Relationships
        [Required]
        [ForeignKey("AppUser")]
        [MaxLength(255)]
        [EmailAddress]
        public string AppUserEmail { get; set; } // Foreign Key to AppUser.Email

        public AppUser AppUser { get; set; }
    }
}
