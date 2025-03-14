using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    [Index(nameof(Username), IsUnique = true)]  
    [Index(nameof(Email), IsUnique = true)]     
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }  

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }  

        public bool IsActive { get; set; } = true; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;  
        public DateTime? LastLogin { get; set; } 

        // Relationships
        public Analytics Analytics { get; set; }
        public ICollection<MailAccount> MailAccounts { get; set; }
        public ICollection<Contact> Contacts { get; set; }
    }
}
