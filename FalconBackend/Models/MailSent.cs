using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class MailSent : Mail
    {
        [Required]
        public override string Body { get; set; } // Enforces requirement only for sent emails

        [Required]
        public override string Subject { get; set; }

        public DateTime TimeSent { get; set; }
    }
}
