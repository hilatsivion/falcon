using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        // Relationships
        public Analytics Analytics { get; set; }
        public ICollection<MailAccount> MailAccounts { get; set; }
        public ICollection<Contact> Contacts { get; set; }
    }
}
