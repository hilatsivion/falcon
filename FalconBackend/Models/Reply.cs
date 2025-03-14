using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(RepliedToMailId))] // Optimized query performance when searching by replied mail ID
    public class Reply
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int Id { get; set; }

        [Required]
        public int RepliedToMailId { get; set; } 

        [Required]
        public int ReplyChainPosition { get; set; } 

        // Relationships
        [ForeignKey("RepliedToMailId")]
        public Mail Mail { get; set; }
    }
}
