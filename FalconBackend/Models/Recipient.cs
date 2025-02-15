using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Recipient
    {
        public int Id { get; set; }
        public string Email { get; set; } // Stores the recipient's email address

        public int MailId { get; set; }
        public Mail Mail { get; set; }
    }
}
