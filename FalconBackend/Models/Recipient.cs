using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(MailId))] // Optimized query performance when searching by MailId
    [Index(nameof(Email))]  // Optimized search for recipients by email
    public class Recipient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incrementing primary key
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } // Stores the recipient's email address

        // Relationships
        [ForeignKey("MailId")]
        public int MailId { get; set; }
        public Mail Mail { get; set; }
    }
}
