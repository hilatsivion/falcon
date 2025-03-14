using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(MailId))] // Index for faster lookup by MailId
    public class Attachments
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incrementing primary key
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(20)]
        public string FileType { get; set; }

        [Required]
        public float FileSize { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        // Relationships
        [ForeignKey("MailId")]
        public int MailId { get; set; }

        public Mail Mail { get; set; }
    }
}
